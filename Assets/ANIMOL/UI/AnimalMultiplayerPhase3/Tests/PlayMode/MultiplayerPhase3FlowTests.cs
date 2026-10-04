using System.Collections;
using System.IO;
using System.Linq;
using ANIMOL.AnimalUiV2;
using ANIMOL.PortraitArtV1;
using ANIMOL.Typography;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ANIMOL.AnimalMultiplayerPhase3.Tests
{
    public sealed class MultiplayerPhase3FlowTests
    {
        private UiNavigationService nav;
        private AnimalMultiplayerPhase3Host host;
        private AnimalUiPresenter P => host.Presenter;
        private static string Output => Path.Combine(Path.GetTempPath(), "ANIMOL-MultiplayerPhase3-Captures");
        [UnitySetUp] public IEnumerator Lobby()
        {
            SceneManager.LoadScene("Lobby");
            yield return null; yield return null; yield return null; yield return null;
            nav = Object.FindFirstObjectByType<UiNavigationService>();
            host = nav.GetComponent<AnimalMultiplayerPhase3Entry>().Host;
            Assert.NotNull(host); Assert.IsFalse(host.gameObject.activeSelf);
            nav.GetComponentInChildren<PortraitEntryController>(true).ModeScreen.Competition.onClick.Invoke();
            Assert.That(nav.CurrentScreenId, Is.EqualTo("SC05_CompetitiveHub"));
        }
        private IEnumerator Open(string button = "CompetitiveMatchButton")
        {
            nav.GetComponentsInChildren<Button>(true).Single(b => b.name == button).onClick.Invoke();
            yield return null; yield return null; yield return new WaitForSecondsRealtime(.5f);
            Assert.That(nav.CurrentScreenId, Is.EqualTo(AnimalMultiplayerPhase3Host.ScreenId));
        }
        [UnityTest] public IEnumerator ActualExistingEntryRoutesBrowseWithoutRoomOrSavedChanges()
        {
            var source = nav.GetComponent<MultiplayerUiPresenter>();
            int participants = source.PreviewParticipantCount;
            var launch = ANIMOL.Core.CampaignLaunchContext.Pending;
            string sourceBefore = JsonUtility.ToJson(source);
            foreach (string button in new[] { "CompetitiveMatchButton", "RankedMatchButton", "CompetitivePrivateButton" })
            {
                yield return Open(button);
                Assert.IsNull(host.Context.ModeId); Assert.IsNull(host.Context.EntryIntent);
                Assert.IsNull(host.Context.Requirements.PolicyRevision);
                Assert.That(host.Context.Requirements.RepresentativeRole, Is.EqualTo((AnimalRole)(-1)));
                Assert.That(P.Backend, Is.TypeOf<AnimalMultiplayerProjectAdapter>()); Assert.That(P.Draft.Fingerprint(), Is.EqualTo("||"));
                Assert.IsFalse(P.View.Primary.interactable);
                P.View.Primary.onClick.Invoke(); Assert.IsFalse(P.View.Modal.activeSelf);
                Assert.That(source.PreviewParticipantCount, Is.EqualTo(participants));
                Assert.That(ANIMOL.Core.CampaignLaunchContext.Pending, Is.SameAs(launch));
                Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.IsNull(Object.FindFirstObjectByType<AnimalMultiplayerPhase3DemoHost>());
                Assert.IsNull(Object.FindFirstObjectByType<AnimalUiDemoSwitcher>());
                yield return Capture(button + ".png");
                P.View.Back.onClick.Invoke(); yield return null;
                Assert.That(nav.CurrentScreenId, Is.EqualTo("SC05_CompetitiveHub"));
                Assert.That(host.LastReturnedLoadout.Fingerprint(), Is.EqualTo("||"));
            }
            Assert.That(JsonUtility.ToJson(source), Is.EqualTo(sourceBefore));
            nav.Back(); Assert.That(nav.CurrentScreenId, Is.EqualTo(PortraitEntryController.ModeId));
        }
        [UnityTest] public IEnumerator FifteenPortraitsBothGameViewsAndSafeArea()
        {
            yield return Open();
            string map = JsonUtility.ToJson(host.IdMap);
            foreach (int height in new[] { 1920, 2400 })
            foreach (bool inset in new[] { false, true })
            {
                yield return SetResolution(height, inset);
                string prefix = height + (inset ? "_safe" : "_full");
                var v = P.View; var footer = (RectTransform)v.Primary.transform; var position = footer.position;
                foreach (AnimalRole role in System.Enum.GetValues(typeof(AnimalRole)))
                {
                    v.RoleTabs[(int)role].onClick.Invoke(); yield return null;
                    var cards = v.RosterContent.GetComponentsInChildren<AnimalCardView>().Where(c => c.gameObject.activeInHierarchy).ToArray();
                    Assert.That(cards.Length, Is.EqualTo(5)); Assert.IsFalse(v.RosterScroll.enabled);
                    foreach (var animal in P.Catalog.ForRole(role))
                    {
                        var card = cards.Single(c => c.Name.text == animal.DisplayName);
                        card.Button.onClick.Invoke(); v.BodyScroll.StopMovement(); v.BodyScroll.verticalNormalizedPosition = 1;
                        yield return null; yield return null;
                        Assert.That(P.InspectedAnimalId, Is.EqualTo(animal.Id));
                        Assert.That(P.Draft.Fingerprint(), Is.EqualTo("||"));
                        Assert.That(v.HeroPortrait.sprite, Is.SameAs(animal.Portrait));
                        Assert.That(card.Portrait.sprite, Is.SameAs(animal.Portrait));
                        Assert.IsTrue(v.HeroPortrait.preserveAspect && card.Portrait.preserveAspect);
                        AssertContained(v.HeroPortrait.rectTransform, v.BodyScroll.viewport);
                        Assert.IsNull(card.Portrait.GetComponent<Mask>());
                        Assert.That(v.HeroDescription.text, Does.Contain("액티브: --").And.Contain("패시브: --"));
                        foreach (var bridge in P.GetComponentsInChildren<PixelTextBridge>()) Assert.IsFalse(bridge.Overflow, animal.Id + "/" + bridge.name);
                        yield return Capture(prefix + "_" + animal.Id + ".png");
                    }
                    v.BodyScroll.verticalNormalizedPosition = 0; yield return null; yield return null;
                    AssertContained(v.RosterPanel, v.BodyScroll.viewport);
                    yield return Capture(prefix + "_cards_" + role + ".png");
                    var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, cards[0].Portrait.transform.position) };
                    var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits); Assert.IsNotEmpty(hits);
                    var drag = ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject); Assert.That(drag, Is.SameAs(v.BodyScroll.gameObject));
                    ExecuteEvents.Execute(drag, pointer, ExecuteEvents.initializePotentialDrag); ExecuteEvents.Execute(drag, pointer, ExecuteEvents.beginDragHandler);
                    var before = v.BodyContent.anchoredPosition;
                    pointer.position -= new Vector2(0, 200); ExecuteEvents.Execute(drag, pointer, ExecuteEvents.dragHandler); ExecuteEvents.Execute(drag, pointer, ExecuteEvents.endDragHandler);
                    Assert.That(v.BodyContent.anchoredPosition.y, Is.LessThan(before.y)); v.BodyScroll.StopMovement();
                    Assert.That(footer.position, Is.EqualTo(position));
                }
                AssertContained(footer, (RectTransform)nav.GetComponentInChildren<SafeAreaLayout>().transform);
                v.SelectionDetailsButton.onClick.Invoke(); yield return null; yield return null;
                Assert.That(v.ModalBody.text, Does.Contain("Lv. --").And.Contain("설정 대기"));
                yield return Capture(prefix + "_modal.png"); v.ModalCancel.onClick.Invoke();
            }
            Assert.That(JsonUtility.ToJson(host.IdMap), Is.EqualTo(map));
            var safe = nav.GetComponentInChildren<SafeAreaLayout>(); safe.enabled = true; safe.Apply();
        }
        [UnityTest] public IEnumerator FixtureOnlyPermissionsDraftWholeConfirmationAndOriginalReturn()
        {
            var context = new MultiplayerSelectionRequest
            {
                ModeId = "TEST_ONLY_MODE", DisplayName = "TEST ONLY · " + string.Join(" · ", Enumerable.Repeat("긴 모드 이름 표시 검사", 8)), EntryIntent = "opaque host value / TEST_ONLY",
                InitialLoadout = new AnimalLoadout { Ground = "Rabbit", Special = "DreamFox", Air = "Swallow" }, AllowedAnimalIds = null,
                Requirements = new SelectionRequirements { RequiredRoles = new[] { AnimalRole.Ground, AnimalRole.Special, AnimalRole.Air }, RepresentativeRole = AnimalRole.Air, PolicyRevision = "TEST_ONLY_POLICY" }
            };
            var backend = host.gameObject.AddComponent<MultiplayerPhase3Fixture>();
            backend.Snapshot = new AnimalUiSnapshot { Revision = "TEST_ONLY_READ", ContextId = context.ModeId,
                PolicyRevision = context.Requirements.PolicyRevision, EntryIntent = context.EntryIntent };
            string longEffect = string.Join("\n", Enumerable.Repeat("장문 표시 검사 전용입니다. 실제 서비스의 능력이나 레벨이 아닙니다.", 25));
            foreach (var animal in P.Catalog.Animals)
                backend.Snapshot.Animals.Add(new AnimalProgress { AnimalId = animal.Id, Implemented = animal.Id != "Otter",
                    HasContextPermission = true, CanUseInContext = true, Unlocked = false, ActiveLevel = int.MaxValue,
                    ActiveDescription = longEffect, PassiveLevel = 7, PassiveDescription = "독립 패시브 마지막 설명" });
            P.SetBackend(backend); Assert.IsTrue(host.Open(context)); yield return null; yield return null;
            Assert.That(host.Context.EntryIntent, Is.EqualTo(context.EntryIntent)); Assert.IsNull(host.Context.AllowedAnimalIds);
            P.View.RoleTabs[0].onClick.Invoke();
            var cards = P.View.RosterContent.GetComponentsInChildren<AnimalCardView>();
            cards.Single(c => c.Name.text == P.Catalog.Find("Wolf").DisplayName).Button.onClick.Invoke();
            Assert.That(P.Draft.Ground, Is.EqualTo("Wolf")); Assert.That(context.InitialLoadout.Ground, Is.EqualTo("Rabbit"));
            cards.Single(c => c.Name.text == P.Catalog.Find("Otter").DisplayName).Button.onClick.Invoke();
            Assert.That(P.InspectedAnimalId, Is.EqualTo("Otter")); Assert.That(P.Draft.Ground, Is.EqualTo("Wolf"));
            Assert.That(backend.Requests, Is.Zero);
            foreach (int height in new[] { 1920, 2400 })
            {
                yield return SetResolution(height, true);
                yield return Capture("fixture_long_mode_" + height + ".png");
                var subtitle = P.View.Subtitle.GetComponent<PixelTextBridge>(); subtitle.Synchronize();
                Assert.IsFalse(subtitle.Overflow, "Long fixture mode name must fit the header, with full text accessible in details.");
                P.View.SelectionDetailsButton.onClick.Invoke(); yield return null; yield return null; yield return null;
                var scroll = P.View.ModalBodyScroll;
                var bridge = P.View.ModalBody.GetComponent<PixelTextBridge>(); bridge.Synchronize(); Assert.IsFalse(bridge.Overflow);
                Assert.That(P.View.ModalBody.text, Does.Contain(longEffect).And.Contain(int.MaxValue.ToString()).And.Contain("독립 패시브 마지막 설명"));
                Assert.That(P.View.ModalBody.text, Does.Contain(context.DisplayName));
                yield return Capture("fixture_" + height + "_detail_top.png");
                scroll.StopMovement(); scroll.verticalNormalizedPosition = 0; yield return null; yield return null;
                yield return Capture("fixture_" + height + "_detail_bottom.png"); P.View.ModalCancel.onClick.Invoke();
                P.View.Primary.onClick.Invoke(); yield return null; yield return null;
                Assert.IsTrue(P.View.Modal.activeSelf);
                Assert.That(P.View.ModalBody.text, Does.Contain(context.DisplayName).And.Contain("늑대").And.Contain("몽환여우").And.Contain("제비").And.Not.Contain("수달"));
                yield return Capture("fixture_" + height + "_confirmation.png"); P.View.ModalCancel.onClick.Invoke(); Assert.That(backend.Requests, Is.Zero);
            }
            P.View.Back.onClick.Invoke(); yield return null;
            Assert.That(nav.CurrentScreenId, Is.EqualTo("SC05_CompetitiveHub"));
            Assert.That(host.LastReturnedLoadout.Fingerprint(), Is.EqualTo(context.InitialLoadout.Fingerprint()));
            context.AllowedAnimalIds = new string[0]; Assert.IsTrue(host.Open(context)); yield return null;
            Assert.That(host.Context.AllowedAnimalIds, Is.Empty); Assert.IsFalse(P.View.Primary.interactable);
            P.View.Primary.onClick.Invoke(); Assert.IsFalse(P.View.Modal.activeSelf); Assert.That(backend.Requests, Is.Zero);
            Assert.IsFalse(host.Open(AnimalMultiplayerPhase3DemoHost.CreateReadonlyFixture()));
        }
        private IEnumerator SetResolution(int height, bool inset)
        {
            var setup = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t => t != null);
            setup.GetMethod("SelectFixedResolution").Invoke(null, new object[] { 1080, height, "ANIMOL Phase3 QA" });
            yield return null; yield return null;
            Assert.That(Screen.width, Is.EqualTo(1080)); Assert.That(Screen.height, Is.EqualTo(height));
            var safe = nav.GetComponentInChildren<SafeAreaLayout>(); safe.enabled = true; safe.Apply();
            if (inset)
            {
                safe.enabled = false; var rect = (RectTransform)safe.transform;
                rect.anchorMin = new Vector2(48f / 1080, 96f / height); rect.anchorMax = new Vector2(1 - 48f / 1080, 1 - 120f / height);
            }
            yield return null; yield return null; yield return null; Canvas.ForceUpdateCanvases();
        }
        private static IEnumerator Capture(string name)
        {
            Directory.CreateDirectory(Output); yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(Output, name), image.EncodeToPNG()); Object.Destroy(image);
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
