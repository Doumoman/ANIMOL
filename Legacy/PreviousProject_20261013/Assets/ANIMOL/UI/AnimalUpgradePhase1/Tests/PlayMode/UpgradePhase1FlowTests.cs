using System.Collections;
using System.IO;
using System.Linq;
using ANIMOL.AnimalUiV2;
using ANIMOL.Typography;
using ANIMOL.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ANIMOL.AnimalUpgradePhase1.Tests
{
    public sealed class UpgradePhase1FlowTests
    {
        private UiNavigationService nav;
        private AnimalUiPresenter presenter;
        private Button Button(string name) => nav.GetComponentsInChildren<Button>(true).Single(b => b.name == name);

        [UnitySetUp] public IEnumerator OpenLobby()
        {
            SceneManager.LoadScene("Lobby");
            yield return null; yield return null; yield return null; yield return null;
            nav = Object.FindFirstObjectByType<UiNavigationService>();
            Assert.NotNull(nav);
            presenter = nav.GetComponentInChildren<AnimalUpgradePhase1Host>(true).Presenter;
        }

        [UnityTest] public IEnumerator BothHubRoutesBrowseAndReturnWithoutDemoOrPurchases()
        {
            foreach (var route in new[] { "PV1_Character", "UpgradeHubCharacterButton" })
            {
                nav.Navigate("SC10_UpgradeHub"); Button(route).onClick.Invoke(); yield return null;
                Assert.That(nav.CurrentScreenId, Is.EqualTo(AnimalUpgradePhase1Host.ScreenId));
                Assert.IsTrue(presenter.isActiveAndEnabled);
                Assert.That(presenter.Backend, Is.TypeOf<AnimalUpgradeProjectAdapter>());
                Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.IsNull(Object.FindFirstObjectByType<AnimalUiDemoSwitcher>());
                Assert.IsNull(Object.FindFirstObjectByType<AnimalUpgradePhase1DemoHost>());
                presenter.View.Back.onClick.Invoke(); yield return null;
                Assert.That(nav.CurrentScreenId, Is.EqualTo("SC10_UpgradeHub"));
            }
            Button("PV1_Account").onClick.Invoke(); yield return null;
            Assert.That(nav.CurrentScreenId, Is.EqualTo("SC10A_AccountUpgrade"));
            Assert.IsTrue(nav.transform.Find("SafeArea/ScreenHost/SC10A_AccountUpgrade/ProductionV1").gameObject.activeInHierarchy);
            nav.Back(); nav.Back(); yield return null;
            Assert.That(nav.CurrentScreenId, Is.EqualTo("SC01_Lobby"));
        }

        [UnityTest] public IEnumerator PortraitGameViewsBrowseAllAnimalsAndSafeArea()
        {
#if UNITY_EDITOR
            var gameViewSetup = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ANIMOL.Editor.PortraitGameViewSetup")).First(t => t != null);
            var output = Path.Combine(Path.GetTempPath(), "ANIMOL-UpgradePhase1-Captures");
            Directory.CreateDirectory(output);
            nav.Navigate("SC10_UpgradeHub"); Button("PV1_Character").onClick.Invoke(); yield return null;
            yield return new WaitForSecondsRealtime(.4f);
            var v = presenter.View;
            foreach (int height in new[] { 1920, 2400 })
            {
                gameViewSetup.GetMethod("SelectFixedResolution").Invoke(null, new object[] { 1080, height, "ANIMOL Phase1 QA" });
                yield return null; yield return null;
                Assert.That(Screen.width, Is.EqualTo(1080)); Assert.That(Screen.height, Is.EqualTo(height));
                var safe = nav.GetComponentInChildren<SafeAreaLayout>();
                foreach (bool inset in new[] { false, true })
                {
                    safe.enabled = true; safe.Apply();
                    if (inset)
                    {
                        safe.enabled = false;
                        var rect = (RectTransform)safe.transform;
                        rect.anchorMin = new Vector2(48f / 1080, 96f / height);
                        rect.anchorMax = new Vector2(1 - 48f / 1080, 1 - 120f / height);
                    }
                    yield return null;
                    string prefix = height + (inset ? "_safe" : "_full");
                    foreach (AnimalRole role in System.Enum.GetValues(typeof(AnimalRole)))
                    {
                        v.RoleTabs[(int)role].onClick.Invoke(); yield return null;
                        var cards = v.RosterContent.GetComponentsInChildren<AnimalCardView>().Where(c => c.gameObject.activeInHierarchy).ToArray();
                        Assert.That(cards.Length, Is.EqualTo(5));
                        Assert.IsFalse(v.RosterScroll.enabled, "Nested roster must not capture body drags.");
                        // Exercise the actual EventSystem drag handler under the first card, not just scroll properties.
                        v.BodyScroll.verticalNormalizedPosition = 1; Canvas.ForceUpdateCanvases();
                        var pointer = new PointerEventData(EventSystem.current)
                        { position = RectTransformUtility.WorldToScreenPoint(null, cards[0].Portrait.rectTransform.TransformPoint(cards[0].Portrait.rectTransform.rect.center)) };
                        var hits = new System.Collections.Generic.List<RaycastResult>();
                        EventSystem.current.RaycastAll(pointer, hits);
                        Assert.IsNotEmpty(hits);
                        var drag = ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject);
                        Assert.That(drag, Is.SameAs(v.BodyScroll.gameObject));
                        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.initializePotentialDrag);
                        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.beginDragHandler);
                        var scrollBefore = v.BodyContent.anchoredPosition;
                        pointer.position += new Vector2(0, 300);
                        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.dragHandler);
                        ExecuteEvents.Execute(drag, pointer, ExecuteEvents.endDragHandler);
                        if (v.BodyContent.rect.height > v.BodyScroll.viewport.rect.height)
                            Assert.That(v.BodyContent.anchoredPosition.y, Is.GreaterThan(scrollBefore.y));
                        v.BodyScroll.StopMovement(); v.BodyScroll.verticalNormalizedPosition = 1;
                        foreach (var animal in presenter.Catalog.ForRole(role))
                        {
                            var card = cards.Single(c => c.Name.text == animal.DisplayName);
                            card.Button.onClick.Invoke(); v.BodyScroll.verticalNormalizedPosition = 1;
                            yield return null; yield return null;
                            Assert.That(presenter.InspectedAnimalId, Is.EqualTo(animal.Id));
                            Assert.That(v.HeroName.text, Is.EqualTo(animal.DisplayName));
                            Assert.That(v.HeroRole.text, Does.Contain(AnimalUiRules.RoleName(role)));
                            Assert.That(v.HeroPortrait.sprite, Is.SameAs(animal.Portrait));
                            Assert.That(v.HeroState.text, Is.EqualTo(animal.Id == "Rabbit" ? "성장 설정 대기" : "ID 설정 대기"));
                            Assert.That(v.ActiveTrack.Level.text, Is.EqualTo("Lv. --"));
                            Assert.That(v.PassiveTrack.Level.text, Is.EqualTo("Lv. --"));
                            Assert.That(v.ActiveTrack.Cost.text, Does.Contain("설정 대기"));
                            Assert.IsFalse(v.ActiveTrack.UpgradeButton.interactable);
                            Assert.IsFalse(v.PassiveTrack.UpgradeButton.interactable);
                            Assert.That(v.Balances.text, Does.Contain(animal.DisplayName + " 숙련도 --"));
                            Assert.That(presenter.Backend, Is.TypeOf<AnimalUpgradeProjectAdapter>());
                            AssertContained(v.HeroPortrait.rectTransform, v.BodyScroll.viewport);
                            AssertContained(card.Portrait.rectTransform, v.RosterScroll.viewport);
                            Assert.IsNull(card.Portrait.GetComponent<Mask>());
                            Assert.IsTrue(card.Portrait.preserveAspect && v.HeroPortrait.preserveAspect);
                            foreach (var bridge in presenter.GetComponentsInChildren<PixelTextBridge>())
                                Assert.IsFalse(bridge.Overflow, animal.Id + "/" + bridge.name + ": " + bridge.Source.text);
                            yield return Capture(Path.Combine(output, prefix + "_" + animal.Id + ".png"));
                        }
                    }
                    var footer = (RectTransform)v.Primary.transform;
                    var before = footer.position;
                    v.BodyScroll.verticalNormalizedPosition = 0;
                    yield return null; yield return null;
                    Assert.That(footer.position, Is.EqualTo(before));
                    AssertContained((RectTransform)v.PassiveTrack.transform, v.BodyScroll.viewport);
                    AssertContained(footer, (RectTransform)safe.transform);
                    yield return Capture(Path.Combine(output, prefix + "_bottom.png"));
                    v.ActiveTrack.DetailsButton.onClick.Invoke(); yield return null;
                    Assert.IsTrue(v.Modal.activeInHierarchy);
                    Assert.That(v.ModalBody.text, Does.Contain("--"));
                    v.ModalCancel.onClick.Invoke(); yield return null;
                    Assert.IsFalse(v.Modal.activeSelf);
                }
                safe.enabled = true; safe.Apply();
            }
#else
            Assert.Ignore("Game View resolution/capture check requires the Unity Editor.");
            yield break;
#endif
        }

        private static IEnumerator Capture(string path)
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, image.EncodeToPNG()); Object.Destroy(image);
        }

        private static void AssertContained(RectTransform child, RectTransform parent)
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4]; child.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var point = parent.InverseTransformPoint(corner);
                Assert.That(point.x, Is.InRange(parent.rect.xMin - 1, parent.rect.xMax + 1), child.name + " x");
                Assert.That(point.y, Is.InRange(parent.rect.yMin - 1, parent.rect.yMax + 1), child.name + " y");
            }
        }
    }
}
