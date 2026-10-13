#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.PortraitArtV1.Editor
{
    public sealed class PortraitArtCapture : MonoBehaviour
    {
        private const string Output="Docs/PortraitArtV1/Captures/";
        private readonly List<string> checks=new List<string>();
        private bool previousRunInBackground;
        private void Awake() => previousRunInBackground=Application.runInBackground;
        private void OnDestroy() => Application.runInBackground=previousRunInBackground;
        public static void Begin()
        {
            if(!Application.isPlaying) throw new InvalidOperationException("Play Bootstrap first.");
            var go=new GameObject("Portrait art evidence capture");DontDestroyOnLoad(go);
            go.AddComponent<PortraitArtCapture>().StartCoroutine(go.GetComponent<PortraitArtCapture>().Run());
        }
        private IEnumerator Run()
        {
            Application.runInBackground=true;
            yield return new WaitForSecondsRealtime(.7f);
            var c=FindFirstObjectByType<PortraitEntryController>();
            if(c.Navigation.CurrentScreenId!=PortraitEntryController.StartId) throw new InvalidOperationException("Begin on Start.");
            yield return Frame("Start");
            Click(c.StartScreen.Play);
            yield return new WaitForSecondsRealtime(.8f);
            c=FindFirstObjectByType<PortraitEntryController>();
            Check(c.Navigation.CurrentScreenId==PortraitEntryController.ModeId,"Play -> existing Lobby mode view");
            yield return Frame("ModeSelect");
            foreach(var item in new[]{(c.ModeScreen.Shop,"SC11_Store","StoreBackButton"),(c.ModeScreen.Settings,"SC13_Settings","SettingsBackButton"),(c.ModeScreen.Campaign,"SC02_ThemeSelect","ThemeBackButton")}) {
                Click(item.Item1);yield return new WaitForSecondsRealtime(.4f);
                Check(c.Navigation.CurrentScreenId==item.Item2,"Route "+item.Item2);
                yield return Frame(item.Item2);
                var back=c.Navigation.GetComponentsInChildren<Button>(true).First(b=>b.name==item.Item3);
                Click(back);yield return new WaitForSecondsRealtime(.4f);
                Check(c.Navigation.CurrentScreenId==PortraitEntryController.ModeId,"Return from "+item.Item2);
            }
            foreach(var button in new[]{c.ModeScreen.Ad,c.ModeScreen.Competition,c.ModeScreen.Cooperation}) {
                Check(!button.interactable,"Disabled "+button.name);
                ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
                Check(c.Navigation.CurrentScreenId==PortraitEntryController.ModeId,"Disabled click does not navigate "+button.name);
            }
            Check(c.ModeScreen.Currency.text=="--" && !c.Ledger.VerifiedCoinBalance.HasValue,"No fabricated coin balance");
            Click(c.ModeScreen.Back);yield return new WaitForSecondsRealtime(.35f);
            Check(c.Navigation.CurrentScreenId==PortraitEntryController.StartId,"Back -> Start");
            Check(c.StartScreen.GetComponentsInChildren<Button>().Length==1,"Start has only Play");
            Click(c.StartScreen.Play);yield return new WaitForSecondsRealtime(.35f);
            var safe=c.Navigation.GetComponentInChildren<SafeAreaLayout>();var rect=(RectTransform)safe.transform;
            var min=rect.anchorMin;var max=rect.anchorMax; safe.enabled=false;
            var anchors=SafeAreaLayout.CalculateNormalizedAnchors(new Rect(0,96,Screen.width,Screen.height-192),new Vector2(Screen.width,Screen.height));
            rect.anchorMin=new Vector2(anchors.x,anchors.y);rect.anchorMax=new Vector2(anchors.z,anchors.w);
            yield return null;yield return Frame("ModeSelect_SimulatedSafeArea96");
            rect.anchorMin=min;rect.anchorMax=max;safe.enabled=true;safe.Apply();
            File.WriteAllLines(Output+Screen.width+"x"+Screen.height+"_navigation.txt",checks);
            File.WriteAllText(Output+Screen.width+"x"+Screen.height+".complete","Actual Canvas capture and pointer-event routes complete.");
            Destroy(gameObject);
        }
        private void Check(bool condition,string label)
        {
            if(!condition) throw new InvalidOperationException(label);checks.Add("PASS "+label);
        }
        private void Click(Button button)
        {
            Check(button!=null && button.IsActive() && button.IsInteractable(),"Available "+button?.name);
            var point=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);
            // Manifest pivots are not always centered; click the actual rect center.
            var rect=(RectTransform)button.transform;
            point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var pointer=new PointerEventData(EventSystem.current){position=point};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Check(hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,"Top raycast "+button.name);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private IEnumerator Frame(string name)
        {
            yield return new WaitForEndOfFrame();Directory.CreateDirectory(Output);
            string path=Output+name+"_"+Screen.width+"x"+Screen.height;
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path+".png",image.EncodeToPNG());Destroy(image);
            var c=FindFirstObjectByType<PortraitEntryController>();
            var current=c.Navigation.transform.Find("SafeArea/ScreenHost/"+c.Navigation.CurrentScreenId);
            var labels=current.GetComponentsInChildren<TMP_Text>().Select(t=>new LabelAudit {
                name=t.name,text=t.text,size=t.fontSize,rect=t.rectTransform.rect,preferred=Measured(t),
                missing=new string(t.text.Where(x=>!char.IsControl(x)&&!t.font.HasCharacter(x,true,true)).Distinct().ToArray()),
                overflow=Measured(t).x>t.rectTransform.rect.width+1 || Measured(t).y>t.rectTransform.rect.height+1
            }).ToArray();
            var buttons=current.GetComponentsInChildren<Button>().Select(b=>new ButtonAudit {
                name=b.name,interactable=b.interactable,anchor=((RectTransform)b.transform).anchorMin,
                position=((RectTransform)b.transform).anchoredPosition,size=((RectTransform)b.transform).sizeDelta,
                screenBounds=Bounds((RectTransform)b.transform)
            }).ToArray();
            var safe=Bounds((RectTransform)c.Navigation.transform.Find("SafeArea"));
            foreach(var button in buttons) Check(safe.Contains(button.screenBounds.min) && safe.Contains(button.screenBounds.max),"SafeArea "+name+"/"+button.name);
            foreach(var label in labels) Check(!label.overflow && label.missing.Length==0,"Label fits "+name+"/"+label.name);
            File.WriteAllText(path+".json",JsonUtility.ToJson(new Audit { width=Screen.width,height=Screen.height,scene=SceneManager.GetActiveScene().name,screen=c.Navigation.CurrentScreenId,safeArea=safe,labels=labels,buttons=buttons },true));
        }
        private static Vector2 Measured(TMP_Text t) => t.GetPreferredValues(t.text,t.rectTransform.rect.width,Mathf.Infinity);
        private static Rect Bounds(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var a=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(null,corners[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
        }
        [Serializable] private sealed class Audit { public int width,height;public string scene,screen;public Rect safeArea;public LabelAudit[] labels;public ButtonAudit[] buttons; }
        [Serializable] private sealed class LabelAudit { public string name,text,missing;public float size;public bool overflow;public Rect rect;public Vector2 preferred; }
        [Serializable] private sealed class ButtonAudit { public string name;public bool interactable;public Vector2 anchor,position,size;public Rect screenBounds; }
    }
}
#endif
