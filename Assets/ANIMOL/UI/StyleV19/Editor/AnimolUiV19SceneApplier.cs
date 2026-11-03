using System;
using ANIMOL.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.UI.StyleV19.Editor
{
    // Changes the existing lobby objects; it never rebuilds the catalog or replaces navigation.
    public static class AnimolUiV19SceneApplier
    {
        const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";
        const string Icons = AnimolUiV19Installer.Root + "/Textures/Icons/";

        [MenuItem("Tools/ANIMOL/UI V19/5 Apply to main lobby")]
        public static void ApplyToMainLobby()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before applying UI V19.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the currently open scene before applying UI V19.");
            AssetDatabase.Refresh();
            if (!AssetDatabase.LoadAssetAtPath<AnimolUiV19Skin>(AnimolUiV19Installer.SkinPath))
                AnimolUiV19Installer.ImportAndBuildSkin();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var skin = AssetDatabase.LoadAssetAtPath<AnimolUiV19Skin>(AnimolUiV19Installer.SkinPath);
            if (!skin || !skin.paperStrip) throw new InvalidOperationException("V19 skin was not generated.");
            var lobby = UnityEngine.Object.FindFirstObjectByType<MainLobbyController>();
            if (!lobby || !lobby.canvas || !lobby.topPaper || !lobby.dock || !lobby.catalog)
                throw new InvalidOperationException("The existing main lobby is incomplete.");

            var catalog = lobby.catalog;
            var scenery = lobby.backgroundImage.sprite;
            var illustration = lobby.heroImage.sprite;
            var bindings = lobby.GetComponent<AnimolUiV19Bindings>();
            if (!bindings) bindings = lobby.gameObject.AddComponent<AnimolUiV19Bindings>();
            bindings.skin = skin;
            bindings.topPaper = RequireImage(lobby.topPaper);
            bindings.bottomPaper = RequireImage(lobby.dock);
            bindings.buttons.Clear();
            Add(bindings, lobby.campaignButton, PaperButtonRole.Mode, true);
            Add(bindings, lobby.battleButton, PaperButtonRole.Mode);
            Add(bindings, lobby.customButton, PaperButtonRole.Mode);
            Add(bindings, lobby.playButton, PaperButtonRole.Primary);
            Add(bindings, lobby.settingsButton, PaperButtonRole.Utility);
            Add(bindings, lobby.soundButton, PaperButtonRole.Utility);
            Add(bindings, lobby.campaignBackButton, PaperButtonRole.Utility);
            Add(bindings, lobby.campaignContinueButton, PaperButtonRole.Primary);
            Add(bindings, lobby.modalCloseButton, PaperButtonRole.Utility);
            bindings.Apply();

            // Guest is a display surface in this lobby, not a clickable login action.
            var guest = lobby.topPaper.Find("Top Bar/Guest") as RectTransform;
            if (!guest) throw new InvalidOperationException("Guest profile surface is missing.");
            var profile = RequireImage(guest);
            profile.sprite = skin.profile.normal;
            profile.overrideSprite = null;
            profile.type = Image.Type.Sliced;
            profile.fillCenter = true;
            profile.preserveAspect = false;
            profile.pixelsPerUnitMultiplier = 1;
            profile.color = Color.white;
            profile.material = null;
            profile.raycastTarget = false;

            Icon(guest, "Profile Icon", "profile");
            Icon(lobby.settingsButton.transform, "Settings Icon", "settings");
            Icon(lobby.soundButton.transform, "Sound Icon", "sound");
            Icon(lobby.campaignButton.transform, "Mode Icon", "campaign");
            Icon(lobby.battleButton.transform, "Mode Icon", "battle");
            Icon(lobby.customButton.transform, "Mode Icon", "custom");
            Icon(lobby.playButton.transform, "Play Arrow", "arrow-right");

            var scaler = lobby.canvas.GetComponent<CanvasScaler>();
            if (!scaler) throw new InvalidOperationException("Main lobby CanvasScaler is missing.");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.referencePixelsPerUnit = 100;
            lobby.uiV19 = bindings;
            lobby.ForceLayout();

            if (lobby.catalog != catalog || lobby.backgroundImage.sprite != scenery || lobby.heroImage.sprite != illustration)
                throw new InvalidOperationException("V19 application changed art or progress references.");
            Validate(lobby);
            EditorUtility.SetDirty(lobby);
            EditorUtility.SetDirty(bindings);
            EditorUtility.SetDirty(scaler);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("UI V19 applied to the existing main lobby with nine SpriteSwap buttons and shared PaperStrip.", lobby);
        }

        static Image RequireImage(Transform target)
        {
            var image = target ? target.GetComponent<Image>() : null;
            if (!image) throw new InvalidOperationException("Expected background Image: " + (target ? target.name : "null"));
            return image;
        }

        static void Add(AnimolUiV19Bindings bindings, Button button, PaperButtonRole role, bool active = false)
        {
            if (!button) throw new InvalidOperationException("Existing lobby button is missing for role " + role);
            var feedback = button.GetComponents<MainMenuPressFeedback>();
            for (int i = 1; i < feedback.Length; i++) UnityEngine.Object.DestroyImmediate(feedback[i]);
            button.targetGraphic = RequireImage(button.transform);
            bindings.buttons.Add(new PaperButtonBinding { control = button, role = role, active = active });
        }

        static void Icon(Transform parent, string childName, string spriteName)
        {
            var child = parent.Find(childName);
            if (!child) throw new InvalidOperationException("Existing icon is missing: " + childName);
            var image = RequireImage(child);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Icons + spriteName + ".png");
            if (!sprite) throw new InvalidOperationException("V19 icon was not imported: " + spriteName);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        [MenuItem("Tools/ANIMOL/UI V19/6 Validate main lobby")]
        public static void ValidateMainLobby()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var lobby = UnityEngine.Object.FindFirstObjectByType<MainLobbyController>();
            Validate(lobby);
            Debug.Log("UI V19 main lobby references validated: " + scene.path, lobby);
        }

        public static void Validate(MainLobbyController lobby)
        {
            if (!lobby || !lobby.uiV19 || !lobby.uiV19.skin)
                throw new InvalidOperationException("Main lobby V19 bindings are missing.");
            var b = lobby.uiV19;
            if (b.buttons.Count != 9 || b.topPaper.sprite != b.skin.paperStrip ||
                b.bottomPaper.sprite != b.skin.paperStrip ||
                b.topPaper.type != Image.Type.Sliced || b.bottomPaper.type != Image.Type.Sliced ||
                b.topPaper.pixelsPerUnitMultiplier != 1 || b.bottomPaper.pixelsPerUnitMultiplier != 1)
                throw new InvalidOperationException("V19 paper or binding count mismatch.");
            foreach (var entry in b.buttons)
            {
                var set = b.skin.Get(entry.role);
                var image = entry.control.targetGraphic as Image;
                var state = entry.control.spriteState;
                if (!image || image != entry.control.GetComponent<Image>() ||
                    entry.control.GetComponents<MainMenuPressFeedback>().Length > 1 ||
                    image.sprite != (entry.active ? set.active : set.normal) ||
                    image.type != Image.Type.Sliced || image.color != Color.white ||
                    image.pixelsPerUnitMultiplier != 1 ||
                    entry.control.transition != Selectable.Transition.SpriteSwap ||
                    state.highlightedSprite != set.highlighted ||
                    state.pressedSprite != set.pressed || state.disabledSprite != set.disabled ||
                    state.selectedSprite != set.highlighted)
                    throw new InvalidOperationException("V19 button mismatch: " + entry.control.name);
            }
        }
    }
}
