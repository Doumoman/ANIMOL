using System;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.StarwayMaps.Editor
{
    // No NUnit/Test Framework dependency. These execute IN Unity, not in the delivered zip's Python QA.
    public static class StarwaySourceChecks
    {
        [MenuItem("ANIMOL/Starway 20/6 Run Source and Camera Checks")]
        public static void RunSourceAndCameraChecks()
        {
            StarwayMapInput[] inputs = StarwayPackCommands.LoadAndValidateInputs();
            foreach (StarwayMapInput input in inputs)
            {
                string json = JsonUtility.ToJson(input);
                StarwayMapInput roundtrip = JsonUtility.FromJson<StarwayMapInput>(json);
                Check(JsonUtility.ToJson(roundtrip) == json, input.map.id + " JsonUtility roundtrip");
                StarwayInputValidator.Validate(roundtrip, input.map.index);
                string path = StarwayPackCommands.DataRoot + "/UnityReplay/" + input.map.id + ".json";
                UnityEngine.TextAsset text = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(path);
                Check(text != null, input.map.id + " replay TextAsset");
                StarwayReplayExpectations replay = JsonUtility.FromJson<StarwayReplayExpectations>(text.text);
                Check(replay != null && replay.schema == "animol.unity-replay.v1" && replay.id == input.map.id &&
                    replay.verificationScope == "REPORT_MODEL_ONLY", input.map.id + " replay identity");
                Check(replay.actions == input.referenceActions && replay.inputCount == input.referenceSuccessfulInputs &&
                    replay.steps.Length == replay.inputCount, input.map.id + " replay length");
                for (int i = 0; i < replay.steps.Length; i++)
                    Check(replay.steps[i].turn == i + 1 && replay.steps[i].action == replay.actions[i].ToString(), input.map.id + " replay sequence");
                Check(replay.steps[replay.steps.Length - 1].playerAfter.Equals(input.map.exit) &&
                    replay.steps[replay.steps.Length - 1].bubbleMask == 7, input.map.id + " final exit/three bubbles");
            }
            Vector2 focus = StarwayCameraMath.FocusWorldSize(Vector2.one);
            Near(focus.x, 7, "focus width"); Near(focus.y, 11, "focus height");
            int[,] resolutions = { { 1080, 1920 }, { 1080, 2340 }, { 1200, 1920 }, { 1920, 1080 } };
            for (int i = 0; i < resolutions.GetLength(0); i++)
            {
                Rect screen = new Rect(0, 0, resolutions[i, 0], resolutions[i, 1]);
                Rect available = new Rect(0, 180, screen.width, screen.height - 300);
                Rect fitted = StarwayCameraMath.FitInsidePixels(available, 7f / 11f);
                Near(fitted.width / fitted.height, 7f / 11f, "focus pixel aspect");
                Check(fitted.xMin >= available.xMin - .01f && fitted.xMax <= available.xMax + .01f &&
                    fitted.yMin >= available.yMin - .01f && fitted.yMax <= available.yMax + .01f, "viewport inside HUD-free area");
                Rect n = StarwayCameraMath.NormalizePixels(fitted, resolutions[i, 0], resolutions[i, 1]);
                Check(n.xMin >= 0 && n.yMin >= 0 && n.xMax <= 1.001f && n.yMax <= 1.001f, "normalized viewport");
            }
            Vector2 big = StarwayCameraMath.ClampCenter(new Vector2(-100, 100), new Rect(-.5f, -.5f, 18, 22), focus);
            Near(big.x, 3, "left clamp half-cell convention"); Near(big.y, 16, "top clamp half-cell convention");
            Vector2 small = StarwayCameraMath.ClampCenter(new Vector2(100, -100), new Rect(10, 20, 4, 6), focus);
            Near(small.x, 12, "small map center x"); Near(small.y, 23, "small map center y");
            Rect region = StarwayCameraMath.CellBounds(new[] { new StarwayCell(2, 3), new StarwayCell(5, 8) }, new Vector2(10, -2), new Vector2(2, 3));
            Near(region.xMin, 13, "offset/scaled grid min x"); Near(region.xMax, 21, "offset/scaled grid max x");
            Near(region.yMin, 5.5f, "offset/scaled grid min y"); Near(region.yMax, 23.5f, "offset/scaled grid max y");
            bool rejected = false;
            try { StarwayCameraMath.Intersect(new Rect(0, 0, 1, 1), new Rect(2, 2, 1, 1)); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "empty gameplay area rejected");
            Debug.Log("[Starway20] Unity JsonUtility/source/camera math checks PASSED. Gameplay replay is not covered by these checks.");
        }

        [MenuItem("ANIMOL/Starway 20/7 Check Source Import Idempotence")]
        public static void CheckSourceImportIdempotence()
        {
            StarwayPackCommands.ImportOrUpdateSources();
            string[] paths = new string[21], guids = new string[21];
            for (int i = 0; i < 20; i++) paths[i] = StarwayPackCommands.GeneratedRoot + "/Maps/SW" + (i + 1).ToString("00") + ".asset";
            paths[20] = StarwayPackCommands.CatalogPath;
            for (int i = 0; i < paths.Length; i++) guids[i] = AssetDatabase.AssetPathToGUID(paths[i]);
            StarwayPackCommands.ImportOrUpdateSources();
            for (int i = 0; i < paths.Length; i++) Check(!string.IsNullOrEmpty(guids[i]) && guids[i] == AssetDatabase.AssetPathToGUID(paths[i]), "GUID stable: " + paths[i]);
            Check(AssetDatabase.FindAssets("t:StarwayMapSource", new[] { StarwayPackCommands.GeneratedRoot }).Length == 20, "exactly 20 staging map assets");
            StarwayPackCommands.ValidateSources();
            Debug.Log("[Starway20] Source import twice, count and GUID idempotence PASSED. Native adapter idempotence must be checked separately.");
        }
        private static void Near(float actual, float expected, string label) { Check(Mathf.Abs(actual - expected) < .001f, label + ": " + actual + " != " + expected); }
        private static void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("Starway source check failed: " + label); }
    }
}
