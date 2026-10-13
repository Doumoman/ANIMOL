#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Animol.Editor
{
    // Reads the atlas JSON shipped beside the PNGs. JSON rects have a top-left origin;
    // Unity's SpriteMetaData rects have a bottom-left origin.
    public static class AnimolAtlasImporter
    {
        private const string AtlasDirectory = "Assets/ANIMOL/Atlases";
        private const string GeneratedDirectory = "Assets/ANIMOL/Generated";
        private const string ClipDirectory = GeneratedDirectory + "/Clips";
        private const string PrefabDirectory = GeneratedDirectory + "/Prefabs";
        private const string TerrainDirectory = GeneratedDirectory + "/TerrainTiles";
        private const string QaScenePath = "Assets/ANIMOL/Scenes/ANIMOL_Atlas_QA.unity";

        [Serializable] private class AtlasSprite
        {
            public string name;
            public int[] rect_px;
            public float[] pivot;
        }

        [Serializable] private class AtlasClip
        {
            public string name;
            public float fps;
            public string[] sprite_names;
        }

        [Serializable] private class AtlasPart
        {
            public string name;
            public string role;
            public int order;
            public AtlasClip[] clips;
        }

        [Serializable] private class AtlasObject
        {
            public string id;
            public int[] size_cells;
            public AtlasPart[] parts;
        }

        [Serializable] private class AtlasTile
        {
            public string name;
            public string sprite_name;
            public string role;
        }

        [Serializable] private class AtlasTheme
        {
            public string name;
            public AtlasTile[] tiles;
        }

        [Serializable] private class AtlasManifest
        {
            public int cell_px;
            public string texture;
            public int[] atlas_px;
            public AtlasSprite[] sprites;
            public AtlasObject[] objects;
            public AtlasTheme[] themes;
        }

        public static void Import()
        {
            try
            {
                var animation = ReadManifest(AtlasDirectory + "/animation_atlas.json");
                var terrain = ReadManifest(AtlasDirectory + "/terrain_atlas.json");
                if (animation.cell_px != 32 || terrain.cell_px != 32)
                    throw new Exception("These atlases must use 32 pixels per tile.");

                EnsureFolder(GeneratedDirectory);
                EnsureFolder(ClipDirectory);
                EnsureFolder(PrefabDirectory);
                EnsureFolder(TerrainDirectory);

                var animationSprites = Slice(animation);
                var terrainSprites = Slice(terrain);
                int objects = CreateObjects(animation, animationSprites);
                int tiles = CreateTerrain(terrain, terrainSprites);
                CreateQaScene(terrain);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"ANIMOL: imported {animationSprites.Count} animation slices, {objects} prefabs, and {tiles} terrain Tile assets.");
            }
            catch (Exception ex)
            {
                Debug.LogError("ANIMOL atlas import failed: " + ex);
            }
        }

        private static AtlasManifest ReadManifest(string assetPath)
        {
            var file = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (file == null)
                throw new Exception("Missing atlas JSON at " + assetPath);
            var manifest = JsonUtility.FromJson<AtlasManifest>(file.text);
            if (manifest == null || manifest.sprites == null || manifest.atlas_px == null || manifest.atlas_px.Length != 2)
                throw new Exception("Invalid atlas JSON at " + assetPath);
            return manifest;
        }

        private static Dictionary<string, Sprite> Slice(AtlasManifest manifest)
        {
            var path = AtlasDirectory + "/" + manifest.texture;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new Exception("Missing PNG texture at " + path);
            if (manifest.atlas_px[0] > 4096 || manifest.atlas_px[1] > 4096)
                throw new Exception("The atlas exceeds the importer maximum of 4096 px: " + path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;

            var metadata = new SpriteMetaData[manifest.sprites.Length];
            for (int i = 0; i < metadata.Length; i++)
            {
                var item = manifest.sprites[i];
                if (item.rect_px == null || item.rect_px.Length != 4)
                    throw new Exception("Invalid sprite rectangle: " + item.name);
                var r = item.rect_px;
                if (r[0] < 0 || r[1] < 0 || r[2] <= 0 || r[3] <= 0 ||
                    r[0] + r[2] > manifest.atlas_px[0] || r[1] + r[3] > manifest.atlas_px[1])
                    throw new Exception("Sprite is outside the atlas: " + item.name);
                float px = item.pivot != null && item.pivot.Length == 2 ? item.pivot[0] : .5f;
                float py = item.pivot != null && item.pivot.Length == 2 ? item.pivot[1] : .5f;
                metadata[i] = new SpriteMetaData
                {
                    name = item.name,
                    rect = new Rect(r[0], manifest.atlas_px[1] - r[1] - r[3], r[2], r[3]),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(px, py)
                };
            }

#pragma warning disable CS0618 // Legacy spritesheet metadata works in Unity 2022.3.
            importer.spritesheet = metadata;
#pragma warning restore CS0618
            importer.SaveAndReimport();
            var result = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>().ToDictionary(sprite => sprite.name, sprite => sprite);
            foreach (var item in manifest.sprites)
                if (!result.ContainsKey(item.name))
                    throw new Exception("Unity failed to slice sprite " + item.name);
            return result;
        }

        private static int CreateObjects(AtlasManifest manifest, Dictionary<string, Sprite> sprites)
        {
            if (manifest.objects == null) throw new Exception("Animation JSON has no objects.");
            foreach (var obj in manifest.objects)
            {
                if (obj.parts == null || obj.parts.Length == 0)
                    throw new Exception("Object has no parts: " + obj.id);
                string objectClipDirectory = ClipDirectory + "/" + obj.id;
                EnsureFolder(objectClipDirectory);

                    var root = new GameObject(obj.id);
                try
                {
                    var animation = root.AddComponent<Animation>();
                    root.AddComponent<AnimolPhasePlayer>();
                    var generatedClips = new List<AnimationClip>();
                    AnimationClip idleClip = null;
                    foreach (var part in obj.parts)
                    {
                        var child = new GameObject(part.name);
                        child.transform.SetParent(root.transform, false);
                        var renderer = child.AddComponent<SpriteRenderer>();
                        renderer.sortingOrder = part.order;
                        var idle = FindClip(part, "idle");
                        if (idle == null || idle.sprite_names == null || idle.sprite_names.Length == 0)
                            throw new Exception("Missing idle sprite for " + obj.id + "/" + part.name);
                        renderer.sprite = RequireSprite(sprites, idle.sprite_names[0]);
                        // Every layer has the same full object canvas, 32 PPU and center pivot.
                        child.transform.localPosition = Vector3.zero;
                        if (RequiresMovingPartCollider(obj.id, part.name))
                        {
                            child.AddComponent<BoxCollider2D>();
                            child.AddComponent<AnimolPartColliderFollower>().Configure(true);
                        }
                    }

                    var states = new HashSet<string> { "idle" };
                    foreach (var part in obj.parts)
                        foreach (var clip in part.clips ?? Array.Empty<AtlasClip>())
                            states.Add(clip.name);
                    foreach (var state in states.OrderBy(s => s == "idle" ? 0 : 1).ThenBy(s => s))
                    {
                        var clip = new AnimationClip { legacy = true, name = state, frameRate = 32 };
                        clip.wrapMode = state == "idle" ? WrapMode.Loop : WrapMode.Once;
                        foreach (var part in obj.parts)
                        {
                            var partClip = FindClip(part, state) ?? FindClip(part, "idle");
                            var names = partClip.sprite_names;
                            float fps = Mathf.Max(1f, partClip.fps);
                            var keys = new ObjectReferenceKeyframe[names.Length];
                            for (int i = 0; i < names.Length; i++)
                            {
                                keys[i] = new ObjectReferenceKeyframe
                                {
                                    time = i / fps,
                                    value = RequireSprite(sprites, names[i])
                                };
                            }
                            AnimationUtility.SetObjectReferenceCurve(clip,
                                EditorCurveBinding.PPtrCurve(part.name, typeof(SpriteRenderer), "m_Sprite"), keys);
                        }
                        var clipPath = objectClipDirectory + "/" + obj.id + "_" + state + ".anim";
                        ReplaceAsset(clipPath);
                        AssetDatabase.CreateAsset(clip, clipPath);
                        generatedClips.Add(clip);
                        if (state == "idle") idleClip = clip;
                    }
                    AnimationUtility.SetAnimationClips(animation, generatedClips.ToArray());
                    animation.clip = idleClip;
                    var prefabPath = PrefabDirectory + "/" + obj.id + ".prefab";
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            return manifest.objects.Length;
        }

        private static bool RequiresMovingPartCollider(string objectId, string partName)
        {
            return (objectId == "C6" && partName == "cart") ||
                   (objectId == "M2" && (partName == "left_plate" || partName == "right_plate")) ||
                   (objectId == "M6" && partName.StartsWith("stair_", StringComparison.Ordinal)) ||
                   (objectId == "R8" && partName == "cart");
        }

        private static int CreateTerrain(AtlasManifest manifest, Dictionary<string, Sprite> sprites)
        {
            if (manifest.themes == null) throw new Exception("Terrain JSON has no themes.");
            int count = 0;
            foreach (var theme in manifest.themes)
            {
                string themeDirectory = TerrainDirectory + "/" + theme.name;
                EnsureFolder(themeDirectory);
                foreach (var entry in theme.tiles)
                {
                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = RequireSprite(sprites, entry.sprite_name);
                    tile.color = Color.white;
                    tile.transform = Matrix4x4.identity;
                    tile.name = theme.name + "_" + entry.name;
                    // One-way behavior needs a PlatformEffector2D on the relevant Tilemap.
                    tile.colliderType = entry.role != null && entry.role.StartsWith("one_way", StringComparison.Ordinal)
                        ? Tile.ColliderType.None : Tile.ColliderType.Sprite;
                    var path = themeDirectory + "/" + tile.name + ".asset";
                    ReplaceAsset(path);
                    AssetDatabase.CreateAsset(tile, path);
                    count++;
                }
            }
            return count;
        }

        private static void CreateQaScene(AtlasManifest terrain)
        {
            EnsureFolder("Assets/ANIMOL/Scenes");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene.name = "ANIMOL_Atlas_QA";
                var root = new GameObject("ANIMOL Atlas QA");

                for (int themeIndex = 0; themeIndex < terrain.themes.Length; themeIndex++)
                {
                    var theme = terrain.themes[themeIndex];
                    var themeRoot = new GameObject("Terrain_" + theme.name);
                    themeRoot.transform.SetParent(root.transform, false);
                    themeRoot.transform.position = new Vector3(-7.5f, 6f - themeIndex * 2f, 0f);
                    for (int tileIndex = 0; tileIndex < theme.tiles.Length; tileIndex++)
                    {
                        var entry = theme.tiles[tileIndex];
                        var tile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainDirectory + "/" + theme.name + "/" + theme.name + "_" + entry.name + ".asset");
                        if (tile == null) throw new Exception("Missing QA terrain tile: " + theme.name + "/" + entry.name);
                        var tileObject = new GameObject(entry.name);
                        tileObject.transform.SetParent(themeRoot.transform, false);
                        tileObject.transform.localPosition = new Vector3(tileIndex, 0f, 0f);
                        tileObject.AddComponent<SpriteRenderer>().sprite = tile.sprite;
                        if (entry.role != null && entry.role.StartsWith("one_way", StringComparison.Ordinal))
                        {
                            var collision = new GameObject("OneWayCollision");
                            collision.transform.SetParent(tileObject.transform, false);
                            collision.transform.localPosition = new Vector3(0f, .45f, 0f);
                            var box = collision.AddComponent<BoxCollider2D>();
                            box.size = new Vector2(1f, .1f);
                            box.usedByEffector = true;
                            collision.AddComponent<PlatformEffector2D>();
                        }
                    }
                }

                var ids = new[] { "C6", "M2", "M6", "R8" };
                for (int i = 0; i < ids.Length; i++)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDirectory + "/" + ids[i] + ".prefab");
                    if (prefab == null) throw new Exception("Missing QA prefab: " + ids[i]);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.name = ids[i] + "_QA";
                    instance.transform.SetParent(root.transform, true);
                    instance.transform.position = new Vector3(-6f + i * 4f, -6f, 0f);
                    instance.AddComponent<AnimolQaPhaseCycler>();
                }

                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 9f;
                camera.backgroundColor = new Color(.035f, .045f, .075f, 1f);
                camera.clearFlags = CameraClearFlags.SolidColor;

                EditorSceneManager.SaveScene(scene, QaScenePath);
            }
            finally
            {
                if (setup != null && setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static AtlasClip FindClip(AtlasPart part, string name)
        {
            return (part.clips ?? Array.Empty<AtlasClip>()).FirstOrDefault(clip => clip.name == name);
        }

        private static Sprite RequireSprite(Dictionary<string, Sprite> sprites, string name)
        {
            if (!sprites.TryGetValue(name, out var sprite))
                throw new Exception("Missing atlas sprite " + name);
            return sprite;
        }

        private static void ReplaceAsset(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                string parent = string.Join("/", parts.Take(i).ToArray());
                string folder = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent, parts[i]);
            }
        }
    }
}
#endif
