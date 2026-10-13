using System.Collections;
using System.IO;
using System.Linq;
using ANIMOL.AnimalUiV2;
using ANIMOL.ProductionV1;
using ANIMOL.Typography;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ANIMOL.AnimalStagePhase2.Tests
{
    public sealed class StagePhase2FlowTests
    {
        private UiNavigationService nav;
        private ProductionController source;
        private AnimalStagePhase2Host host;
        private AnimalUiPresenter P => host.Presenter;
        private Button Button(string name) => nav.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
        [UnitySetUp] public IEnumerator Lobby()
        {
            SceneManager.LoadScene("Lobby");
            yield return null; yield return null; yield return null; yield return null;
            nav = Object.FindFirstObjectByType<UiNavigationService>();
            source = nav.GetComponent<ProductionController>();
            host = nav.GetComponentInChildren<AnimalStagePhase2Host>(true);
            Assert.NotNull(host);
        }
        private IEnumerator Open(int theme)
        {
            nav.Navigate("SC02_ThemeSelect"); Button("PV1_Theme_" + theme).onClick.Invoke(); yield return null;
            Assert.That(nav.CurrentScreenId, Is.EqualTo("SC03_StageSelect"));
            Button("PV1_Stage_0").onClick.Invoke(); yield return null; yield return null;
            Assert.That(nav.CurrentScreenId, Is.EqualTo(AnimalStagePhase2Host.ScreenId));
        }
        [UnityTest] public IEnumerator ActualThemesContextAndReturnNeverLaunch()
        {
            foreach (int theme in Enumerable.Range(0, 5))
            {
                yield return Open(theme);
                var stage = source.SelectedStage;
                Assert.That(host.Context.StageId, Is.EqualTo(stage.StageId));
                Assert.That(host.Context.Policy, Is.EqualTo(CampaignSelectionPolicy.Fixed));
                Assert.That(host.Context.Requirements.RequiredRoles, Is.EquivalentTo(new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air }));
                Assert.That(P.View.CampaignBackground, Is.SameAs(source.Catalog.Find("BG_UI_Campaign_" + stage.ThemeId)));
                Assert.NotNull(P.View.CampaignBackground);
                Assert.That(P.View.Background.sprite, Is.SameAs(P.View.CampaignBackground));
                Assert.That(P.Draft.Fingerprint(), Is.EqualTo(theme == 0 ? "Rabbit||" : "||"));
                Assert.That(P.Backend, Is.TypeOf<AnimalStageProjectAdapter>());
                Assert.IsFalse(P.View.Primary.interactable);
                P.View.Primary.onClick.Invoke();
                Assert.IsFalse(P.View.Modal.activeSelf);
                Assert.That(source.LoadRequests, Is.Zero);
                Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.IsNull(Object.FindFirstObjectByType<AnimalStagePhase2DemoHost>());
                Assert.IsNull(Object.FindFirstObjectByType<AnimalUiDemoSwitcher>());
                yield return new WaitForSecondsRealtime(.5f);
                string output = Path.Combine(Path.GetTempPath(), "ANIMOL-StagePhase2-Captures"); Directory.CreateDirectory(output);
                yield return Capture(Path.Combine(output, "actual_" + stage.StageId + ".png"));
                P.View.Back.onClick.Invoke(); yield return null;
                Assert.That(nav.CurrentScreenId, Is.EqualTo("SC03_StageSelect"));
                Assert.IsTrue(source.StageScroll.gameObject.activeInHierarchy);
            }
        }
        [UnityTest] public IEnumerator AllFifteenAtBothGameViewsAndSafeArea()
        {
#if UNITY_EDITOR
            yield return Open(0);
            yield return new WaitForSecondsRealtime(.5f);
            var setup = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t => t != null);
            string output = Path.Combine(Path.GetTempPath(), "ANIMOL-StagePhase2-Captures"); Directory.CreateDirectory(output);
            string stageBefore = JsonUtility.ToJson(source.SelectedStage);
            string mapBefore = JsonUtility.ToJson(host.IdMap);
            string draft = P.Draft.Fingerprint();
            var v = P.View;
            foreach (int height in new[] { 1920, 2400 })
            {
                setup.GetMethod("SelectFixedResolution").Invoke(null, new object[] { 1080, height, "ANIMOL Phase2 QA" });
                yield return null; yield return null;
                Assert.That(Screen.width, Is.EqualTo(1080)); Assert.That(Screen.height, Is.EqualTo(height));
                var safe = nav.GetComponentInChildren<SafeAreaLayout>();
                foreach (bool inset in new[] { false, true })
                {
                    safe.enabled = true; safe.Apply();
                    if (inset)
                    {
                        safe.enabled = false; var rect = (RectTransform)safe.transform;
                        rect.anchorMin = new Vector2(48f / 1080, 96f / height);
                        rect.anchorMax = new Vector2(1 - 48f / 1080, 1 - 120f / height);
                    }
                    yield return null; yield return null; yield return null;
                    Canvas.ForceUpdateCanvases();
                    string prefix = height + (inset ? "_safe" : "_full");
                    var footer = (RectTransform)v.Primary.transform;
                    var footerBefore = footer.position;
                    foreach (AnimalRole role in System.Enum.GetValues(typeof(AnimalRole)))
                    {
                        v.RoleTabs[(int)role].onClick.Invoke(); yield return null;
                        var cards = v.RosterContent.GetComponentsInChildren<AnimalCardView>().Where(c => c.gameObject.activeInHierarchy).ToArray();
                        Assert.That(cards.Length, Is.EqualTo(5)); Assert.IsFalse(v.RosterScroll.enabled);
                        foreach (var animal in P.Catalog.ForRole(role))
                        {
                            var card = cards.Single(c => c.Name.text == animal.DisplayName);
                            card.Button.onClick.Invoke(); v.BodyScroll.verticalNormalizedPosition = 1;
                            yield return null; yield return null;
                            Assert.That(P.InspectedAnimalId, Is.EqualTo(animal.Id));
                            Assert.That(P.Draft.Fingerprint(), Is.EqualTo(draft));
                            Assert.That(host.Context.FixedLoadout.Fingerprint(), Is.EqualTo(draft));
                            Assert.That(v.HeroPortrait.sprite, Is.SameAs(animal.Portrait));
                            Assert.That(card.Portrait.sprite, Is.SameAs(animal.Portrait));
                            Assert.IsTrue(v.HeroPortrait.preserveAspect && card.Portrait.preserveAspect);
                            AssertContained(v.HeroPortrait.rectTransform, v.BodyScroll.viewport);
                            AssertContained(card.Portrait.rectTransform, v.RosterScroll.viewport);
                            Assert.IsNull(card.Portrait.GetComponent<Mask>());
                            Assert.That(v.HeroDescription.text, Does.Contain("액티브: --").And.Contain("패시브: --"));
                            Assert.IsFalse(v.Primary.interactable);
                            foreach (var bridge in P.GetComponentsInChildren<PixelTextBridge>())
                                Assert.IsFalse(bridge.Overflow, animal.Id + "/" + bridge.name + ": " + bridge.Source.text);
                            yield return Capture(Path.Combine(output, prefix + "_" + animal.Id + ".png"));
                        }
                        v.BodyScroll.verticalNormalizedPosition = 0; yield return null; yield return null;
                        AssertContained(v.RosterPanel, v.BodyScroll.viewport);
                        yield return Capture(Path.Combine(output, prefix + "_cards_" + role + ".png"));
                        // Real EventSystem drag routing from a visible card must reach the outer scroll.
                        var point = RectTransformUtility.WorldToScreenPoint(null, cards[0].Portrait.rectTransform.TransformPoint(cards[0].Portrait.rectTransform.rect.center));
                        var pointer = new PointerEventData(EventSystem.current) { position = point };
                        var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                        Assert.IsNotEmpty(hits);
                        var drag = ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject);
                        Assert.That(drag, Is.SameAs(v.BodyScroll.gameObject));
                        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.initializePotentialDrag);
                        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.beginDragHandler);
                        var scrollBefore = v.BodyContent.anchoredPosition;
                        pointer.position -= new Vector2(0, 200);
                        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.dragHandler);
                        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.endDragHandler);
                        Assert.That(v.BodyContent.anchoredPosition.y, Is.LessThan(scrollBefore.y));
                        v.BodyScroll.StopMovement();
                        Assert.That(footer.position, Is.EqualTo(footerBefore));
                    }
                    AssertContained(footer, (RectTransform)safe.transform);
                    v.BodyScroll.verticalNormalizedPosition = 1; yield return null;
                    v.SelectionDetailsButton.onClick.Invoke(); yield return null;
                    Assert.IsTrue(v.Modal.activeInHierarchy);
                    Assert.That(v.ModalBody.text, Does.Contain("액티브").And.Contain("패시브").And.Contain("설정 대기"));
                    yield return Capture(Path.Combine(output, prefix + "_modal.png"));
                    v.ModalCancel.onClick.Invoke(); yield return null;
                    Assert.IsFalse(v.Modal.activeSelf);
                }
                safe.enabled = true; safe.Apply();
            }
            Assert.That(JsonUtility.ToJson(source.SelectedStage), Is.EqualTo(stageBefore));
            Assert.That(JsonUtility.ToJson(host.IdMap), Is.EqualTo(mapBefore));
            Assert.That(P.Draft.Fingerprint(), Is.EqualTo(draft));
            Assert.That(source.LoadRequests, Is.Zero);
#else
            Assert.Ignore("Requires Editor Game View."); yield break;
#endif
        }
        [UnityTest] public IEnumerator FixtureOnlyFullRosterConfirmationAndLongDescriptions()
        {
            yield return Open(0); yield return new WaitForSecondsRealtime(.5f);
            host.enabled = false;
            var backend = host.gameObject.AddComponent<StagePhase2PresentationFixture>();
            backend.Snapshot = new AnimalUiSnapshot { Revision = "TEST_ONLY", ContextId = "TEST_ONLY", PolicyRevision = "TEST_ONLY" };
            string effect = string.Join("\n", Enumerable.Repeat("실제 서비스 데이터가 아닌 장문 표시 검사 문자열입니다. 액티브와 패시브 설명을 끝까지 확인합니다.", 20));
            foreach (var animal in P.Catalog.Animals)
                backend.Snapshot.Animals.Add(new AnimalProgress { AnimalId = animal.Id, Implemented = true, Unlocked = true,
                    HasContextPermission = true, CanUseInContext = true, ActiveLevel = int.MaxValue, PassiveLevel = 7,
                    ActiveDescription = effect, PassiveDescription = "독립 패시브 검사" });
            P.SetBackend(backend);
            var context = AnimalStagePhase2DemoHost.CreateReadonlyFixture();
            context.StageId = "TEST_ONLY"; context.DisplayName = "표시 검사 전용 · 실제 스테이지 아님";
            context.Requirements.PolicyRevision = "TEST_ONLY";
            P.OpenCampaign(context); yield return null; yield return null;
            P.View.RosterContent.GetComponentsInChildren<AnimalCardView>().Single(c => c.Name.text == P.Catalog.Find("Wolf").DisplayName).Button.onClick.Invoke();
            Assert.That(P.InspectedAnimalId, Is.EqualTo("Wolf"));
            Assert.That(P.Draft.Fingerprint(), Is.EqualTo("Rabbit|DreamFox|Swallow"));
            P.View.SelectionDetailsButton.onClick.Invoke(); yield return null; yield return null;
            Assert.That(P.View.ModalBody.text, Does.Contain(effect).And.Contain(int.MaxValue.ToString()).And.Contain("독립 패시브 검사"));
            var scroll = P.View.ModalBodyScroll;
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            scroll.StopMovement(); scroll.verticalNormalizedPosition = 0; yield return null; yield return null;
            Assert.That(scroll.content.anchoredPosition.y, Is.GreaterThan(0));
            var modalTypography = P.View.ModalBody.GetComponent<PixelTextBridge>();
            modalTypography.Synchronize();
            Assert.IsFalse(modalTypography.Overflow, "Full modal text must fit its scroll content, including the final passive description.");
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0).Within(.01f));
            string output = Path.Combine(Path.GetTempPath(), "ANIMOL-StagePhase2-Captures"); Directory.CreateDirectory(output);
            yield return Capture(Path.Combine(output, "fixture_only_long_detail.png"));
            P.View.ModalCancel.onClick.Invoke();
            P.View.Primary.onClick.Invoke(); yield return null;
            Assert.IsTrue(P.View.Modal.activeSelf);
            Assert.That(P.View.ModalBody.text, Does.Contain(P.Catalog.Find("Rabbit").DisplayName)
                .And.Contain(P.Catalog.Find("DreamFox").DisplayName).And.Contain(P.Catalog.Find("Swallow").DisplayName));
            Assert.That(P.View.ModalBody.text, Does.Not.Contain(P.Catalog.Find("Wolf").DisplayName));
            yield return Capture(Path.Combine(output, "fixture_only_fixed_roster_confirmation.png"));
            P.View.ModalCancel.onClick.Invoke(); Assert.That(backend.Submissions, Is.Zero);
            Assert.That(source.LoadRequests, Is.Zero);
#if UNITY_EDITOR
            var setup = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t => t != null);
            foreach (int height in new[] { 1920, 2400 })
            foreach (bool inset in new[] { false, true })
            {
                setup.GetMethod("SelectFixedResolution").Invoke(null, new object[] { 1080, height, "ANIMOL Phase2 QA" });
                yield return null; yield return null;
                var safe = nav.GetComponentInChildren<SafeAreaLayout>(); safe.enabled = true; safe.Apply();
                if (inset)
                {
                    safe.enabled = false; var rect = (RectTransform)safe.transform;
                    rect.anchorMin = new Vector2(48f / 1080, 96f / height);
                    rect.anchorMax = new Vector2(1 - 48f / 1080, 1 - 120f / height);
                }
                yield return null; yield return null;
                P.View.SelectionDetailsButton.onClick.Invoke(); yield return null; yield return null; yield return null;
                modalTypography.Synchronize(); Assert.IsFalse(modalTypography.Overflow);
                AssertContained((RectTransform)P.View.ModalCancel.transform, (RectTransform)safe.transform);
                AssertContained((RectTransform)P.View.ModalConfirm.transform, (RectTransform)safe.transform);
                string prefix = "fixture_only_" + height + (inset ? "_safe" : "_full");
                scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; yield return null;
                yield return Capture(Path.Combine(output, prefix + "_detail_top.png"));
                scroll.verticalNormalizedPosition = 0; yield return null; yield return null;
                Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(0).Within(.01f));
                yield return Capture(Path.Combine(output, prefix + "_detail_bottom.png"));
                P.View.ModalCancel.onClick.Invoke(); P.View.Primary.onClick.Invoke(); yield return null; yield return null;
                if (scroll.content.rect.height > scroll.viewport.rect.height)
                    Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(.01f));
                else AssertContained(scroll.content, scroll.viewport);
                yield return Capture(Path.Combine(output, prefix + "_confirmation.png"));
                P.View.ModalCancel.onClick.Invoke();
                safe.enabled = true; safe.Apply();
            }
#endif
            P.SetBackend(null); Object.Destroy(backend); host.enabled = true;
        }

        [UnityTest] public IEnumerator ManualRefreshRebuildsHostContextAndNeverLoads()
        {
            yield return Open(0);
            var stage = source.SelectedStage;
            var changed = Object.Instantiate(stage);
            // An in-memory catalog update fixture. Never changes the source asset.
            typeof(ANIMOL.Core.CampaignStageDefinition).GetField("contentVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(changed, stage.ContentVersion + 1);
            var property = typeof(ProductionController).GetProperty("SelectedStage");
            try
            {
                property.SetValue(source, changed);
                string previous = host.Context.Requirements.PolicyRevision;
                Assert.IsTrue(P.View.Refresh.interactable);
                P.View.Refresh.onClick.Invoke(); yield return null;
                Assert.That(host.Context.Requirements.PolicyRevision, Is.Not.EqualTo(previous));
                Assert.That(P.Draft.Fingerprint(), Is.EqualTo("Rabbit||"));
                Assert.IsFalse(P.View.Primary.interactable);
                Assert.That(source.LoadRequests, Is.Zero);
            }
            finally { property.SetValue(source, stage); Object.Destroy(changed); }
        }

        [UnityTest] public IEnumerator FixtureOnlyUnknownKeepsSameRequestAcrossDisableAndCannotLoad()
        {
            yield return Open(0); host.enabled = false;
            var backend = host.gameObject.AddComponent<StagePhase2PresentationFixture>();
            var context = AnimalStagePhase2DemoHost.CreateReadonlyFixture();
            context.StageId = "TEST_ONLY"; context.Requirements.PolicyRevision = "TEST_ONLY";
            backend.Snapshot = new AnimalUiSnapshot { Revision = "TEST_ONLY", ContextId = context.StageId, PolicyRevision = "TEST_ONLY" };
            foreach (var a in P.Catalog.Animals)
                backend.Snapshot.Animals.Add(new AnimalProgress { AnimalId = a.Id, Implemented = context.FixedLoadout.Get(a.Role) == a.Id,
                    HasContextPermission = true, CanUseInContext = true, Unlocked = false });
            var pending = new System.Threading.Tasks.TaskCompletionSource<AnimalUiCommitResult>();
            backend.OnCampaign = _ => pending.Task;
            P.SetBackend(backend); P.OpenCampaign(context); yield return null;
            P.View.Primary.onClick.Invoke(); P.View.ModalCancel.onClick.Invoke(); Assert.That(backend.Submissions, Is.Zero);
            P.View.Primary.onClick.Invoke(); P.View.ModalConfirm.onClick.Invoke(); P.View.ModalConfirm.onClick.Invoke();
            Assert.That(backend.Submissions, Is.EqualTo(1)); Assert.IsTrue(P.IsCommitting);
            Assert.IsFalse(P.OpenCampaign(new StageSelectionRequest { StageId = "OTHER" }));
            P.View.Back.onClick.Invoke(); P.View.ModalCancel.onClick.Invoke(); P.View.Refresh.onClick.Invoke();
            Assert.IsTrue(P.IsCommitting); Assert.That(source.LoadRequests, Is.Zero);
            pending.SetException(new System.TimeoutException("TEST_ONLY timeout")); yield return null; yield return null;
            Assert.IsTrue(P.IsCommitting);
            host.enabled = true; // The real host must also refuse to replace an uncertain request.
            host.gameObject.SetActive(false); yield return null; host.gameObject.SetActive(true); yield return null;
            Assert.IsTrue(P.IsCommitting);
            backend.OnCampaign = _ => System.Threading.Tasks.Task.FromResult(new AnimalUiCommitResult { Status = CommitStatus.Unavailable, Message = "TEST_ONLY final refusal" });
            P.View.ModalConfirm.onClick.Invoke(); yield return null;
            Assert.That(backend.Submissions, Is.EqualTo(2));
            var first = backend.Requests[0]; var retry = backend.Requests[1];
            Assert.That(retry.ActionId, Is.EqualTo(first.ActionId)); Assert.That(retry.ContextId, Is.EqualTo(first.ContextId));
            Assert.That(retry.SnapshotRevision, Is.EqualTo(first.SnapshotRevision)); Assert.That(retry.PolicyRevision, Is.EqualTo(first.PolicyRevision));
            Assert.That(retry.Loadout.Fingerprint(), Is.EqualTo(first.Loadout.Fingerprint()));
            Assert.IsFalse(P.IsCommitting); Assert.IsFalse(P.View.Primary.interactable);
            Assert.That(P.View.Status.text, Does.Contain("TEST_ONLY final refusal")); Assert.That(source.LoadRequests, Is.Zero);
            P.SetBackend(null); Object.Destroy(backend); host.enabled = true;
        }

        [UnityTest] public IEnumerator FixtureOnlyLateStageReadCannotOverwriteCurrentStage()
        {
            yield return Open(0); host.enabled = false;
            var backend = AttachFixture(out var a);
            var b = a.Clone(); b.StageId = "TEST_ONLY_B";
            var delayed = new System.Threading.Tasks.TaskCompletionSource<AnimalUiSnapshot>();
            var old = backend.Snapshot;
            old.Find("Rabbit").ActiveDescription = "OLD_A";
            backend.Snapshot = FixtureSnapshot(b);
            backend.Snapshot.Find("Rabbit").ActiveDescription = "CURRENT_B";
            backend.Snapshot.Find("Rabbit").PassiveDescription = "PASSIVE_B";
            backend.OnRead = r => r.Stage.StageId == a.StageId ? delayed.Task : System.Threading.Tasks.Task.FromResult(backend.Snapshot);
            P.OpenCampaign(a); yield return null;
            Assert.IsFalse(P.View.Primary.interactable);
            P.OpenCampaign(b); yield return null;
            Assert.That(P.View.HeroDescription.text, Does.Contain("CURRENT_B").And.Contain("PASSIVE_B"));
            delayed.SetResult(old); yield return null; yield return null;
            Assert.That(P.View.HeroDescription.text, Does.Contain("CURRENT_B").And.Not.Contain("OLD_A"));
            P.View.Primary.onClick.Invoke(); P.View.ModalConfirm.onClick.Invoke(); yield return null;
            Assert.That(backend.Requests.Single().ContextId, Is.EqualTo(b.StageId));
            Assert.That(source.LoadRequests, Is.Zero);
        }

        [UnityTest] public IEnumerator FixtureOnlyInvalidAuthorityNeverDispatchesEvenWhenButtonsInvoked()
        {
            yield return Open(0); host.enabled = false;
            var backend = AttachFixture(out var context);
            foreach (string fault in new[] { "implemented", "grant", "usable", "ownershipOnly", "context", "policy", "revision", "role", "representative" })
            {
                var request = context.Clone(); backend.Snapshot = FixtureSnapshot(request);
                var rabbit = backend.Snapshot.Find("Rabbit");
                if (fault == "implemented") rabbit.Implemented = false;
                if (fault == "grant") rabbit.HasContextPermission = false;
                if (fault == "usable") rabbit.CanUseInContext = false;
                if (fault == "ownershipOnly") { rabbit.Unlocked = true; rabbit.HasContextPermission = false; rabbit.CanUseInContext = false; }
                if (fault == "context") backend.Snapshot.ContextId = "OTHER";
                if (fault == "policy") backend.Snapshot.PolicyRevision = "OTHER";
                if (fault == "revision") backend.Snapshot.Revision = " ";
                if (fault == "role") request.FixedLoadout.Air = "Wolf";
                if (fault == "representative") request.Requirements.RepresentativeRole = (AnimalRole)99;
                P.OpenCampaign(request); yield return null;
                Assert.IsFalse(P.View.Primary.interactable, fault);
                P.View.Primary.onClick.Invoke(); P.View.ModalConfirm.onClick.Invoke();
                Assert.IsFalse(P.View.Modal.activeSelf, fault);
                Assert.That(backend.Submissions, Is.Zero, fault);
                Assert.That(source.LoadRequests, Is.Zero, fault);
            }
        }

        [UnityTest] public IEnumerator FixtureOnlyMalformedReceiptsStayUnknownThenExactReceiptEmitsOnce()
        {
            yield return Open(0); host.enabled = false;
            var backend = AttachFixture(out var context);
            int accepted = 0; P.CampaignAccepted += _ => accepted++;
            string fault = "token";
            backend.OnCampaign = request =>
            {
                var result = Receipt(request);
                if (fault == "token") result.AcceptanceToken = " ";
                if (fault == "context") result.AcceptedContextId = "OTHER";
                if (fault == "policy") result.AcceptedPolicyRevision = "OTHER";
                if (fault == "loadout") result.AcceptedLoadout.Ground = "Wolf";
                return System.Threading.Tasks.Task.FromResult(result);
            };
            P.OpenCampaign(context); yield return null;
            P.View.Primary.onClick.Invoke(); P.View.ModalCancel.onClick.Invoke(); Assert.That(backend.Submissions, Is.Zero);
            P.View.Primary.onClick.Invoke(); P.View.ModalConfirm.onClick.Invoke(); yield return null;
            foreach (string next in new[] { "context", "policy", "loadout", "valid" })
            {
                Assert.That(accepted, Is.Zero); Assert.IsTrue(P.IsCommitting);
                Assert.That(source.LoadRequests, Is.Zero);
                Assert.IsFalse(P.OpenCampaign(context));
                fault = next; P.View.ModalConfirm.onClick.Invoke(); yield return null;
            }
            Assert.That(accepted, Is.EqualTo(1)); Assert.IsFalse(P.IsCommitting);
            Assert.That(backend.Submissions, Is.EqualTo(5));
            Assert.That(backend.Requests.Select(r => JsonUtility.ToJson(r)).Distinct().Count(), Is.EqualTo(1));
            P.View.ModalConfirm.onClick.Invoke(); P.View.Primary.onClick.Invoke();
            host.gameObject.SetActive(false); yield return null; host.gameObject.SetActive(true); yield return null;
            P.View.Primary.onClick.Invoke(); P.View.ModalConfirm.onClick.Invoke();
            Assert.That(accepted, Is.EqualTo(1)); Assert.That(backend.Submissions, Is.EqualTo(5));
            // This observes a presenter event only. No production receipt consumer or loader is installed.
            Assert.That(source.LoadRequests, Is.Zero);
        }

        [UnityTest] public IEnumerator FixtureOnlyLateAcceptanceWhileInactiveRequiresSameRequestReconciliation()
        {
            yield return Open(0); host.enabled = false;
            var backend = AttachFixture(out var context);
            int accepted = 0; P.CampaignAccepted += _ => accepted++;
            var pending = new System.Threading.Tasks.TaskCompletionSource<AnimalUiCommitResult>();
            backend.OnCampaign = _ => pending.Task;
            P.OpenCampaign(context); yield return null;
            P.View.Primary.onClick.Invoke(); P.View.ModalConfirm.onClick.Invoke();
            var original = backend.Requests.Single();
            host.gameObject.SetActive(false); yield return null;
            pending.SetResult(Receipt(original)); yield return null; yield return null;
            Assert.That(accepted, Is.Zero); Assert.That(source.LoadRequests, Is.Zero);
            host.gameObject.SetActive(true); yield return null;
            Assert.IsTrue(P.IsCommitting); Assert.IsFalse(P.OpenCampaign(context));
            backend.OnCampaign = r => System.Threading.Tasks.Task.FromResult(Receipt(r));
            P.View.ModalConfirm.onClick.Invoke(); yield return null;
            Assert.That(accepted, Is.EqualTo(1)); Assert.That(backend.Submissions, Is.EqualTo(2));
            Assert.That(JsonUtility.ToJson(backend.Requests[1]), Is.EqualTo(JsonUtility.ToJson(original)));
            Assert.That(source.LoadRequests, Is.Zero);
        }

        private StagePhase2PresentationFixture AttachFixture(out StageSelectionRequest context)
        {
            context = AnimalStagePhase2DemoHost.CreateReadonlyFixture();
            context.StageId = "TEST_ONLY_A"; context.Requirements.PolicyRevision = "TEST_ONLY_POLICY";
            var backend = host.gameObject.AddComponent<StagePhase2PresentationFixture>();
            backend.Snapshot = FixtureSnapshot(context); P.SetBackend(backend); return backend;
        }
        private AnimalUiSnapshot FixtureSnapshot(StageSelectionRequest context)
        {
            var snapshot = new AnimalUiSnapshot { ContextId = context.StageId, PolicyRevision = context.Requirements.PolicyRevision, Revision = "TEST_ONLY_READ" };
            foreach (var a in P.Catalog.Animals)
            {
                bool designated = context.FixedLoadout.Get(a.Role) == a.Id;
                snapshot.Animals.Add(new AnimalProgress { AnimalId = a.Id, Implemented = designated,
                    HasContextPermission = designated, CanUseInContext = designated, Unlocked = false });
            }
            return snapshot;
        }
        private static AnimalUiCommitResult Receipt(SelectionCommitRequest request) => new AnimalUiCommitResult
        {
            Status = CommitStatus.Accepted, AcceptanceToken = "TEST_ONLY_RECEIPT", AcceptedContextId = request.ContextId,
            AcceptedPolicyRevision = request.PolicyRevision, AcceptedLoadout = request.Loadout.Clone()
        };

        private static IEnumerator Capture(string path)
        {
            yield return new WaitForEndOfFrame(); var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, image.EncodeToPNG()); Object.Destroy(image);
        }
        private static void AssertContained(RectTransform child, RectTransform parent)
        {
            Canvas.ForceUpdateCanvases(); var corners = new Vector3[4]; child.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var p = parent.InverseTransformPoint(corner);
                Assert.That(p.x, Is.InRange(parent.rect.xMin - 1, parent.rect.xMax + 1), child.name + " x");
                Assert.That(p.y, Is.InRange(parent.rect.yMin - 1, parent.rect.yMax + 1), child.name + " y");
            }
        }
    }
}
