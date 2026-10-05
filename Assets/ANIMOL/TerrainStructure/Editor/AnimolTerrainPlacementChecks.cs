using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Engine = Animol.TerrainStructure.AnimolTerrainPlacementEngine;

namespace Animol.TerrainStructure.Editor
{
    /// <summary>
    /// Run in Unity: real catalog/example JSON and pure engine transactions.
    /// This menu does not verify Physics, Shader rendering or Play Mode.
    /// </summary>
    public static class AnimolTerrainPlacementChecks
    {
        private const string Root = "Assets/ANIMOL/TerrainStructure/Data/";

        [MenuItem("ANIMOL/Terrain Structure/Validate Saved Map Contract")]
        public static void Run()
        {
            AnimolTerrainCatalogData catalog = JsonUtility.FromJson<AnimolTerrainCatalogData>(Load("editor_tile_catalog.json"));
            Require(Engine.ValidateCatalog(catalog, out string error), error);
            for (int theme = 1; theme <= 5; theme++)
            {
                AnimolTerrainSavedMap map = JsonUtility.FromJson<AnimolTerrainSavedMap>(
                    Load("ExampleMaps/T" + theme.ToString("00") + "_SavedMap_Example.json"));
                AnimolTerrainResolveResult before = Resolve(catalog, map);
                string json = JsonUtility.ToJson(map);
                AnimolTerrainSavedMap roundTrip = JsonUtility.FromJson<AnimolTerrainSavedMap>(json);
                SameResolved(before, Resolve(catalog, roundTrip));
                Require(json == JsonUtility.ToJson(roundTrip), "Example JSON round trip changed stored fields/IDs/hashes.");
            }

            AnimolTerrainSavedMap empty = new AnimolTerrainSavedMap { themeId = "T01" };
            string originalEmpty = JsonUtility.ToJson(empty);
            Require(Engine.TryPlace(catalog, empty, "T01_D_Source", new Vector2Int(-1, -2),
                out AnimolTerrainSavedMap bridge, out error), error);
            Require(Resolve(catalog, bridge).solids.Count == 52, "T01_D must expand to 52 solid cells.");
            Require(bridge.revision == empty.revision + 1 && JsonUtility.ToJson(empty) == originalEmpty,
                "Place must advance one revision and preserve its input.");
            string originalBridge = JsonUtility.ToJson(bridge);
            Vector2Int archVoid = new Vector2Int(3, -2); // Local (4,0), origin (-1,-2).
            Require(Engine.TrySetBaseCell(catalog, bridge, archVoid, true, "T01_A",
                out AnimolTerrainSavedMap painted, out error), error);
            Require(Resolve(catalog, painted).solids.Count == 53 && painted.baseCells.Count == 1,
                "Arch void brush must add exactly one ordinary cell.");
            Require(painted.revision == bridge.revision + 1 && JsonUtility.ToJson(bridge) == originalBridge,
                "Void brush mutated its input or advanced revision incorrectly.");
            string originalPainted = JsonUtility.ToJson(painted);
            Require(!Engine.TrySetBaseCell(catalog, painted, new Vector2Int(-1, -2), false, "T01_A",
                out AnimolTerrainSavedMap failed, out error) && failed == null && !String.IsNullOrEmpty(error),
                "Partial erase of a structure # cell must fail with a null candidate.");
            Require(JsonUtility.ToJson(painted) == originalPainted, "Failed brush mutated map/revision.");
            Require(Engine.TryDelete(catalog, painted, painted.placements[0].instanceId,
                out AnimolTerrainSavedMap deleted, out error), error);
            AnimolTerrainResolveResult afterDelete = Resolve(catalog, deleted);
            Require(afterDelete.solids.Count == 1 && afterDelete.solids.ContainsKey(archVoid) &&
                String.IsNullOrEmpty(afterDelete.solids[archVoid].ownerId) && deleted.placements.Count == 0,
                "Whole-owner delete must preserve the ordinary arch cell only.");
            Require(deleted.revision == painted.revision + 1 && JsonUtility.ToJson(painted) == originalPainted,
                "Delete must be one immutable transaction.");

            AnimolTerrainSavedMap ground = new AnimolTerrainSavedMap { themeId = "T01" };
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 6; x++)
                    ground.baseCells.Add(new AnimolTerrainBaseCell { x = x, y = y, themeId = "T01", styleId = "T01_A" });
            Require(Engine.TryPlace(catalog, ground, "T01_A_Fill_Source", new Vector2Int(2, 2),
                out AnimolTerrainSavedMap decorated, out error), error);
            AnimolTerrainResolveResult filled = Resolve(catalog, decorated);
            Require(filled.solids.Count == 36 && filled.overlays.Count == 1 && filled.placements.Count == 0,
                "Interior overlay must add zero occupancy.");
            string originalDecorated = JsonUtility.ToJson(decorated);
            Require(!Engine.TryMove(catalog, decorated, decorated.placements[0].instanceId, new Vector2Int(-10, -10),
                out failed, out error) && failed == null, "Moving an overlay off support must reject, not prune the request.");
            Require(JsonUtility.ToJson(decorated) == originalDecorated, "Failed overlay move mutated its input.");
            Require(Engine.TrySetBaseCell(catalog, decorated, new Vector2Int(1, 1), false, "T01_A",
                out AnimolTerrainSavedMap pruned, out error), error);
            AnimolTerrainResolveResult afterPrune = Resolve(catalog, pruned);
            Require(afterPrune.solids.Count == 35 && afterPrune.overlays.Count == 0 && pruned.placements.Count == 0,
                "Removing a diagonal neighbor must prune the unsupported overlay and keep 35 ground cells.");
            Require(pruned.revision == decorated.revision + 1 && JsonUtility.ToJson(decorated) == originalDecorated,
                "Ground erase and overlay cascade must share one immutable transaction.");
            Require(!Engine.TryPlace(catalog, empty, "T01_A_Fill_Source", Vector2Int.zero,
                out failed, out error) && failed == null, "An unsupported new overlay must fail.");

            AnimolTerrainSavedMap bad = Engine.CloneMap(bridge);
            bad.placements[0].catalogVersion++;
            Invalid(catalog, bad, "catalog version mismatch");
            bad = Engine.CloneMap(bridge); bad.placements[0].maskHash = new string('0', 64);
            Invalid(catalog, bad, "mask hash mismatch");
            bad = Engine.CloneMap(bridge); bad.placements.Add(Engine.CloneMap(bridge).placements[0]);
            Invalid(catalog, bad, "duplicate instance ID");
            bad = Engine.CloneMap(bridge); bad.placements[0].orientation = "FlipX";
            Invalid(catalog, bad, "unsupported orientation");
            bad = Engine.CloneMap(bridge); bad.placements[0].catalogId = "Missing_Catalog_Entry";
            Invalid(catalog, bad, "unknown catalog ID");
            bad = Engine.CloneMap(bridge); bad.placements[0].themeId = "T02";
            Invalid(catalog, bad, "theme mismatch");
            Require(JsonUtility.ToJson(bridge) == originalBridge, "Bad-map clone checks mutated their source.");

            Require(Engine.FloorDiv(-1, 16) == -1 && Engine.FloorDiv(-16, 16) == -1 &&
                Engine.FloorDiv(-17, 16) == -2 && Engine.FloorDiv(16, 16) == 1 &&
                Engine.FloorDiv(Int32.MinValue, 16) == -134217728, "Negative chunk division differs from floor.");
            HashSet<Vector2Int> chunks = new HashSet<Vector2Int>(Engine.GetCoveredChunks(
                Resolve(catalog, bridge).entriesById["T01_D_Source"], new Vector2Int(-17, -1), 16));
            Require(chunks.SetEquals(new[] { new Vector2Int(-2, -1), new Vector2Int(-1, -1),
                new Vector2Int(-2, 0), new Vector2Int(-1, 0) }), "All four footprint chunks must be indexed.");
            Debug.Log("ANIMOL saved-map contract checks PASS: 5 real JSON round trips and pure engine transactions. " +
                "Physics, Shader rendering and Play Mode were not checked.");
        }

        private static string Load(string path)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + path);
            Require(asset != null, "Missing check input: " + Root + path);
            return asset.text;
        }

        private static AnimolTerrainResolveResult Resolve(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap map)
        {
            Require(Engine.Resolve(catalog, map, out AnimolTerrainResolveResult result, out string error), error);
            return result;
        }

        private static void SameResolved(AnimolTerrainResolveResult a, AnimolTerrainResolveResult b)
        {
            Require(a.solids.Count == b.solids.Count && a.placements.Count == b.placements.Count &&
                a.overlays.Count == b.overlays.Count, "Round trip changed occupancy/placement counts.");
            foreach (KeyValuePair<Vector2Int, AnimolTerrainCellResult> pair in a.solids)
            {
                Require(b.solids.TryGetValue(pair.Key, out AnimolTerrainCellResult cell) &&
                    pair.Value.ownerId == cell.ownerId && pair.Value.themeId == cell.themeId &&
                    pair.Value.styleId == cell.styleId && pair.Value.cell == cell.cell,
                    "Round trip changed owned cell " + pair.Key);
            }
        }

        private static void Invalid(AnimolTerrainCatalogData catalog, AnimolTerrainSavedMap map, string label)
        {
            Require(!Engine.Resolve(catalog, map, out AnimolTerrainResolveResult result, out string error) &&
                result == null && !String.IsNullOrEmpty(error), "Must fail closed: " + label);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("ANIMOL contract check: " + message);
        }
    }
}
