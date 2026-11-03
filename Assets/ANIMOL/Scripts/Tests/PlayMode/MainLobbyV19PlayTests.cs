using System.Collections;
using System;
using System.Linq;
using System.Reflection;
using ANIMOL.Menu;
using ANIMOL.UI.StyleV19;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ANIMOL.Menu.Tests
{
    public sealed class MainLobbyV19PlayTests
    {
        [UnityTest]
        public IEnumerator FinalSpriteReferencesSurviveLobbyInitializationAndClicks()
        {
#if UNITY_EDITOR
            var portrait = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("ANIMOL.Presentation.Editor.PortraitGameView"))
                .First(type => type != null);
            portrait.GetMethod("Use1080x1920", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, null);
            EditorSceneManager.LoadSceneInPlayMode("Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var lobby = Object.FindFirstObjectByType<MainLobbyController>();
            Assert.That(lobby, Is.Not.Null);
            Assert.That(lobby.uiV19, Is.Not.Null);
            var bindings = lobby.uiV19;
            var skin = bindings.skin;
            Assert.That(skin, Is.Not.Null);
            Assert.That(bindings.buttons.Count, Is.EqualTo(9));
            Assert.That(bindings.topPaper.sprite, Is.SameAs(skin.paperStrip));
            Assert.That(bindings.bottomPaper.sprite, Is.SameAs(skin.paperStrip));
            Assert.That(lobby.canvas.GetComponent<CanvasScaler>().referenceResolution,
                Is.EqualTo(new Vector2(1080, 1920)));
            Assert.That(lobby.battleButton.interactable, Is.False);
            Assert.That(lobby.catalog, Is.Not.Null);
            Assert.That(lobby.heroImage.sprite, Is.Not.Null);
            Assert.That(lobby.backgroundImage.sprite, Is.Not.Null);
            foreach (var entry in bindings.buttons) Check(entry, skin);

            lobby.customButton.onClick.Invoke();
            yield return null;
            Assert.That(lobby.customButton.image.sprite, Is.SameAs(skin.mode.active));
            Assert.That(lobby.campaignButton.image.sprite, Is.SameAs(skin.mode.normal));
            lobby.campaignButton.onClick.Invoke();
            yield return null;
            Assert.That(lobby.campaignButton.image.sprite, Is.SameAs(skin.mode.active));
            Assert.That(lobby.battleButton.interactable, Is.False);
            foreach (var entry in bindings.buttons) Check(entry, skin);

            lobby.playButton.onClick.Invoke();
            Assert.That(lobby.IsCampaignOpen, Is.True);
            lobby.campaignBackButton.onClick.Invoke();
            Assert.That(lobby.IsCampaignOpen, Is.False);
            lobby.settingsButton.onClick.Invoke();
            Assert.That(lobby.IsModalOpen, Is.True);
            lobby.modalCloseButton.onClick.Invoke();
            Assert.That(lobby.IsModalOpen, Is.False);
            lobby.soundButton.onClick.Invoke();
            Assert.That(lobby.IsModalOpen, Is.True);
            lobby.modalCloseButton.onClick.Invoke();
            Assert.That(AssetDatabase.GetAssetPath(skin.paperStrip),
                Is.EqualTo("Assets/ANIMOL/UI/StyleV19/Textures/Panels/PaperStrip.png"));
            Assert.That(lobby.dock.GetComponentInChildren<DiagonalPattern.DiagonalPatternBackground>(true),
                Is.Not.Null);
#else
            Assert.Ignore("Requires the Unity Editor scene loader.");
            yield break;
#endif
        }

        static void Check(PaperButtonBinding entry, AnimolUiV19Skin skin)
        {
            var set = skin.Get(entry.role);
            var button = entry.control as Button;
            Assert.That(button, Is.Not.Null);
            Assert.That(button.targetGraphic, Is.SameAs(button.GetComponent<Image>()));
            Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap));
            Assert.That(button.image.sprite, Is.SameAs(entry.active ? set.active : set.normal), button.name);
            Assert.That(button.image.type, Is.EqualTo(Image.Type.Sliced), button.name);
            Assert.That(button.image.color, Is.EqualTo(Color.white), button.name);
            Assert.That(button.image.pixelsPerUnitMultiplier, Is.EqualTo(1), button.name);
            var state = button.spriteState;
            Assert.That(state.highlightedSprite, Is.SameAs(set.highlighted));
            Assert.That(state.pressedSprite, Is.SameAs(set.pressed));
            Assert.That(state.selectedSprite, Is.SameAs(set.highlighted));
            Assert.That(state.disabledSprite, Is.SameAs(set.disabled));
        }
    }
}
