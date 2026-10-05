using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Animol.TerrainStructure.Editor
{
    /// <summary>Isolated visual/collision preview. Never changes a production scene or loader.</summary>
    public static class AnimolTerrainMapPreview
    {
        private const string Root = "Assets/ANIMOL/TerrainStructure";
        public static void Create(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap map)
        {
            if (!AnimolTerrainPlacementEngine.Resolve(catalog, map, out AnimolTerrainResolveResult result, out string error))
                throw new InvalidOperationException(error);
            foreach (AnimolTerrainPlacement placement in map.placements)
            {
                AnimolTerrainCatalogEntry entry = result.entriesById[placement.catalogId];
                AnimolTerrainArtFrameDefinition frame = Frame(entry);
                if (frame == null || !frame.ValidateForFootprint(entry.width, entry.height, out error))
                    throw new InvalidOperationException("먼저 원본 아트를 초기화하세요: " + entry.id + ". " + error);
            }
            EnsureFolder(Root + "/Generated/MapPreviews");
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                if (!SceneManager.SetActiveScene(scene))
                    throw new InvalidOperationException("미리보기 씬을 활성화하지 못했습니다.");
                GameObject gridObject = new GameObject("ANIMOL_Standalone_Map_Preview");
                SceneManager.MoveGameObjectToScene(gridObject, scene);
                Grid grid = gridObject.AddComponent<Grid>(); grid.cellSize = Vector3.one;
                GameObject collisionObject = new GameObject("Derived_Solid_Cells_PREVIEW_ONLY");
                collisionObject.transform.SetParent(gridObject.transform, false);
                Tilemap solid = collisionObject.AddComponent<Tilemap>();
                collisionObject.AddComponent<TilemapRenderer>().enabled = false;
                Rigidbody2D body = collisionObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
                collisionObject.AddComponent<TilemapCollider2D>();
                Tile collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(Root + "/Generated/DEV_Solid.asset");
                if (collisionTile == null)
                {
                    // The art initializer makes this asset. Do not save an unreferenced temporary Tile.
                    throw new InvalidOperationException("DEV_Solid.asset이 없습니다. 원본 아트를 초기화하세요.");
                }
                foreach (Vector2Int cell in result.solids.Keys) solid.SetTile(new Vector3Int(cell.x, cell.y, 0), collisionTile);
                foreach (AnimolTerrainPlacement p in result.placements) AddArt(gridObject.transform, p, result.entriesById[p.catalogId], 10);
                foreach (AnimolTerrainPlacement p in result.overlays) AddArt(gridObject.transform, p, result.entriesById[p.catalogId], 30);
                // Base-cell texture is deliberately a preview marker; production resolves the
                // existing nine-direction tile/variant registry and draws ordinary terrain above art.
                Tilemap baseVisual = NewVisualTilemap(gridObject.transform, "Ordinary_1x1_Cells_DIAGNOSTIC", 20);
                Tile marker = MarkerTile();
                foreach (AnimolTerrainBaseCell c in map.baseCells) baseVisual.SetTile(new Vector3Int(c.x, c.y, 0), marker);
                Vector2 min = new Vector2(-1, -1), max = new Vector2(15, 8);
                if (result.solids.Count > 0)
                {
                    min = new Vector2(result.solids.Keys.Min(c => c.x) - 1, result.solids.Keys.Min(c => c.y) - 1);
                    max = new Vector2(result.solids.Keys.Max(c => c.x) + 2, result.solids.Keys.Max(c => c.y) + 2);
                }
                GameObject cameraObject = new GameObject("Preview_Camera"); cameraObject.transform.SetParent(gridObject.transform, false);
                Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(26 / 255f, 28 / 255f, 44 / 255f);
                cameraObject.transform.position = new Vector3((min.x + max.x) * .5f, (min.y + max.y) * .5f, -10);
                camera.orthographicSize = Mathf.Max((max.y - min.y) * .5f, (max.x - min.x) * .5f / (16f / 9f));
                string path = AssetDatabase.GenerateUniqueAssetPath(Root + "/Generated/MapPreviews/" + map.themeId + "_Map_Preview.unity");
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new InvalidOperationException("미리보기 씬을 저장하지 못했습니다: " + path);
                Selection.activeGameObject = gridObject;
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(new Bounds((min + max) * .5f, max - min), false);
                Debug.Log("ANIMOL standalone preview saved: " + path + ". Production runtime integration and play QA are separate checks.");
            }
            catch
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                throw;
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }
        }
        private static AnimolTerrainArtFrameDefinition Frame(AnimolTerrainCatalogEntry e) { return AssetDatabase.LoadAssetAtPath<AnimolTerrainArtFrameDefinition>(Root + "/Generated/SourceFrames/" + e.frameId + ".asset"); }
        private static void AddArt(Transform parent, AnimolTerrainPlacement p, AnimolTerrainCatalogEntry e, int order)
        {
            AnimolTerrainArtFrameDefinition frame = Frame(e);
            GameObject owner = new GameObject(p.instanceId + "_" + e.id); owner.transform.SetParent(parent, false);
            owner.transform.localPosition = new Vector3(p.x, p.y, 0);
            GameObject art = new GameObject("Complete_Art_NO_COLLIDER"); art.transform.SetParent(owner.transform, false);
            art.transform.localPosition = frame.VisualOffset; art.transform.localScale = Vector3.one * frame.UniformScale;
            SpriteRenderer renderer = art.AddComponent<SpriteRenderer>(); renderer.sprite = frame.Sprite;
            renderer.sharedMaterial = frame.Material; renderer.sortingOrder = order;
        }
        private static Tilemap NewVisualTilemap(Transform parent, string name, int order)
        {
            GameObject obj = new GameObject(name); obj.transform.SetParent(parent, false);
            Tilemap tilemap = obj.AddComponent<Tilemap>(); obj.AddComponent<TilemapRenderer>().sortingOrder = order;
            return tilemap;
        }
        private static Tile MarkerTile()
        {
            string path = Root + "/Generated/MapPreviews/DIAGNOSTIC_BaseCell.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false); texture.name = "DIAGNOSTIC_BaseCell_Texture";
            texture.SetPixel(0, 0, Color.white); texture.Apply(); texture.filterMode = FilterMode.Point;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1); sprite.name = "DIAGNOSTIC_BaseCell_Sprite";
            tile = ScriptableObject.CreateInstance<Tile>(); tile.name = "DIAGNOSTIC_BaseCell"; tile.sprite = sprite;
            tile.color = new Color(.34f, .42f, .53f); tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path); AssetDatabase.AddObjectToAsset(texture, tile); AssetDatabase.AddObjectToAsset(sprite, tile); AssetDatabase.SaveAssets();
            return tile;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = PathParent(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
        private static string PathParent(string path) { return System.IO.Path.GetDirectoryName(path).Replace('\\', '/'); }
    }
}
