using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ANIMOL.Typography.Tests
{
    public sealed class PixelTypographyPlayModeTests
    {
        private RenderTexture portraitTarget;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var old=SceneManager.GetActiveScene(); var scene=SceneManager.CreateScene("TypographyTestCleanup");SceneManager.SetActiveScene(scene);
            Time.timeScale=1; yield return SceneManager.UnloadSceneAsync(old);
            if (portraitTarget != null) { portraitTarget.Release(); Object.Destroy(portraitTarget); portraitTarget=null; }
        }
        private IEnumerator Load(string path)
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return new WaitForSecondsRealtime(.6f);
        }
        [UnityTest] public IEnumerator MoonHudKeepsSafeAreaAndRealPauseButtonInput()
        {
            yield return Load("Assets/ANIMOL/GraphicsQA/MoonGraphicsQA.unity");
            var bridge=Object.FindObjectsByType<PixelTextBridge>(FindObjectsSortMode.None).First(b=>b.Source.name=="Clock");
            // Batch mode has a 640x480 host surface. Use an explicit portrait camera target
            // for layout/input assertions; real overlay Game View captures are audited separately.
            var canvas=bridge.GetComponentInParent<Canvas>();
            canvas.GetComponent<ANIMOL.UI.PortraitDisplayController>().enabled=false;
            var scaler=canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor=1;
            portraitTarget=new RenderTexture(1080,1920,24); portraitTarget.Create();
            var camera=Camera.main; camera.targetTexture=portraitTarget;
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=10;
            yield return null; Canvas.ForceUpdateCanvases(); bridge.Synchronize();
            Assert.That(canvas.pixelRect.size,Is.EqualTo(new Vector2(1080,1920)));
            var rect=bridge.Source.rectTransform; var position=rect.anchoredPosition; var size=rect.sizeDelta;
            Assert.That(bridge.Display.raycastTarget,Is.False);Assert.That(bridge.Display.font,Is.EqualTo(bridge.Profile.Font));
            Assert.That(bridge.Overflow,Is.False,$"Screen={Screen.width}x{Screen.height}; scale={bridge.Display.canvas.scaleFactor}; rect={rect.rect}; size={bridge.PhysicalPointSize}; preferred={bridge.Display.GetPreferredValues(bridge.Display.text,rect.rect.width,Mathf.Infinity)}");Assert.That(bridge.PhysicalPointSize%16,Is.EqualTo(0).Within(.01f));
            var button=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="PauseButton");
            var buttonRect=(RectTransform)button.transform; var buttonSize=buttonRect.sizeDelta; int persistent=button.onClick.GetPersistentEventCount();
            var point=RectTransformUtility.WorldToScreenPoint(camera,buttonRect.position);
            var hits=new System.Collections.Generic.List<RaycastResult>();var pointer=new PointerEventData(EventSystem.current){position=point};
            EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits.Any(h=>h.gameObject.transform.IsChildOf(button.transform)||h.gameObject==button.gameObject),Is.True);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
            Assert.That(Time.timeScale,Is.EqualTo(0));
            var resume=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="ContinueButton");
            ExecuteEvents.Execute(resume.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;Assert.That(Time.timeScale,Is.EqualTo(1));
            Assert.That(rect.anchoredPosition,Is.EqualTo(position));Assert.That(rect.sizeDelta,Is.EqualTo(size));
            Assert.That(buttonRect.sizeDelta,Is.EqualTo(buttonSize));Assert.That(button.onClick.GetPersistentEventCount(),Is.EqualTo(persistent));
        }
        [UnityTest] public IEnumerator LegacyTextChangesAndDisablingBridgeRestoreOriginalPresentation()
        {
            yield return Load("Assets/ANIMOL/Scenes/Results.unity");
            var b=Object.FindObjectsByType<PixelTextBridge>(FindObjectsSortMode.None).First(x=>x.Source.name=="RewardBreakdown");
            b.Source.text="새 문구 · 똠 힣 · 123 ✓"; yield return null;yield return null;
            Assert.That(b.Display.text,Is.EqualTo(b.Source.text));Assert.That(b.Source.enabled,Is.True);Assert.That(b.Display.raycastTarget,Is.False);
            b.enabled=false;yield return null;Assert.That(b.Display.gameObject.activeSelf,Is.False);
            b.enabled=true;yield return null;yield return null;Assert.That(b.Display.gameObject.activeSelf,Is.True);
        }
        [UnityTest] public IEnumerator CommonUiPrefabInstancesKeepAllOriginalRectsAndCallbacks()
        {
            yield return Load("Assets/ANIMOL/Scenes/Results.unity");
            var canvas=Object.FindFirstObjectByType<Canvas>();
#if UNITY_EDITOR
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/ANIMOL/Prefabs/UI"})) {
                var path=AssetDatabase.GUIDToAssetPath(guid); if(path.EndsWith("UI_CommonRoot.prefab")) continue;
                var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),canvas.transform.Find("SafeArea/ScreenHost"));
                var rects=go.GetComponentsInChildren<RectTransform>(true);
                var poses=rects.Select(r=>(r.anchorMin,r.anchorMax,r.pivot,r.sizeDelta,r.anchoredPosition)).ToArray();
                var buttons=go.GetComponentsInChildren<Button>(true);var callbacks=buttons.Select(b=>b.onClick.GetPersistentEventCount()).ToArray();
                canvas.GetComponentInChildren<PixelTypographyInstaller>().Scan();
                foreach(var bridge in go.GetComponentsInChildren<PixelTextBridge>(true)) bridge.Synchronize();
                for(int i=0;i<rects.Length;i++) Assert.That((rects[i].anchorMin,rects[i].anchorMax,rects[i].pivot,rects[i].sizeDelta,rects[i].anchoredPosition),Is.EqualTo(poses[i]),path);
                Assert.That(buttons.Select(b=>b.onClick.GetPersistentEventCount()),Is.EqualTo(callbacks),path);
                Assert.That(go.GetComponentsInChildren<PixelTextBridge>(true).Length,Is.EqualTo(go.GetComponentsInChildren<Text>(true).Count(t=>t.GetComponentInParent<InputField>()==null)),path);
                Object.Destroy(go); yield return null;
            }
#endif
        }
    }
}
