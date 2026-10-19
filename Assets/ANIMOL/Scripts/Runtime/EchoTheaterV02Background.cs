using System;
using System.Collections.Generic;
using Animol.EchoTheater.V02;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    [Serializable]
    public sealed class EchoTheaterV02BackgroundLayout
    {
        [Serializable]
        public sealed class Sky { public string file; public float positionZ, coverOverscan; public int sortingOrder; }
        [Serializable]
        public sealed class Item
        {
            public string id, file;
            public float[] positionWorld, localScale, driftAmplitude, driftPeriodSeconds, guardWorld, color;
            public int sortingOrder;
            public float parallaxMultiplier, phaseDegrees, twinkleAmount, twinklePeriodSeconds;
            public bool wrapToView;
        }
        public Sky skyBase;
        public Item[] layers;
    }

    // Independent background SpriteRenderers, owned by the existing puzzle presentation.
    public sealed class EchoTheaterV02Background : MonoBehaviour
    {
        EchoTheaterV02BackgroundRig rig;

        public void Configure(EchoTheaterV02Art art, Camera camera)
        {
            if (!art || !art.backgroundLayout || !camera) return;
            var layout = JsonUtility.FromJson<EchoTheaterV02BackgroundLayout>(art.backgroundLayout.text);
            if (layout?.skyBase == null || layout.layers == null || layout.layers.Length != 130)
                throw new InvalidOperationException("Echo Theater V02 background layout requires 130 independent layers and one sky.");
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 128f);
            var root = new GameObject("Independent theater sky and depth layers").transform;
            root.SetParent(transform, false);
            var skySprite = art.Find(layout.skyBase.file);
            if (!skySprite) throw new InvalidOperationException("Missing theater sky sprite");
            var sky = Renderer(root, "Sky base", skySprite,
                new Vector3(camera.transform.position.x, camera.transform.position.y, layout.skyBase.positionZ),
                Vector3.one, layout.skyBase.sortingOrder, Color.white);
            sky.gameObject.AddComponent<EchoTheaterV02CameraCover>().Configure(camera, layout.skyBase.coverOverscan);

            var layers = new List<EchoTheaterV02BackgroundRig.Layer>(layout.layers.Length);
            foreach (var item in layout.layers)
            {
                var sprite = art.Find(item.file);
                if (!sprite) throw new InvalidOperationException("Missing theater background sprite: " + item.file);
                var position = new Vector3(camera.transform.position.x + item.positionWorld[0],
                    camera.transform.position.y + item.positionWorld[1], item.positionWorld[2]);
                var renderer = Renderer(root, item.id, sprite, position,
                    new Vector3(item.localScale[0], item.localScale[1], item.localScale[2]),
                    item.sortingOrder, new Color(item.color[0], item.color[1], item.color[2], item.color[3]));
                layers.Add(new EchoTheaterV02BackgroundRig.Layer
                {
                    renderer = renderer,
                    sortingOrder = item.sortingOrder,
                    parallaxMultiplier = item.parallaxMultiplier,
                    driftAmplitude = new Vector2(item.driftAmplitude[0], item.driftAmplitude[1]),
                    driftPeriodSeconds = new Vector2(item.driftPeriodSeconds[0], item.driftPeriodSeconds[1]),
                    phaseDegrees = item.phaseDegrees,
                    twinkleAmount = item.twinkleAmount,
                    twinklePeriodSeconds = item.twinklePeriodSeconds,
                    wrapToView = item.wrapToView,
                    guardWorld = new Vector2(item.guardWorld[0], item.guardWorld[1])
                });
            }
            rig = root.gameObject.AddComponent<EchoTheaterV02BackgroundRig>();
            rig.Configure(camera, 0f, "Background", layers.ToArray());
            rig.SetReducedMotion(StarwayRanchMotionSettings.ReducedMotion);
        }

        static SpriteRenderer Renderer(Transform root, string name, Sprite sprite,
            Vector3 position, Vector3 scale, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = order;
            renderer.color = color;
            return renderer;
        }

        void Update()
        {
            if (rig && rig.ReducedMotion != StarwayRanchMotionSettings.ReducedMotion)
                rig.SetReducedMotion(StarwayRanchMotionSettings.ReducedMotion);
        }
        public void Rebase() { if (rig) rig.Rebase(); }
    }
}
