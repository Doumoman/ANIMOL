using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ANIMOL.AnimalMultiplayerPhase3;
using ANIMOL.AnimalMultiplayerPhase3.Tests;
using ANIMOL.AnimalUiV2;
using ANIMOL.PortraitArtV1;
using ANIMOL.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace ANIMOL.MissingUiV1.Project.Multiplayer.Tests
{
    public sealed class MissingMultiplayerFlowTests
    {
        private UiNavigationService nav;
        private AnimalMultiplayerPhase3Host host;
        private MultiplayerUiPresenter legacy;
        private string originalClipboard,originalLegacy;
        private int language,large;private bool hadLanguage,hadLarge,background;
        private int waitingTransitions;
        private Dictionary<string,byte[]> saveFiles;
        private MissingMultiplayerView View(MissingMultiplayerScreen s)=>nav.GetComponentsInChildren<MissingMultiplayerView>(true).Single(v=>v.Screen==s);
        private const string Evidence="Docs/MissingUiV1Task3/";
        [UnitySetUp] public IEnumerator Setup()
        {
            originalClipboard=GUIUtility.systemCopyBuffer;background=Application.runInBackground;Application.runInBackground=true;
            hadLanguage=PlayerPrefs.HasKey("ANIMOL.M6.Language");language=PlayerPrefs.GetInt("ANIMOL.M6.Language");
            hadLarge=PlayerPrefs.HasKey("ANIMOL.M6.LargeText");large=PlayerPrefs.GetInt("ANIMOL.M6.LargeText");
            saveFiles=GameSaveFiles().ToDictionary(p=>p,File.ReadAllBytes);
            SceneManager.LoadScene("Lobby");yield return Settle();
            nav=Object.FindFirstObjectByType<UiNavigationService>();host=nav.GetComponent<AnimalMultiplayerPhase3Entry>().Host;
            legacy=nav.GetComponent<MultiplayerUiPresenter>();originalLegacy=JsonUtility.ToJson(legacy);
            nav.GetComponentInChildren<PortraitEntryController>(true).ModeScreen.Competition.onClick.Invoke();yield return Settle();
            Assert.That(nav.CurrentScreenId,Is.EqualTo("SC05_CompetitiveHub"));Assert.IsTrue(View(MissingMultiplayerScreen.Hub).isActiveAndEnabled);
            waitingTransitions=0;nav.ScreenChanged+=Track;
        }
        [UnityTearDown] public IEnumerator Restore()
        {
            nav.ScreenChanged-=Track;GUIUtility.systemCopyBuffer=originalClipboard;Application.runInBackground=background;
            if(hadLanguage)PlayerPrefs.SetInt("ANIMOL.M6.Language",language);else PlayerPrefs.DeleteKey("ANIMOL.M6.Language");
            if(hadLarge)PlayerPrefs.SetInt("ANIMOL.M6.LargeText",large);else PlayerPrefs.DeleteKey("ANIMOL.M6.LargeText");
            PlayerPrefs.Save();MissingUtilityView.ApplyPreferences();
            CollectionAssert.AreEquivalent(saveFiles.Keys,GameSaveFiles());
            foreach(var f in saveFiles)CollectionAssert.AreEqual(f.Value,File.ReadAllBytes(f.Key),f.Key);
            yield return null;
        }
        private void Track(string id){if(id=="SC06_MatchRoom")waitingTransitions++;}
        private static IEnumerable<string> GameSaveFiles()=>Directory.GetFiles(Application.persistentDataPath,"*",SearchOption.AllDirectories).Where(p=>
        {
            var relative=Path.GetRelativePath(Application.persistentDataPath,p).Replace('\\','/');
            return !(relative.StartsWith("Unity/",StringComparison.Ordinal)&&(relative.Contains("/Editor/Analytics/")||relative.Contains("/Insights/")));
        });
        [UnityTest] public IEnumerator ActualHubCustomAndExistingPhase3ReturnWithoutRoomEntry()
        {
            var hub=View(MissingMultiplayerScreen.Hub);
            foreach(var button in new[]{hub.NormalBrowse,hub.RankedBrowse})
            {
                button.onClick.Invoke();yield return Settle();AssertBrowseOnly();host.Presenter.View.Back.onClick.Invoke();yield return Settle();
                Assert.That(nav.CurrentScreenId,Is.EqualTo("SC05_CompetitiveHub"));Assert.That(host.LastReturnedLoadout.Fingerprint(),Is.EqualTo("||"));
            }
            hub.Custom.onClick.Invoke();yield return Settle();View(MissingMultiplayerScreen.Custom).Create.onClick.Invoke();yield return Settle();
            Assert.That(nav.CurrentScreenId,Is.EqualTo(MissingMultiplayerView.ScreenId(MissingMultiplayerScreen.Create)));
            var create=View(MissingMultiplayerScreen.Create);Assert.IsFalse(create.Entry.interactable);create.Entry.onClick.Invoke();
            create.Browse.onClick.Invoke();yield return Settle();AssertBrowseOnly();host.Presenter.View.Back.onClick.Invoke();yield return Settle();
            Assert.That(nav.CurrentScreenId,Is.EqualTo(MissingMultiplayerView.ScreenId(MissingMultiplayerScreen.Create)));
            create.Back.onClick.Invoke();View(MissingMultiplayerScreen.Custom).Back.onClick.Invoke();hub.Back.onClick.Invoke();
            Assert.That(nav.CurrentScreenId,Is.EqualTo(PortraitEntryController.ModeId));Assert.That(waitingTransitions,Is.Zero);
            Assert.That(legacy.PreviewParticipantCount,Is.Zero);Assert.That(JsonUtility.ToJson(legacy),Is.EqualTo(originalLegacy));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }
        private void AssertBrowseOnly()
        {
            Assert.That(nav.CurrentScreenId,Is.EqualTo(AnimalMultiplayerPhase3Host.ScreenId));
            Assert.That(host.Presenter.Backend,Is.TypeOf<AnimalMultiplayerProjectAdapter>());
            Assert.IsNull(host.Context.ModeId);Assert.IsNull(host.Context.EntryIntent);Assert.IsNull(host.Context.Requirements.PolicyRevision);
            Assert.That(host.Presenter.Draft.Fingerprint(),Is.EqualTo("||"));Assert.IsFalse(host.Presenter.View.Primary.interactable);
            host.Presenter.View.Primary.onClick.Invoke();Assert.IsFalse(host.Presenter.View.Modal.activeSelf);
        }
        [UnityTest] public IEnumerator CodeInputPasteAndSubmitHaveNoInventedFormatOrEntry()
        {
            View(MissingMultiplayerScreen.Hub).Custom.onClick.Invoke();View(MissingMultiplayerScreen.Custom).Join.onClick.Invoke();yield return Settle();
            var join=View(MissingMultiplayerScreen.Join);var field=join.Code.Field;
            Assert.That(field.characterLimit,Is.Zero);Assert.That(field.characterValidation,Is.EqualTo(TMP_InputField.CharacterValidation.None));
            Assert.That(field.lineType,Is.EqualTo(TMP_InputField.LineType.SingleLine));
#if UNITY_EDITOR
            // TMP's public getter returns true on desktop regardless of the mobile configuration.
            Assert.IsFalse(new UnityEditor.SerializedObject(field).FindProperty("m_HideSoftKeyboard").boolValue);
            Assert.IsFalse(new UnityEditor.SerializedObject(field).FindProperty("m_HideMobileInput").boolValue);
#endif
            GUIUtility.systemCopyBuffer="";join.Paste.onClick.Invoke();Assert.That(join.CodeNotice.Korean,Does.Contain("없습니다"));
            const string code="Invitation-초대_自由-abc/1234567890-Z";
            GUIUtility.systemCopyBuffer=code;join.Paste.onClick.Invoke();yield return Settle();Assert.That(field.text,Is.EqualTo(code));
            Assert.IsTrue(field.isFocused);Assert.That(field.textComponent.GetComponentInParent<TMP_InputField>(),Is.SameAs(field));
            field.onSubmit.Invoke(field.text);join.Lookup.onClick.Invoke();join.Entry.onClick.Invoke();yield return Settle();
            Assert.IsFalse(join.Lookup.interactable);Assert.IsFalse(join.Entry.interactable);Assert.That(waitingTransitions,Is.Zero);
            Assert.That(nav.CurrentScreenId,Is.EqualTo(MissingMultiplayerView.ScreenId(MissingMultiplayerScreen.Join)));
            Assert.That(MissingStoreView.Find<Text>(join.transform,"UnknownRoom").text,Does.Contain("--"));
            Assert.IsFalse(MissingStoreView.Find<Text>(join.transform,"UnknownRoom").text.Contains(code));
            join.Back.onClick.Invoke();Assert.IsFalse(field.isFocused);Assert.That(legacy.PreviewParticipantCount,Is.Zero);
        }
        [UnityTest] public IEnumerator FourUnknownSlotsCannotReadyStartInviteCopyOrLeave()
        {
            // Inspection only: production hub deliberately has no route to an unapproved room.
            nav.Navigate("SC06_MatchRoom");yield return Settle();var room=View(MissingMultiplayerScreen.Room);
            Assert.IsTrue(room.isActiveAndEnabled);Assert.That(room.ParticipantSlots.Length,Is.EqualTo(4));
            foreach(var slot in room.ParticipantSlots)Assert.That(slot.GetComponentInChildren<MissingUtilityText>().Korean,Does.Contain("--"));
            foreach(var b in new[]{room.Ready,room.StartMatch,room.Invite,room.Copy,room.Leave}){Assert.IsFalse(b.interactable);b.onClick.Invoke();}
            Assert.That(nav.CurrentScreenId,Is.EqualTo("SC06_MatchRoom"));Assert.That(legacy.PreviewParticipantCount,Is.Zero);
            Assert.That(GUIUtility.systemCopyBuffer,Is.EqualTo(originalClipboard));
            foreach(var label in room.GetComponentsInChildren<Text>())Assert.That(label.text,Does.Not.Contain("DEV-P").And.Not.Contain("4~8").And.Not.Contain("8명"));
            room.Back.onClick.Invoke();Assert.That(nav.CurrentScreenId,Is.EqualTo("SC05_CompetitiveHub"));
        }
        [UnityTest] public IEnumerator ReentryInstallerAndModalDoNotDuplicateOrBypassNavigation()
        {
            var hub=View(MissingMultiplayerScreen.Hub);var modals=nav.GetComponent<UiModalStack>();modals.Push("ServiceErrorModal");
            hub.Custom.onClick.Invoke();hub.NormalBrowse.onClick.Invoke();hub.Back.onClick.Invoke();Assert.That(nav.CurrentScreenId,Is.EqualTo("SC05_CompetitiveHub"));modals.Pop();
            for(int i=0;i<3;i++)
            {
                MissingMultiplayerEntry.Install(SceneManager.GetActiveScene(),LoadSceneMode.Single);hub.Custom.onClick.Invoke();yield return Settle();
                View(MissingMultiplayerScreen.Custom).Back.onClick.Invoke();Assert.That(nav.CurrentScreenId,Is.EqualTo("SC05_CompetitiveHub"));
            }
            Assert.That(nav.GetComponents<MissingMultiplayerEntry>().Length,Is.EqualTo(1));Assert.That(nav.GetComponentsInChildren<MissingMultiplayerView>(true).Length,Is.EqualTo(5));
            Assert.That(nav.GetComponentsInChildren<AnimalMultiplayerPhase3Host>(true).Length,Is.EqualTo(1));Assert.That(waitingTransitions,Is.Zero);
            foreach(var button in hub.GetComponentsInChildren<Button>(true))Assert.That(button.GetComponents<UiButtonFeedback>().Length,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator FixturePendingUnknownCannotReenterFromNewUi()
        {
            var p=host.Presenter;var fixture=host.gameObject.AddComponent<MultiplayerTransactionFixture>();fixture.Catalog=p.Catalog;fixture.ReconciliationAvailable=false;
            var context=new MultiplayerSelectionRequest{ModeId="TEST_ONLY_MODE",DisplayName="TEST ONLY",EntryIntent="TEST_ONLY_INTENT",InitialLoadout=new AnimalLoadout{Ground="Rabbit",Special="DreamFox",Air="Swallow"},Requirements=new SelectionRequirements{RequiredRoles=new[]{AnimalRole.Ground,AnimalRole.Special,AnimalRole.Air},RepresentativeRole=AnimalRole.Ground,PolicyRevision="TEST_POLICY"}};
            fixture.OnRead=c=>
            {
                var s=new AnimalUiSnapshot{Revision="TEST_READ",ContextId=c.ModeId,EntryIntent=c.EntryIntent,PolicyRevision=c.Requirements.PolicyRevision};
                foreach(var a in p.Catalog.Animals)s.Animals.Add(new AnimalProgress{AnimalId=a.Id,Implemented=true,Unlocked=false,HasContextPermission=true,CanUseInContext=true,ActiveLevel=1,PassiveLevel=2,ActiveDescription="TEST ACTIVE",PassiveDescription="TEST PASSIVE"});
                return Task.FromResult(s);
            };
            var pending=new TaskCompletionSource<AnimalUiCommitResult>();fixture.OnSubmit=_=>pending.Task;p.SetBackend(fixture);
            Assert.IsTrue(host.Open(context));yield return Settle();
            p.View.Primary.onClick.Invoke();p.View.ModalCancel.onClick.Invoke();Assert.That(fixture.Requests.Count,Is.Zero);
            p.View.Primary.onClick.Invoke();p.View.ModalConfirm.onClick.Invoke();yield return Settle();Assert.IsTrue(p.IsCommitting);
            for(int i=0;i<4;i++){p.View.ModalConfirm.onClick.Invoke();p.Back();View(MissingMultiplayerScreen.Hub).NormalBrowse.onClick.Invoke();View(MissingMultiplayerScreen.Create).Entry.onClick.Invoke();}
            Assert.IsFalse(host.OpenUnconfigured());Assert.That(fixture.Requests.Count,Is.EqualTo(1));Assert.That(waitingTransitions,Is.Zero);
            var original=JsonUtility.ToJson(fixture.Requests.Single());pending.SetResult(new AnimalUiCommitResult{Status=CommitStatus.Unknown,Message="TEST timeout"});yield return Settle();
            host.gameObject.SetActive(false);yield return null;host.gameObject.SetActive(true);yield return Settle();
            p.View.ModalConfirm.onClick.Invoke();yield return Settle();Assert.IsTrue(p.IsCommitting);Assert.IsFalse(host.OpenUnconfigured());
            Assert.That(fixture.Requests.Count,Is.EqualTo(1));Assert.That(JsonUtility.ToJson(fixture.Requests.Single()),Is.EqualTo(original));Assert.That(waitingTransitions,Is.Zero);
            Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"fixture-pending-unknown.json",JsonUtility.ToJson(new TransactionEvidence{Scope="Unity test fixture only; no operational service",ProtectedEntryCalls=fixture.Requests.Count,WaitingRoomTransitions=waitingTransitions,Request=original,Unknown=p.IsCommitting},true));
        }
        [Serializable] private sealed class TransactionEvidence{public string Scope,Request;public int ProtectedEntryCalls,WaitingRoomTransitions;public bool Unknown;}
        [UnityTest] public IEnumerator ExistingCoopDevelopmentRouteRemainsSeparate()
        {
            nav.Navigate("SC08_CoopHub");yield return Settle();
            var dev=nav.GetComponentsInChildren<Button>(true).Single(b=>b.name=="CoopDevPreviewButton");dev.onClick.Invoke();yield return Settle();
            Assert.That(nav.CurrentScreenId,Is.EqualTo("SC06_MatchRoom"));Assert.That(legacy.PreviewParticipantCount,Is.EqualTo(2));Assert.IsFalse(View(MissingMultiplayerScreen.Room).isActiveAndEnabled);
            var toggle=nav.GetComponentsInChildren<Button>(true).Single(b=>b.name=="RoomParticipantCountToggleButton");toggle.onClick.Invoke();yield return Settle();Assert.That(legacy.PreviewParticipantCount,Is.EqualTo(4));
        }
        [UnityTest] public IEnumerator GameView1920()=>Survey(1920);
        [UnityTest] public IEnumerator GameView2400()=>Survey(2400);
        private IEnumerator Survey(int height)
        {
#if UNITY_EDITOR
            var setup=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t=>t!=null);
            setup.GetMethod("SelectFixedResolution").Invoke(null,new object[]{1080,height,"MissingUI Task3 QA"});
            var window=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.Show();window.Focus();yield return Settle();
            Assert.That(Screen.width,Is.EqualTo(1080));Assert.That(Screen.height,Is.EqualTo(height));
            var safe=nav.GetComponentInChildren<SafeAreaLayout>();var probe=safe.gameObject.AddComponent<MissingMultiplayerSafeProbe>();probe.enabled=false;
            foreach(bool inset in new[]{false,true})
            {
                probe.enabled=false;safe.enabled=true;safe.Apply();
                if(inset){safe.enabled=false;probe.Minimum=new Vector2(48f/1080,96f/height);probe.Maximum=new Vector2(1-48f/1080,1-120f/height);probe.enabled=true;}
                MobileControlPreferences.LargeText=inset;MobileControlPreferences.Language=inset?UiLanguage.English:UiLanguage.Korean;MissingUtilityView.ApplyPreferences();
                foreach(MissingMultiplayerScreen kind in Enum.GetValues(typeof(MissingMultiplayerScreen)))
                {
                    nav.Navigate(MissingMultiplayerView.ScreenId(kind),false);yield return Settle();var v=View(kind);v.Body.StopMovement();v.Body.verticalNormalizedPosition=1;yield return Settle();
                    if(v.Code!=null){v.Code.Field.text="TEST ONLY / Long invitation code 1234567890 / 초대 입력";v.Code.Field.DeactivateInputField();}
                    string prefix=height+"_"+(inset?"safe_english_large_":"full_korean_")+kind;
                    yield return Capture(prefix+"_top",inset,probe);v.Body.verticalNormalizedPosition=0;yield return Settle();yield return Capture(prefix+"_bottom",inset,probe);
                    AssertInside((RectTransform)v.Back.transform,(RectTransform)safe.transform);
                    var e=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,v.Back.transform.position)};
                    var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);Assert.IsNotEmpty(hits);Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.SameAs(v.Back.gameObject));
                    v.Body.verticalNormalizedPosition=1;e.scrollDelta=new Vector2(0,-20);ExecuteEvents.Execute(v.Body.gameObject,e,ExecuteEvents.scrollHandler);yield return Settle();
                    if(v.Body.content.rect.height>v.Body.viewport.rect.height+1)Assert.That(v.Body.verticalNormalizedPosition,Is.LessThan(1));
                }
            }
            Object.Destroy(probe);safe.enabled=true;safe.Apply();
#else
            Assert.Ignore("Editor Game View required");yield break;
#endif
        }
        private static IEnumerator Capture(string name,bool inset,MissingMultiplayerSafeProbe probe)
        {
            yield return new WaitForEndOfFrame();
            if(inset){var r=(RectTransform)probe.transform;Assert.That(r.anchorMin,Is.EqualTo(probe.Minimum));Assert.That(r.anchorMax,Is.EqualTo(probe.Maximum));}
            var image=ScreenCapture.CaptureScreenshotAsTexture();Directory.CreateDirectory(Evidence+"Captures");File.WriteAllBytes(Evidence+"Captures/"+name+".png",image.EncodeToPNG());Object.Destroy(image);
        }
        private static IEnumerator Settle(){for(int i=0;i<12;i++)yield return null;Canvas.ForceUpdateCanvases();}
        private static void AssertInside(RectTransform child,RectTransform parent)
        {
            var corners=new Vector3[4];child.GetWorldCorners(corners);
            foreach(var p in corners){var v=parent.InverseTransformPoint(p);Assert.That(v.x,Is.InRange(parent.rect.xMin-1,parent.rect.xMax+1));Assert.That(v.y,Is.InRange(parent.rect.yMin-1,parent.rect.yMax+1));}
        }
    }
    [DefaultExecutionOrder(31000)] public sealed class MissingMultiplayerSafeProbe:MonoBehaviour
    {
        public Vector2 Minimum,Maximum;
        private void LateUpdate(){var r=(RectTransform)transform;r.anchorMin=Minimum;r.anchorMax=Maximum;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
