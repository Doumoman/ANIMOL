using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Typography
{
    /// <summary>Explicit QA capture only; never records or drives UI during ordinary play.</summary>
    public sealed class PixelTypographyCapture : MonoBehaviour
    {
        public static void Capture() => new GameObject("Pixel font capture").AddComponent<PixelTypographyCapture>().StartCoroutine(Run());
        public static void CaptureCommon(GameObject[] prefabs) => new GameObject("Common font QA").AddComponent<PixelTypographyCapture>().StartCoroutine(Common(prefabs));
        private static IEnumerator Common(GameObject[] prefabs)
        {
            var canvas=FindFirstObjectByType<Canvas>(); var host=canvas.transform.Find("SafeArea/ScreenHost");
            var original=host.Cast<Transform>().Where(t=>t.gameObject.activeSelf).ToArray();
            foreach(var t in original) t.gameObject.SetActive(false);
            foreach(var prefab in prefabs) {
                var go=Instantiate(prefab,host,false); go.name=prefab.name+" (prefab fixture)";
                canvas.GetComponentInChildren<PixelTypographyInstaller>().Scan();
                yield return new WaitForSecondsRealtime(.12f);
                yield return Frame("Common_"+prefab.name);
                Destroy(go);yield return null;
            }
            foreach(var t in original) t.gameObject.SetActive(true);
            File.WriteAllText("Docs/PixelTypography/Captures/Common_"+Screen.width+"x"+Screen.height+".complete","Prefab text fixtures, not live service completion.");
        }
        private static IEnumerator Run()
        {
            Application.runInBackground=true;
            yield return new WaitForSecondsRealtime(.5f);
            string scene=SceneManager.GetActiveScene().name;
            var bridges=FindObjectsByType<PixelTextBridge>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var bridge in bridges) bridge.enabled=false;
            yield return null; yield return Frame(scene+"_before");
            foreach(var bridge in bridges) bridge.enabled=true;
            yield return null; yield return null; yield return Frame(scene+"_after");
            if(scene=="MoonGraphicsQA") {
                var buttons=FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                buttons.First(b=>b.name=="PauseButton").onClick.Invoke();
                yield return new WaitForSecondsRealtime(.3f); yield return Frame(scene+"_buttons");
                buttons.First(b=>b.name=="ContinueButton").onClick.Invoke();
            }
            File.WriteAllText("Docs/PixelTypography/Captures/"+scene+"_"+Screen.width+"x"+Screen.height+".complete","Actual Game View, original gameplay; results fixture is explicitly documented.");
        }
        private static IEnumerator Frame(string label)
        {
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory("Docs/PixelTypography/Captures");
            string path="Docs/PixelTypography/Captures/"+label+"_"+Screen.width+"x"+Screen.height;
            var image=ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(path+".png",image.EncodeToPNG());Destroy(image);
            var rows=FindObjectsByType<PixelTextBridge>(FindObjectsSortMode.None).Where(b=>b.isActiveAndEnabled && b.Source.enabled && b.Display!=null)
                .Select(b=>new Row { path=PathOf(b.transform),text=b.Source.text,pointPixels=b.PhysicalPointSize,
                    overflow=b.Overflow,lines=b.Display.textInfo.lineCount,rect=b.Source.rectTransform.rect,
                    preferred=b.Display.GetPreferredValues(b.Display.text,b.Source.rectTransform.rect.width,Mathf.Infinity),raycast=b.Display.raycastTarget,
                    missing=new string(b.Source.text.Where(c=>!char.IsControl(c)&&!b.Profile.Font.HasCharacter(c,true,true)).Distinct().ToArray()) }).ToArray();
            File.WriteAllText(path+".json",JsonUtility.ToJson(new Audit { width=Screen.width,height=Screen.height,labels=rows },true));
        }
        private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent)+"/"+t.name;
        [Serializable] private sealed class Audit { public int width,height;public Row[] labels; }
        [Serializable] private sealed class Row { public string path,text,missing;public float pointPixels;public bool overflow,raycast;public int lines;public Rect rect;public Vector2 preferred; }
    }
}
