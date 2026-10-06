using System.Linq;
using System.Collections.Generic;
using ANIMOL.Gameplay.ObstacleGraphics;
using Animol.TerrainStructure;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Development
{
    public static class TerrainEditorArt
    {
        public const int PreviewLayer = 31;

        // Never Instantiate a gameplay prefab for a thumbnail/edit preview: Awake/OnEnable
        // could modify singleton state, register triggers, or create colliders.
        public static GameObject CopySprites(GameObject source, Transform parent)
        {
            var root = new GameObject(source == null ? "Missing preview" : source.name);
            root.transform.SetParent(parent, false);
            if (source == null) return root;
            foreach (var original in source.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (original.sprite == null || !original.enabled) continue;
                var child = new GameObject(original.name);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = source.transform.InverseTransformPoint(original.transform.position);
                child.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * original.transform.rotation;
                var scale = original.transform.lossyScale;
                var parentScale = source.transform.lossyScale;
                child.transform.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, 1);
                var renderer = child.AddComponent<SpriteRenderer>();
                renderer.sprite = original.sprite; renderer.sharedMaterial = original.sharedMaterial;
                renderer.color = original.color; renderer.flipX = original.flipX; renderer.flipY = original.flipY;
                renderer.drawMode = original.drawMode; renderer.size = original.size;
                renderer.sortingOrder = original.sortingOrder;
            }
            SetLayer(root);
            return root;
        }

        public static GameObject BuildMap(ITerrainEditorAdapter adapter, Transform parent)
        {
            var map = adapter.Map;
            var units = map.GetEditorPreviewUnitsPerCell();
            var resolved = map.ResolveTerrain(adapter.Terrain);
            var root = new GameObject("Edit artwork · no gameplay or physics");
            root.transform.SetParent(parent, false);
            var grid = root.AddComponent<Grid>(); grid.cellSize = Vector3.one * units;
            var catalog = Resources.Load<StageTerrainTileCatalog>("ANIMOL_TerrainTileCatalog");
            foreach (StageMapLayer layer in System.Enum.GetValues(typeof(StageMapLayer)))
            {
                var go = new GameObject(layer.ToString(), typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(root.transform, false);
                var tilemap = go.GetComponent<Tilemap>();
                go.GetComponent<TilemapRenderer>().sortingOrder = layer == StageMapLayer.Terrain ? 20 : layer == StageMapLayer.Background ? 0 : 35;
                foreach (var c in map.Cells.Where(c => c.Layer == layer))
                {
                    var tile = catalog?.Find(c.TileId);
                    if (tile == null && layer == StageMapLayer.Terrain) tile = catalog?.FindThemeDefault(map.ThemeId);
                    if (tile != null) tilemap.SetTile(new Vector3Int(c.X, c.Y, 0), tile);
                }
            }
            foreach (var p in map.TerrainPlacements.placements)
            {
                var e = resolved.entriesById[p.catalogId];
                StageTerrainStructureRuntime.CreateArt(root.transform, p, e, adapter.Terrain.Frame(e.frameId), units, e.kind == "Structure" ? 10 : 30);
            }
            var obstacles=ObstaclePreview(map);
            if(map.HasFreeShape || obstacles.Count>0)
            {var free=new GameObject("FreeShape artwork");free.transform.SetParent(root.transform,false);free.AddComponent<FreeShapeTerrainRenderer>().Rebuild(map.FreeShapeTerrain,units,obstacles);}
            foreach (var p in map.Objects)
            {
                if(CommonObstacleCatalog.IsRetired(p.Kind)) continue;
                if(CommonObstacleCatalog.IsCurrent(p.Kind)) continue; // Current devices render exclusively through the V6 registry.
                if(obstacles.ContainsKey(new Vector2Int(p.X,p.Y)) && ObstacleSnapshotAdapter.Kind(p.Kind)!=null)continue;
                var art = CopySprites(p.Prefab != null ? p.Prefab : adapter.Objects?.Find(p)?.Prefab, root.transform);
                art.name = p.StableId;
                art.transform.localPosition = new Vector3(p.X, p.Y, 0) * units;
                art.transform.localScale = Vector3.one * units;
                if(art.GetComponentsInChildren<SpriteRenderer>().Length==0)
                {
                    var text=art.AddComponent<TextMesh>();text.text=p.Kind.ToString();text.font=adapter.Font;text.fontSize=24;text.characterSize=.055f;
                    text.color=TerrainEditorScreen.Gold;text.anchor=TextAnchor.MiddleCenter;
                    art.GetComponent<MeshRenderer>().sharedMaterial=adapter.Font.material;art.GetComponent<MeshRenderer>().sortingOrder=40;
                    art.transform.localPosition+=new Vector3(.5f,.5f,0)*units;
                }
            }
            SetLayer(root);
            return root;
        }

        public static Dictionary<Vector2Int,GraphicCell> ObstaclePreview(StageMapDefinition map)
        {
            var cells=new Dictionary<Vector2Int,GraphicCell>();
            foreach(var p in map.Objects)
            {
                try
                {
                    var cell=ObstacleSnapshotAdapter.Read(map,p);if(cell==null)continue;
                    var candidate=new Dictionary<Vector2Int,GraphicCell>(cells);candidate.Add(new Vector2Int(p.X,p.Y),cell);
                    ObstacleSnapshotAdapter.Validate(map.FreeShapeTerrain,candidate);cells=candidate;
                }
                catch(System.InvalidOperationException){ /* Invalid current devices have no legacy-art fallback; placement validation reports the error. */ }
            }
            return cells;
        }
        public static GameObject BuildObstacle(StageMapDefinition map,StageMapObjectPlacement placement,Transform parent)
        {
            var root=new GameObject("V6 obstacle preview");root.transform.SetParent(parent,false);
            var cell=ObstacleSnapshotAdapter.Read(map,placement);
            var position=new Vector2Int(placement.X,placement.Y);float units=map.GetEditorPreviewUnitsPerCell();
            var devices=ObstaclePreview(map);devices.Remove(position);
            var old=map.Objects.FirstOrDefault(p=>p.StableId==placement.StableId);if(old!=null)devices.Remove(new Vector2Int(old.X,old.Y));
            devices[position]=cell;var context=ObstacleSnapshotAdapter.Merge(map.FreeShapeTerrain,devices);
            root.transform.localPosition=-(Vector3)(Vector2)position*units;
            root.AddComponent<FreeShapeTerrainRenderer>().Rebuild(new FreeShapeLayer{seed=map.FreeShapeTerrain.seed},units,
                new Dictionary<Vector2Int,GraphicCell>{{position,cell}},context);
            SetLayer(root);return root;
        }

        public static void SetLayer(GameObject root)
        { foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = PreviewLayer; }
    }
}
