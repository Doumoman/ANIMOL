using System;
using System.IO;
using ANIMOL.Menu;
using DiagonalPattern;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Animol.UI.Pattern.Editor
{
    public static class AnimolMonochromePatternSetup
    {
        private const string Root = "Assets/ANIMOL/Sprites/UI/Pattern/";
        private const string ShaderPath = "Assets/ANIMOL/Materials/UI_AnimolMonochromePattern.shader";
        private const string TexturePath = Root + "Textures/CharacterFacesStars_All_2048.png";
        private const string ScenePath = "Assets/ANIMOL/Scenes/Runtime/ANIMOL_Main.unity";
        private static readonly Color Ink = new Color(166f / 255f, 110f / 255f, 72f / 255f, 1f);
        private static bool importsConfigured;

        [MenuItem("GameObject/UI/ANIMOL/Monochrome Pattern In Selected Blank Panel", false, 2062)]
        public static void Create(MenuCommand command)
        {
            GameObject selected = command.context as GameObject;
            if (!selected) selected = Selection.activeGameObject;
            RectTransform host = selected ? selected.GetComponent<RectTransform>() : null;
            if (!host || !host.GetComponentInParent<Canvas>())
                throw new InvalidOperationException("Select the inset blank apricot PatternHost inside your existing menu Canvas. Do not select the illustrated background.");
            if (host.Find("Character Faces And Stars"))
                throw new InvalidOperationException("This blank panel already contains a pattern. Update the existing component.");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (!shader || !texture)
                throw new InvalidOperationException("Import the ANIMOL pattern shader, texture and supplied DiagonalPatternBackground runtime first.");

            ConfigureTextureImports();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            GameObject go = new GameObject("Character Faces And Stars", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(RawImage));
            Undo.RegisterCreatedObjectUndo(go, "Create ANIMOL pattern");
            go.transform.SetParent(host, false);
            go.transform.SetAsFirstSibling();
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            RawImage image = go.GetComponent<RawImage>();
            image.color = Color.white;
            image.texture = texture;
            image.raycastTarget = false;
            image.uvRect = new Rect(0, 0, 1, 1);
            DiagonalPatternBackground effect = Undo.AddComponent<DiagonalPatternBackground>(go);
            effect.Configure(shader, texture);
            // The host already paints the apricot background. Only pattern ink is added.
            effect.SetAppearance(texture, new Color(0, 0, 0, 0),
                Ink, 0.16f);
            effect.VisualVelocity = new Vector2(8, 8);
            SerializedObject settings = new SerializedObject(effect);
            settings.FindProperty("tileSize").vector2Value = new Vector2(960, 960);
            settings.FindProperty("useUnscaledTime").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Undo.AddComponent<AnimolPatternMotion>(go);
            EditorUtility.SetDirty(effect);
            Selection.activeGameObject = go;
        }

        [MenuItem("Tools/ANIMOL/Pattern/Apply To Active Main Lobby")]
        public static void ApplyToMainLobby()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before installing the pattern.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save or close the current unsaved scene before applying the main lobby pattern.");
            importsConfigured = false;
            ConfigureTextureImports();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var lobby = UnityEngine.Object.FindFirstObjectByType<MainLobbyController>();
            if (!lobby || !lobby.canvas || !lobby.dock || !lobby.campaignPanel || !lobby.modalPanel)
                throw new InvalidDataException("The active main lobby Canvas is incomplete.");
            if (lobby.canvas.GetComponentsInChildren<DiagonalPatternBackground>(true).Length > 7)
                throw new InvalidDataException("Unexpected extra pattern objects in the main lobby.");

            // The original panels paint the apricot surface. Patterns are their
            // first children, below existing art, labels, buttons and frames.
            Ensure(lobby.dock, .18f, new Vector2(2, 2), new Vector2(-2, -2));
            var campaign = (RectTransform)lobby.campaignPanel.transform;
            Ensure(campaign, .16f, new Vector2(2, 2), new Vector2(-2, -2));
            foreach (var card in lobby.themeButtons)
            {
                if (!card) throw new InvalidDataException("Missing campaign card.");
                var caption = card.transform.Find("Caption") as RectTransform;
                if (!caption || !caption.GetComponent<Image>() || !caption.Find("Theme Description"))
                    throw new InvalidDataException("A card does not have the existing blank caption surface.");
                Ensure(caption, .12f, new Vector2(12, 8), new Vector2(-12, -8));
            }
            var sheet = lobby.modalPanel.transform.Find("Modal Sheet") as RectTransform;
            if (!sheet || !sheet.GetComponent<Image>())
                throw new InvalidDataException("The existing modal sheet is missing.");
            Ensure(sheet, .10f, new Vector2(12, 12), new Vector2(-12, -12));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            ValidateInstallation();
            Debug.Log("Main lobby v10 pattern installed in 7 blank surfaces. Existing catalog, scenery, characters and buttons retained.");
        }

        private static void Ensure(RectTransform host, float opacity, Vector2 insetMin, Vector2 insetMax)
        {
            var child = host.Find("Character Faces And Stars") as RectTransform;
            if (!child)
            {
                Create(new MenuCommand(host.gameObject));
                child = host.Find("Character Faces And Stars") as RectTransform;
            }
            if (!child || !child.GetComponent<RawImage>() || !child.GetComponent<AnimolPatternMotion>())
                throw new InvalidDataException("Pattern host is incomplete: " + host.name);
            child.SetAsFirstSibling();
            child.anchorMin = Vector2.zero;
            child.anchorMax = Vector2.one;
            child.offsetMin = insetMin;
            child.offsetMax = insetMax;
            var image = child.GetComponent<RawImage>();
            image.color = Color.white;
            image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            image.uvRect = new Rect(0, 0, 1, 1);
            image.raycastTarget = false;
            var effect = child.GetComponent<DiagonalPatternBackground>();
            effect.Configure(AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
            effect.SetAppearance(AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath), Color.clear, Ink, opacity);
            effect.VisualVelocity = new Vector2(8, 8);
            var settings = new SerializedObject(effect);
            settings.FindProperty("tileSize").vector2Value = new Vector2(960, 960);
            settings.FindProperty("useUnscaledTime").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);
            EditorUtility.SetDirty(child);
        }

        private static void ConfigureTextureImports()
        {
            if (importsConfigured) return;
            var faces = Directory.GetFiles(Root + "Faces", "*.png");
            if (faces.Length != 16)
                throw new InvalidDataException("Expected 16 independent face sprites, found " + faces.Length);
            foreach (var path in faces)
                ConfigureSprite(path.Replace('\\', '/'), 576);
            ConfigureSprite(Root + "Ornaments/Star_FourPoint.png", 128);
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (!importer) throw new FileNotFoundException("Missing v10 repeat texture", TexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            importsConfigured = true;
        }

        private static void ConfigureSprite(string path, int size)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new FileNotFoundException("Missing individual pattern sprite", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spritePivot = new Vector2(.5f, .5f);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.maxTextureSize = 1024;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(.5f, .5f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite || sprite.rect.width != size || sprite.rect.height != size)
                throw new InvalidDataException("Pattern sprite dimensions changed: " + path);
        }

        [MenuItem("Tools/ANIMOL/Pattern/Validate Installation")]
        public static void ValidateInstallation()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/ANIMOL/Scripts/Runtime/AnimolPatternMotion.cs");
            if (!shader || !texture || !script || script.GetClass() != typeof(AnimolPatternMotion))
                throw new InvalidOperationException("ANIMOL pattern assets or C# classes did not resolve.");
            var facePaths = Directory.GetFiles(Root + "Faces", "*.png");
            if (facePaths.Length != 16)
                throw new InvalidDataException("Expected 16 independent face sprites.");
            foreach (var path in facePaths)
            {
                var assetPath = path.Replace('\\', '/');
                var faceImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                var face = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                var settings = new TextureImporterSettings();
                faceImporter?.ReadTextureSettings(settings);
                if (!face || face.rect.size != new Vector2(576, 576) ||
                    !faceImporter || faceImporter.textureType != TextureImporterType.Sprite ||
                    faceImporter.spriteImportMode != SpriteImportMode.Single ||
                    faceImporter.wrapMode != TextureWrapMode.Clamp ||
                    settings.spriteMeshType != SpriteMeshType.FullRect)
                    throw new InvalidDataException("Incorrect independent face import: " + assetPath);
            }
            if (ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("ANIMOL monochrome shader has an import error.");
            var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (!importer || importer.textureType != TextureImporterType.Default ||
                importer.wrapMode != TextureWrapMode.Repeat || importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                importer.maxTextureSize < 2048)
                throw new InvalidDataException("Repeat texture import settings are incorrect.");
            if (SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current unsaved scene before validating the main lobby pattern.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var lobby = UnityEngine.Object.FindFirstObjectByType<MainLobbyController>();
            if (!lobby) throw new InvalidDataException("Main lobby scene is missing.");
            var patterns = lobby.canvas.GetComponentsInChildren<DiagonalPatternBackground>(true);
            if (patterns.Length != 7) throw new InvalidDataException("Expected 7 blank-surface patterns, found " + patterns.Length);
            foreach (var pattern in patterns)
            {
                var data = new SerializedObject(pattern);
                if (data.FindProperty("patternTexture").objectReferenceValue != texture ||
                    data.FindProperty("scrollingShader").objectReferenceValue != shader)
                    throw new InvalidDataException("A pattern lost its serialized shader or texture reference.");
                var image = pattern.GetComponent<RawImage>();
                if (!image || image.raycastTarget || image.color != Color.white ||
                    image.uvRect != new Rect(0, 0, 1, 1))
                    throw new InvalidDataException("A pattern is intercepting UI input or has the wrong UV/color.");
            }
            Debug.Log("Pattern import and 7 main-lobby blank-surface references validated: " + scene.path);
        }
    }
}
