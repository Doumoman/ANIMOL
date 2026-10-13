using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ANIMOL.Gameplay
{
    public sealed partial class StageMapRuntimeLoader
    {
        private GameObject generatedTerrainOwner;
        private Tilemap generatedOneWay;
        private bool standaloneDevelopmentOwner;
        // The V4 development session has no baked scene terrain. Ordinary-cell-only maps
        // therefore need this same operational owner, even when placements are empty.
        public void ConfigureStandaloneDevelopmentOwner() { standaloneDevelopmentOwner = true; }
        private readonly Dictionary<GameObject, bool> suspendedLegacyTerrain = new Dictionary<GameObject, bool>();

        // The existing M9 local-play builder binds only map/objects and bakes greybox segments.
        // Its loader now owns a logical Grid too; this is not the package's additive MapPreview.
        private void PrepareTerrainPhysicsOwner(StageMapDefinition definition, bool structures)
        {
            if (generatedOneWay != null) generatedOneWay.ClearAllTiles();
            if (!structures && !standaloneDevelopmentOwner)
            {
                foreach (var pair in suspendedLegacyTerrain) if (pair.Key != null) pair.Key.SetActive(pair.Value);
                suspendedLegacyTerrain.Clear();
                if (generatedTerrainOwner != null) generatedTerrainOwner.SetActive(false);
                return;
            }
            if (terrain == null)
            {
                generatedTerrainOwner = new GameObject("OperationalTerrainGrid", typeof(Grid));
                generatedTerrainOwner.transform.SetParent(transform, false);
                terrain = NewOwnedTilemap("Terrain", generatedTerrainOwner.transform);
                generatedOneWay = NewOwnedTilemap("LegacyOneWayCells", generatedTerrainOwner.transform);
                var body = generatedOneWay.gameObject.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
                var effector = generatedOneWay.gameObject.AddComponent<PlatformEffector2D>(); effector.useOneWay = true;
                var composite=generatedOneWay.gameObject.AddComponent<CompositeCollider2D>();
                composite.geometryType=CompositeCollider2D.GeometryType.Polygons;
                generatedOneWay.GetComponent<TilemapCollider2D>().compositeOperation=Collider2D.CompositeOperation.Merge;
                composite.usedByEffector=true;
            }
            if (generatedTerrainOwner == null) return; // Campaign scene already has its operational owner.
            generatedTerrainOwner.SetActive(true);
            // Exact host-generated names, restricted to its actual M9 root. No arbitrary scene colliders are removed.
            var legacyRoot = transform.parent;
            if (legacyRoot == null || legacyRoot.name != "M9_T01_S01_Greybox") return;
            foreach (var group in definition.Cells.Where(c => c.Layer == StageMapLayer.Terrain).GroupBy(c => (c.TileId, c.Y)))
            {
                var ordered = group.OrderBy(c => c.X).ToArray(); int start = ordered[0].X, previous = start;
                for (int index = 1; index <= ordered.Length; index++)
                {
                    if (index < ordered.Length && ordered[index].X == previous + 1) { previous = ordered[index].X; continue; }
                    var prefix = group.Key.TileId == "M9_MOON_ONE_WAY_16PX_PLACEHOLDER" ? "OneWay" : "Solid";
                    var child = legacyRoot.Find($"{prefix}_{group.Key.Y}_{start}_{previous}");
                    if (child != null && child.GetComponent<BoxCollider2D>() != null)
                    {
                        if (!suspendedLegacyTerrain.ContainsKey(child.gameObject)) suspendedLegacyTerrain.Add(child.gameObject, child.gameObject.activeSelf);
                        child.gameObject.SetActive(false);
                    }
                    if (index < ordered.Length) start = previous = ordered[index].X;
                }
            }
        }

        private static Tilemap NewOwnedTilemap(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
            go.transform.SetParent(parent, false); go.GetComponent<TilemapRenderer>().sortingOrder = 20;
            return go.GetComponent<Tilemap>();
        }
    }
}
