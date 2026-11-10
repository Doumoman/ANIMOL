using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using William.NotebookUI;

// Update only the catalog's map nodes/links and clear-button label in clean
// prefab assets. Existing user Scene/MainMenuCatalog edits are never saved.
public static class ApplyTwentyStageCampaign
{
    const string Textures = "Assets/ANIMOL/Art/UI/ConstellationMap/Textures/";
    const string Notebook = "Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab";
    const string Gameplay = "Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab";
    static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
    }
    static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Textures + path + ".png")
        ?? throw new InvalidOperationException("Missing stage-link sprite: " + path);
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Use idle Edit Mode.");
        foreach (string path in Directory.GetFiles(Textures + "Map/StageLinks", "*.png"))
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        var catalog = NotebookCatalog.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ANIMOL/Data/UI/Notebook/catalog.json"));
        if (catalog.themes.Any(t => catalog.stages.Count(s => s.themeId == t.id) != NotebookCampaignStages.PerTheme))
            throw new InvalidOperationException("Every theme must have 20 stages.");
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(Notebook);
        var originalRoot = original.GetComponentInChildren<NotebookRoot>(true);
        string Relative(Transform transform) => transform == original.transform ? "" :
            AnimationUtility.CalculateTransformPath(transform, original.transform);
        // ExecuteAlways layout components reflow on prefab load. Preserve every
        // non-map RectTransform's authored values rather than saving that cache.
        var rectangles = original.GetComponentsInChildren<RectTransform>(true)
            .Where(r => r != originalRoot.stageScreen.transform && !r.IsChildOf(originalRoot.stageScreen.transform))
            .ToDictionary(r => Relative(r), r => (r.anchorMin, r.anchorMax, r.pivot, r.anchoredPosition, r.sizeDelta));
        float scaleFactor = original.GetComponent<CanvasScaler>().scaleFactor;
        var contents = PrefabUtility.LoadPrefabContents(Notebook);
        try
        {
            var root = contents.GetComponentInChildren<NotebookRoot>(true);
            var oldNodes = root.nodes.ToDictionary(n => n.stageId);
            var nodes = catalog.stages.Select(s =>
            {
                var node = oldNodes[s.id];
                Place((RectTransform)node.button.transform, s.x - 80, s.y - 80, 160, 198);
                node.number.text = s.order.ToString("00"); node.label.text = s.label;
                return node;
            }).ToArray();
            foreach (var node in root.nodes.Except(nodes)) UnityEngine.Object.DestroyImmediate(node.button.gameObject);
            root.nodes = nodes;
            var oldEdges = root.edges.ToDictionary(e => e.from + ">" + e.to);
            var edges = catalog.edges.Select(e =>
            {
                var view = oldEdges[e.from + ">" + e.to];
                Place(view.image.rectTransform, e.x, e.y, e.width, e.height);
                view.future = Sprite(e.sprite + "_Future"); view.cleared = Sprite(e.sprite + "_Cleared");
                view.image.sprite = view.future; view.image.color = Color.white; view.image.raycastTarget = false;
                return view;
            }).ToArray();
            foreach (var edge in root.edges.Except(edges)) UnityEngine.Object.DestroyImmediate(edge.image.gameObject);
            root.edges = edges;
            contents.GetComponent<CanvasScaler>().scaleFactor = scaleFactor;
            foreach (var pair in rectangles.OrderBy(p => p.Key.Count(c => c == '/')))
            {
                var rect = (RectTransform)(pair.Key == "" ? contents.transform : contents.transform.Find(pair.Key));
                var saved = pair.Value;
                rect.anchorMin = saved.anchorMin; rect.anchorMax = saved.anchorMax; rect.pivot = saved.pivot;
                rect.anchoredPosition = saved.anchoredPosition; rect.sizeDelta = saved.sizeDelta;
            }
            PrefabUtility.SaveAsPrefabAsset(contents, Notebook);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        original = AssetDatabase.LoadAssetAtPath<GameObject>(Gameplay);
        var hudRectangles = original.GetComponentsInChildren<RectTransform>(true).ToDictionary(
            r => AnimationUtility.CalculateTransformPath(r, original.transform),
            r => (r.anchorMin, r.anchorMax, r.pivot, r.anchoredPosition, r.sizeDelta));
        contents = PrefabUtility.LoadPrefabContents(Gameplay);
        try
        {
            var hud = contents.GetComponentInChildren<NotebookGameplayHud>(true);
            hud.nextLabel.text = "다음 스테이지로";
            foreach (var pair in hudRectangles.OrderBy(p => p.Key.Count(c => c == '/')))
            {
                var rect = (RectTransform)(pair.Key == "" ? contents.transform : contents.transform.Find(pair.Key));
                var saved = pair.Value;
                rect.anchorMin = saved.anchorMin; rect.anchorMax = saved.anchorMax; rect.pivot = saved.pivot;
                rect.anchoredPosition = saved.anchoredPosition; rect.sizeDelta = saved.sizeDelta;
            }
            PrefabUtility.SaveAsPrefabAsset(contents, Gameplay);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        return new { themes = catalog.themes.Length, stages = catalog.stages.Length, links = catalog.edges.Length };
    }
}
