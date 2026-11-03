using System;
using DiagonalPattern;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Animol.UI.Pattern.Editor
{
    public static class AnimolMonochromePatternSetup
    {
        private const string Root = "Assets/ANIMOL/UI/Pattern/";
        private const string ShaderPath = Root + "Shaders/UI_AnimolMonochromePattern.shader";
        private const string TexturePath = Root + "Textures/CharacterFacesStars_All_2048.png";

        [MenuItem("GameObject/UI/ANIMOL/Monochrome Pattern In Selected Blank Panel", false, 2062)]
        public static void Create(MenuCommand command)
        {
            GameObject selected = command.context as GameObject;
            if (!selected) selected = Selection.activeGameObject;
            RectTransform host = selected ? selected.GetComponent<RectTransform>() : null;
            if (!host || !host.GetComponentInParent<Canvas>())
                throw new InvalidOperationException("Select the inset blank apricot PatternHost inside your existing menu Canvas. Do not select the illustrated background.");
            if (host.GetComponentInChildren<DiagonalPatternBackground>(true))
                throw new InvalidOperationException("This blank panel already contains a pattern. Update the existing component.");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (!shader || !texture)
                throw new InvalidOperationException("Import the ANIMOL pattern shader, texture and supplied DiagonalPatternBackground runtime first.");

            TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            }
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
            image.raycastTarget = false;
            image.uvRect = new Rect(0, 0, 1, 1);
            DiagonalPatternBackground effect = Undo.AddComponent<DiagonalPatternBackground>(go);
            effect.Configure(shader, texture);
            // The host already paints the apricot background. Only pattern ink is added.
            effect.SetAppearance(texture, new Color(0, 0, 0, 0),
                new Color(166f/255f, 110f/255f, 72f/255f, 1), 0.16f);
            effect.VisualVelocity = new Vector2(8, 8);
            SerializedObject settings = new SerializedObject(effect);
            settings.FindProperty("tileSize").vector2Value = new Vector2(960, 960);
            settings.FindProperty("useUnscaledTime").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Undo.AddComponent<AnimolPatternMotion>(go);
            EditorUtility.SetDirty(effect);
            Selection.activeGameObject = go;
        }

        [MenuItem("Tools/ANIMOL/Pattern/Validate Installation")]
        public static void ValidateInstallation()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "Runtime/AnimolPatternMotion.cs");
            if (!shader || !texture || !script || script.GetClass() != typeof(AnimolPatternMotion))
                throw new InvalidOperationException("ANIMOL pattern assets or C# classes did not resolve.");
            if (ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("ANIMOL monochrome shader has an import error.");
            Debug.Log("Pattern shader and C# references resolved. Test actual clipping, menu layering and target device graphics next.");
        }
    }
}
