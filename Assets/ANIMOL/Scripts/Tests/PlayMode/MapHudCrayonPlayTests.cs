#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using William.NotebookUI;
using Object=UnityEngine.Object;

namespace ANIMOL.Tests
{
    public sealed class MapHudCrayonTestGateway : NotebookGameplayGateway
    {
        public override void RequestPause(bool paused,string operation,Action<bool> completed)=>completed(true);
        public override void RequestNavigation(NotebookGameplayDestination destination,string stage,string target,string operation,Action<bool> completed)=>completed(false);
        public override void OpenApprovedNavigation(NotebookGameplayDestination destination,string stage,string target,string operation) { }
    }
    public sealed class MapHudCrayonPlayTests
    {
        const string Captures="Docs/Validation/MapHudCrayon";
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        NotebookRoot root;GameObject hudObject;
        bool hadWallet,hadPrefs;string wallet,prefs;
        static Component[] Bindings(GameObject host)=>host.GetComponentsInChildren<Component>(true)
            .Where(c=>c&&c.GetType().FullName=="Animol.MapHudCrayonUI.CrayonImageBinding").ToArray();
        static Component Scope(GameObject host)=>host.GetComponents<Component>().First(c=>c&&c.GetType().FullName=="Animol.MapHudCrayonUI.CrayonSkinScope");
        static Image Field(Component binding,string name)=>(Image)binding.GetType().GetField(name).GetValue(binding);
        static void Focus(Component c)=>c.GetType().GetMethod("OnApplicationFocus",Flags)?.Invoke(c,new object[]{true});
        [SetUp] public void Preserve()
        {
            hadWallet=PlayerPrefs.HasKey("WilliamNotebookUI.Wallet");wallet=PlayerPrefs.GetString("WilliamNotebookUI.Wallet");
            hadPrefs=PlayerPrefs.HasKey("WilliamNotebookUI.Preferences");prefs=PlayerPrefs.GetString("WilliamNotebookUI.Preferences");
        }
        [UnityTearDown] public IEnumerator Restore()
        {
            if(hudObject)Object.Destroy(hudObject);
            if(root)
            {
                var canvas=root.GetComponentInParent<Canvas>(true);
                if(canvas)Object.Destroy(canvas.rootCanvas.gameObject);
            }
            yield return null;
            if(hadWallet)PlayerPrefs.SetString("WilliamNotebookUI.Wallet",wallet);else PlayerPrefs.DeleteKey("WilliamNotebookUI.Wallet");
            if(hadPrefs)PlayerPrefs.SetString("WilliamNotebookUI.Preferences",prefs);else PlayerPrefs.DeleteKey("WilliamNotebookUI.Preferences");
            PlayerPrefs.Save();
        }
        IEnumerator Load()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Main/NotebookUI.unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;
            root=Object.FindFirstObjectByType<NotebookRoot>();Assert.That(root,Is.Not.Null);Focus(root);
        }
        static void Resolution(int width,int height)
        {
            if(width>height)typeof(ANIMOL.Presentation.Tests.NotebookUiPlayTests).GetMethod("SelectWidePcResolution",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{width,height});
            else AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Presentation.Editor.PortraitGameView")).First(t=>t!=null)
                .GetMethod("SelectFixedResolution",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)
                .Invoke(null,new object[]{width,height,"Crayon map/HUD QA"});
        }
        static IEnumerator Capture(string name)
        {
            // Render the current UI explicitly; Editor GameView readbacks can lag behind test state.
            var canvas=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Single(c=>c.isRootCanvas&&c.gameObject.activeInHierarchy);
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;
            var cameraObject=new GameObject("UI validation camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=Screen.height*.5f;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.055f,.07f,.10f);camera.transform.position=new Vector3(0,0,-10);
            var target=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
            var oldTarget=RenderTexture.active;Texture2D image=null;
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;
                yield return null;Canvas.ForceUpdateCanvases();yield return null;camera.Render();
                RenderTexture.active=target;image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                Directory.CreateDirectory(Captures);File.WriteAllBytes(Captures+"/"+name+".png",image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;
                RenderTexture.active=oldTarget;if(image)Object.Destroy(image);Object.Destroy(target);Object.Destroy(cameraObject);
            }
        }
        static void ValidBindings(GameObject host,bool still)
        {
            var bindings=Bindings(host);Assert.That(bindings.Length,Is.GreaterThan(0));
            foreach(var b in bindings)
            {
                var source=Field(b,"sourceImage");var decor=Field(b,"decoration");
                Assert.That(source&&decor,Is.True);Assert.That(decor.raycastTarget,Is.False);
                Assert.That(decor.transform.parent,Is.SameAs(source.transform));
                Assert.That(source.transform.Cast<Transform>().Count(t=>t.name=="Prepainted Crayon Decoration"),Is.EqualTo(1),source.name);
                if(decor.gameObject.activeInHierarchy&&decor.enabled)
                { Assert.That(decor.sprite.name,Does.EndWith("_A"));Assert.That(decor.material.name,Does.EndWith(still?"_Still":"_Animated")); }
            }
        }
        [UnityTest] public IEnumerator ActualMapUsesNewNumeralsAndIndependentSelectedStarRotation()
        {
            Resolution(1080,1920);yield return Load();root.SetReducedMotion(true);root.OpenStages();yield return null;yield return null;
            Assert.That(Bindings(root.stageScreen).Length,Is.EqualTo(343));
            Assert.That(root.walletText.font.atlasPopulationMode,Is.EqualTo(AtlasPopulationMode.Static));
            Assert.That(AssetDatabase.GetAssetPath(root.walletText.font),Does.StartWith("Assets/ANIMOL/CrayonNumerals/Generated/"));
            foreach(var text in root.stageScreen.GetComponentsInChildren<TMP_Text>(true).Where(t=>new[]{"I","II","III","IV"}.Contains(t.text)))
                Assert.That(AssetDatabase.GetAssetPath(text.font),Does.StartWith("Assets/ANIMOL/CrayonNumerals/Generated/"));
            ValidBindings(root.stageScreen,true);yield return Capture("Map_Default_1080x1920");
            var node=root.nodes.First(n=>n.stageId=="SW01");node.button.onClick.Invoke();yield return null;yield return null;
            yield return Capture("Map_Selected_Numerals");
            root.SetReducedMotion(false);Focus(root);Focus(Scope(root.stageScreen));
            var sprites=new HashSet<Sprite>();float until=Time.unscaledTime+1.1f;
            while(Time.unscaledTime<until){sprites.Add(node.star.sprite);yield return null;}
            Assert.That(sprites.Count,Is.EqualTo(4),"Selected star lost its separate four-frame rotation");
            ValidBindings(root.stageScreen,false);
            root.SetReducedMotion(true);yield return null;ValidBindings(root.stageScreen,true);
            foreach(var protectedRoot in new[]{root.mainScreen,root.settingsScreen,root.modal})Assert.That(Bindings(protectedRoot),Is.Empty);
        }
        [UnityTest] public IEnumerator ActualHudPrefabPreservesTurnsStarsGaugePauseAndClearAtPortraitAndWideSizes()
        {
            Resolution(1080,1920);yield return Load();
            var asset=((NotebookProjectGateway)root.gateway).gameplayCanvasPrefab;
            Assert.That(AssetDatabase.GetAssetPath(asset),Is.EqualTo("Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab"));
            hudObject=Object.Instantiate(asset);var hud=hudObject.GetComponentInChildren<NotebookGameplayHud>(true);
            root.GetComponentInParent<Canvas>().rootCanvas.gameObject.SetActive(false);
            var gateway=hudObject.AddComponent<MapHudCrayonTestGateway>();hud.BindGateway(gateway);hud.SetReducedMotion(true);
            yield return null;Focus(Scope(hud.gameObject));
            foreach(int value in new[]{0,9,10,17,99,100})
            {
                hud.SetCost(value);yield return null;Assert.That(hud.costNumber.Value,Is.EqualTo(value));
                var shown=hud.costNumber.places.Where(p=>p.gameObject.activeSelf).ToArray();string expected=value.ToString();
                Assert.That(shown.Length,Is.EqualTo(expected.Length));
                for(int i=0;i<shown.Length;i++)Assert.That(shown[i].sprite,Is.SameAs(hud.costNumber.digits[expected[i]-'0']));
                if(value==17||value==100)yield return Capture("HUD_Turns_"+value);
            }
            for(int required=1;required<=4;required++)
            {
                hud.BeginStage("skin","스킨 검수",17,0,required);
                for(int collected=0;collected<=required;collected++)
                {
                    hud.SynchronizeCollected(collected);yield return null;
                    Assert.That(hud.RequiredStars,Is.EqualTo(required));Assert.That(hud.DisplayedCollected,Is.EqualTo(collected));
                    Assert.That(hud.jarImage.sprite,Is.SameAs(hud.jarStates.Single(s=>s.required==required&&s.collected==collected).sprite));
                    ValidBindings(hud.gameObject,true);
                }
                yield return Capture("HUD_Target_"+required);
            }
            for(int remaining=0;remaining<=5;remaining++)
            {
                hud.BeginStage("skin","스킨 검수",17,0,4);
                Assert.That(hud.SynchronizeRetryGauge(new NotebookGameplayRetryGaugeSnapshot{sessionId="skin-"+remaining,remaining=remaining,revision=0}),Is.True);
                yield return null;
                Assert.That(hud.RetryGaugeRemaining,Is.EqualTo(remaining));Assert.That(hud.restartImage.sprite,Is.SameAs(hud.retryGaugeSprites[remaining]));
                Assert.That(hud.restartButton.interactable,Is.EqualTo(remaining>0));
                var press=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(hud.restartButton.gameObject,press,ExecuteEvents.pointerDownHandler);yield return null;
                if(remaining>0)Assert.That(hud.restartImage.overrideSprite,Is.SameAs(hud.retryGaugePressedSprites[remaining]));
                yield return Capture("HUD_Telescope_"+remaining);
                ExecuteEvents.Execute(hud.restartButton.gameObject,press,ExecuteEvents.pointerUpHandler);
            }
            hud.RequestPaused(true);yield return new WaitForSecondsRealtime(.25f);Assert.That(hud.IsPaused,Is.True);yield return Capture("Protected_Pause");
            hud.RequestPaused(false);yield return null;Assert.That(hud.IsPaused,Is.False);
            hud.SynchronizeCollected(4);
            Assert.That(hud.ShowVerifiedClear(new NotebookGameplayClearSnapshot{stageId="skin",stageLabel="스킨 검수",verified=true,exitReached=true,collectedStars=4,requiredStars=4}),Is.True);
            yield return new WaitForSecondsRealtime(.6f);yield return Capture("Protected_Clear");
            foreach(var panel in new[]{hud.pauseOverlay,hud.clearOverlay,hud.modalBackdrop})Assert.That(Bindings(panel),Is.Empty);
            hud.BeginStage("skin","스킨 검수",17,2,4);
            Resolution(1440,900);yield return null;yield return null;yield return Capture("HUD_PC_1440x900");
            Resolution(390,844);yield return null;yield return null;yield return Capture("HUD_Portrait_390x844");
        }
        [UnityTest] public IEnumerator ActualMapKeepsLockedUnownedAndReplayStatesWithoutChangingRecords()
        {
            yield return Load();root.SetReducedMotion(true);
            var model=(NotebookBmModel)typeof(NotebookRoot).GetField("model",Flags).GetValue(root);
            var cleared=model.State.clearedStageIds.ToArray();var owned=model.State.ownedThemes.ToArray();
            var entries=model.State.entryIds.ToArray();var rewards=model.State.rewardIds.ToArray();int balance=model.State.balance;
            Action refresh=()=>typeof(NotebookRoot).GetMethod("RefreshMap",Flags).Invoke(root,null);
            try
            {
                // Fixture changes only the loaded UI model; teardown restores saved preferences/wallet.
                model.State.clearedStageIds.Clear();model.State.ownedThemes.Clear();root.OpenStages();refresh();
                var locked=root.nodes.Single(n=>n.stageId=="SW02");locked.button.onClick.Invoke();yield return null;
                Assert.That(locked.locked.gameObject.activeSelf,Is.True);Assert.That(locked.number.gameObject.activeSelf,Is.False);
                Assert.That(root.launchButton.interactable,Is.False);Assert.That(root.storyReplayButton.gameObject.activeSelf,Is.False);
                root.nodes.Single(n=>n.stageId=="ET-01").button.onClick.Invoke();yield return null;
                Assert.That(root.launchButton.interactable,Is.True);root.launchButton.onClick.Invoke();yield return null;
                Assert.That(root.modal.activeSelf,Is.True);Assert.That(root.modalPrimary.interactable,Is.False);
                Assert.That(Bindings(root.modal),Is.Empty);root.modalClose.onClick.Invoke();
                model.State.clearedStageIds.Add("SW01");refresh();root.nodes.Single(n=>n.stageId=="SW01").button.onClick.Invoke();yield return null;
                Assert.That(root.storyReplayButton.gameObject.activeSelf,Is.True);
                Assert.That(AssetDatabase.GetAssetPath(root.stageRegion.font),Does.Not.StartWith("Assets/ANIMOL/CrayonNumerals/"));
                Assert.That(model.State.balance,Is.EqualTo(balance));CollectionAssert.AreEqual(entries,model.State.entryIds);
                CollectionAssert.AreEqual(rewards,model.State.rewardIds);ValidBindings(root.stageScreen,true);
            }
            finally
            {
                model.State.clearedStageIds.Clear();model.State.clearedStageIds.AddRange(cleared);
                model.State.ownedThemes.Clear();model.State.ownedThemes.AddRange(owned);refresh();
            }
        }
    }
}
#endif
