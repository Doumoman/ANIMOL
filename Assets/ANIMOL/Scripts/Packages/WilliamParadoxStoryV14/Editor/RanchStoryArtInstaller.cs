#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using Animol.Starway;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace WilliamParadox.Story.Editor
{
    /// <summary>Updates the existing entry prefab/scene with Unity serialization, retaining GUIDs and host links.</summary>
    public static class RanchStoryArtInstaller
    {
        public const string DataRoot = "Assets/ANIMOL/Data/StarwayRanch/Story";
        public const string ShaderPath = "Assets/ANIMOL/Art/Shaders/StarwayConstellationParallax.shader";
        [Serializable] private sealed class Binding { public string source, destination, assetId, resourcePath, sha256; public bool transparent; }
        [Serializable] private sealed class Bindings { public Binding[] bindings; }

        public static string Apply(string extractedSource)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before importing.");
            var bindings = JsonUtility.FromJson<Bindings>(File.ReadAllText(DataRoot + "/ArtBindings.json"));
            // Verify the entire package before writing anything.
            foreach (var b in bindings.bindings)
            {
                var file = Path.Combine(extractedSource, b.source);
                using (var hash = SHA256.Create())
                {
                    var actual = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(file))).Replace("-", "").ToLowerInvariant();
                    if (actual != b.sha256) throw new InvalidDataException("Source checksum mismatch: " + b.source);
                }
                if (!b.destination.StartsWith("Assets/ANIMOL/Art/Story/starway/", StringComparison.Ordinal) || b.destination.Contains(".."))
                    throw new InvalidDataException("Unexpected art destination.");
            }
            foreach (var b in bindings.bindings)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(b.destination));
                File.Copy(Path.Combine(extractedSource, b.source), b.destination, true);
                AssetDatabase.ImportAsset(b.destination, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(b.destination);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.alphaIsTransparency = b.transparent;
                importer.mipmapEnabled = false;
                importer.isReadable = false;
                importer.maxTextureSize = 2048;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var prefab = PrefabUtility.LoadPrefabContents(StoryImportPaths.PrefabPath);
            try { Configure(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, StoryImportPaths.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(StoryImportPaths.ScenePath, OpenSceneMode.Additive);
            try
            {
                var runner = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<StoryRunner>(true)).Single();
                Configure(runner.gameObject);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            }
            return "Imported 29 verified PNGs; updated existing ranch prefab and preview scene. Notebook scene untouched.";
        }

        private static void Configure(GameObject root)
        {
            var view = root.GetComponent<StoryView>();
            var runner = root.GetComponent<StoryRunner>();
            if (view == null || runner == null) throw new InvalidDataException("Existing story entry is incomplete.");
            // Keep every imported story PNG in the existing build-scene GUID dependency chain.
            var bindings = JsonUtility.FromJson<Bindings>(File.ReadAllText(DataRoot + "/ArtBindings.json"));
            view.spriteBindings = bindings.bindings.Select(b => new StorySpriteBinding { id = b.assetId, sprite = Sprite(b.assetId) })
                .Concat(new[] { new StorySpriteBinding { id = "BG14_epilogue_book", sprite = Sprite("R17B") } }).ToArray();
            foreach (var old in root.GetComponentsInChildren<RanchStoryStage>(true)) UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (var old in root.GetComponentsInChildren<RanchOptionalLore>(true)) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var canvas = root.GetComponentInChildren<Canvas>(true);
            canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
            var stageRoot = Rect("Ranch Cut Driver", view.artworkRoot);
            var stage = stageRoot.gameObject.AddComponent<RanchStoryStage>(); view.ranchStage = stage; stage.view = view;
            stage.composition = Rect("Distance Composition", stageRoot);
            // One logical stage matching the original full-canvas contain calculation.
            var fit = stage.composition.gameObject.AddComponent<StoryLayerStage>(); fit.viewport = view.artworkRoot; fit.Refresh();
            stage.background = Picture("Background", stage.composition);
            stage.farStars = Overlay("Distant World FX", stage.composition, new Vector3(1, 1, 0));
            stage.friends = Picture("Three Friends", stage.composition, Sprite("R15_Friends"));
            stage.nearStars = Overlay("Near World FX", stage.composition, new Vector3(0, 0, 1));
            stage.hand = Picture("Isol Foreground Hand", stage.composition, Sprite("R15_IsolHand"));
            var hand = stage.hand.rectTransform;
            hand.anchorMin = hand.anchorMax = new Vector2(1, 0); hand.pivot = new Vector2(1, 0);
            hand.sizeDelta = new Vector2(1080 * 0.64f, 1080 * 0.64f * 1.5f);
            hand.anchoredPosition = new Vector2(1080 * 0.02f, -1920 * 0.06f);
            stage.runnerImage = Picture("Isol Foreground Run", stage.composition, Sprite("R08_IsolRun"));
            stage.runnerImage.rectTransform.anchorMin = stage.runnerImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            stage.runnerImage.rectTransform.sizeDelta = new Vector2(1060, 1590);
            stage.distanceBackground = Sprite("R15_BG"); stage.frontPlate = Sprite("R08_FrontPlate");
            stage.reverseShot = Sprite("R08B"); stage.runSprite = Sprite("R08_IsolRun");
            var worldFx = Rect("World FX", stageRoot);
            stage.isolReaction = Picture("Isol Reaction", worldFx, Sprite("R12_IsolReaction"));
            stage.toriReaction = Picture("Tori Reaction", worldFx, Sprite("R12_ToriReaction"));
            Place(stage.isolReaction.rectTransform, new Vector2(0.27f, 0.61f), new Vector2(320, 320));
            Place(stage.toriReaction.rectTransform, new Vector2(0.73f, 0.55f), new Vector2(320, 320));
            stage.impact = Picture("Localized Impact", worldFx);
            stage.impact.color = new Color(0.66f, 0.64f, 1, 0.8f);
            stage.impact.rectTransform.sizeDelta = new Vector2(34, 34);
            stage.impact.rectTransform.anchorMin = stage.impact.rectTransform.anchorMax = new Vector2(0.26f, 0.49f);
            stage.impact.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            stage.soundSource = stageRoot.gameObject.AddComponent<AudioSource>(); stage.soundSource.playOnAwake = false;
            stage.latchClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DataRoot + "/latch_seat.wav");
            stage.Clear();
            // Reuse the existing dialogue/TMP/Safe Area. These are outside every art mask/effect.
            var dialogue = view.dialogueGroup.transform;
            var frame = dialogue.Find("NavyFrame")?.GetComponent<Image>(); if (frame != null) frame.color = new Color(0.12f, 0.83f, 0.88f, 0.45f);
            var panel = dialogue.Find("FlatPanel")?.GetComponent<Image>(); if (panel != null) panel.color = new Color(0.035f, 0.055f, 0.13f, 0.84f);
            if (dialogue.Find("Violet Rim") == null)
            {
                var rim = Picture("Violet Rim", dialogue); rim.color = new Color(0.66f, 0.40f, 0.98f, 0.70f);
                rim.rectTransform.anchorMin = new Vector2(1, 0); rim.rectTransform.anchorMax = Vector2.one;
                rim.rectTransform.offsetMin = new Vector2(-4, 8); rim.rectTransform.offsetMax = new Vector2(-1, -8);
            }
            // Lower the panel slightly to leave the foreground hand and faces clear at 9:16.
            var dialogueRect = (RectTransform)dialogue; dialogueRect.offsetMin = new Vector2(24, 118); dialogueRect.offsetMax = new Vector2(-24, 480);
            BuildLore(root, runner, view);
            EditorUtility.SetDirty(view); EditorUtility.SetDirty(runner);
        }

        private static void BuildLore(GameObject root, StoryRunner runner, StoryView view)
        {
            var safe = view.dialogueGroup.transform.parent;
            var rect = Rect("Optional Environment Reading", safe);
            var reader = rect.gameObject.AddComponent<RanchOptionalLore>(); reader.runner = runner;
            reader.catalog = AssetDatabase.LoadAssetAtPath<TextAsset>(DataRoot + "/ranch_cues.json");
            reader.clockButton = LoreButton("기름 묻은 종이", rect, view, new Vector2(0.18f, 0.86f), new Vector2(300, 62));
            reader.roomButton = LoreButton("열쇠 모양 종이", rect, view, new Vector2(0.18f, 0.82f), new Vector2(300, 62));
            var panel = Rect("Environment Text", rect); Place(panel, new Vector2(0.5f, 0.62f), new Vector2(930, 460));
            Picture("Backdrop", panel).color = new Color(0.035f, 0.055f, 0.13f, 0.94f); reader.panel = panel.gameObject;
            reader.body = UnityEngine.Object.Instantiate(view.bodyText, panel); reader.body.name = "Optional Fragment";
            var text = reader.body.rectTransform; text.anchorMin = Vector2.zero; text.anchorMax = Vector2.one; text.offsetMin = new Vector2(36, 100); text.offsetMax = new Vector2(-36, -24);
            reader.body.fontSize = 30; reader.body.overflowMode = TextOverflowModes.Overflow;
            reader.nextButton = LoreButton("다음 문장", panel, view, new Vector2(0.30f, 0.12f), new Vector2(260, 64));
            reader.closeButton = LoreButton("닫기", panel, view, new Vector2(0.72f, 0.12f), new Vector2(260, 64));
            reader.panel.SetActive(false);
            rect.SetAsLastSibling();
        }
        private static Button LoreButton(string label, Transform parent, StoryView view, Vector2 anchor, Vector2 size)
        {
            var rect = Rect(label, parent); Place(rect, anchor, size);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.04f, 0.10f, 0.19f, 0.9f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var text = UnityEngine.Object.Instantiate(view.bodyText, rect); text.name = "Label"; text.text = label; text.fontSize = 25;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
        private static RanchConstellationOverlay Overlay(string name, Transform parent, Vector3 weights)
        {
            var root = Rect(name, parent); var overlay = root.gameObject.AddComponent<RanchConstellationOverlay>();
            var serialized = new SerializedObject(overlay); serialized.FindProperty("shaderAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath); serialized.ApplyModifiedPropertiesWithoutUndo();
            overlay.texture = null; overlay.uvRect = new Rect(0, 0, 1, 1); overlay.raycastTarget = false; overlay.SetLayerWeights(weights); overlay.ClearCue();
            return overlay;
        }
        private static Sprite Sprite(string id) => Resources.Load<Sprite>("ANIMOL/Story/Starway/" + id);
        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
        }
        private static Image Picture(string name, Transform parent, Sprite sprite = null)
        {
            var image = Rect(name, parent).gameObject.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = sprite != null; image.raycastTarget = false; return image;
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
        }
    }
}
#endif
