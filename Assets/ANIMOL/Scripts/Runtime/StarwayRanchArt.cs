using System;
using System.Collections.Generic;
using Animol.StarwayRanch.V10;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Existing lobby toggle carries its current value across scene changes in this session.
    public static class StarwayRanchMotionSettings
    {
        public static bool ReducedMotion;
    }

    [Serializable] public sealed class RanchSpriteEntry
    {
        public string file;
        public Sprite sprite;
    }

    [Serializable] public sealed class RanchBackgroundLayer
    {
        public string id, file;
        public float[] positionWorld, localScale, driftAmplitude, driftPeriodSeconds, guardWorld, color;
        public int sortingOrder;
        public float phaseDegrees, twinkleAmount, twinklePeriodSeconds, parallaxMultiplier;
        public bool wrapToView;
    }

    [Serializable] public sealed class RanchBackgroundLayout
    {
        public RanchBackgroundLayer[] layers;
    }

    [CreateAssetMenu(menuName = "ANIMOL/Starway Ranch Art")]
    public sealed class StarwayRanchArt : ScriptableObject
    {
        public TextAsset backgroundLayout;
        public RanchSpriteEntry[] sprites = Array.Empty<RanchSpriteEntry>();
        Dictionary<string, Sprite> index;

        public Sprite Find(string file)
        {
            if (index == null)
            {
                index = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                foreach (var entry in sprites) if (entry != null && entry.sprite) index[entry.file] = entry.sprite;
            }
            return file != null && index.TryGetValue(file, out var sprite) ? sprite : null;
        }

        public RanchBackgroundLayout Layout => backgroundLayout
            ? JsonUtility.FromJson<RanchBackgroundLayout>(backgroundLayout.text) : null;

        void OnValidate() => index = null;
    }

    // A scene can bake these independent renderers; dynamic puzzle previews use the same factory.
    public static class StarwayRanchBackground
    {
        public static StarwayRanchV10BackgroundRig Create(Transform parent, Camera camera, StarwayRanchArt art)
        {
            if (!camera || !art || art.Layout == null) return null;
            var root = new GameObject("Starway Ranch background V10").transform;
            root.SetParent(parent, false);
            var skyObject = new GameObject("sky_base_native");
            skyObject.transform.SetParent(root, false);
            skyObject.transform.localPosition = new Vector3(0, 0, 95);
            var sky = skyObject.AddComponent<SpriteRenderer>();
            sky.sprite = art.Find("backgrounds/sky_base_native.png");
            sky.sortingLayerName = "Background";
            sky.sortingOrder = -100;
            skyObject.AddComponent<StarwayRanchV10CameraCover>().Configure(camera, .08f);
            var layout = art.Layout;
            var layers = new StarwayRanchV10BackgroundRig.Layer[layout.layers.Length];
            for (var i = 0; i < layout.layers.Length; i++)
            {
                var row = layout.layers[i];
                var go = new GameObject(row.id);
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(camera.transform.position.x + row.positionWorld[0],
                    camera.transform.position.y + row.positionWorld[1], row.positionWorld[2]);
                go.transform.localScale = new Vector3(row.localScale[0], row.localScale[1], row.localScale[2]);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = art.Find(row.file);
                renderer.color = new Color(row.color[0], row.color[1], row.color[2], row.color[3]);
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = row.sortingOrder;
                layers[i] = new StarwayRanchV10BackgroundRig.Layer {
                    renderer = renderer,
                    sortingOrder = row.sortingOrder,
                    parallaxMultiplier = row.parallaxMultiplier,
                    driftAmplitude = new Vector2(row.driftAmplitude[0], row.driftAmplitude[1]),
                    driftPeriodSeconds = new Vector2(row.driftPeriodSeconds[0], row.driftPeriodSeconds[1]),
                    phaseDegrees = row.phaseDegrees,
                    twinkleAmount = row.twinkleAmount,
                    twinklePeriodSeconds = row.twinklePeriodSeconds,
                    wrapToView = row.wrapToView,
                    guardWorld = new Vector2(row.guardWorld[0], row.guardWorld[1])
                };
            }
            var rig = root.gameObject.AddComponent<StarwayRanchV10BackgroundRig>();
            rig.Configure(camera, 0, "Background", layers);
            rig.SetReducedMotion(StarwayRanchMotionSettings.ReducedMotion);
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 121f);
            return rig;
        }
    }
}
