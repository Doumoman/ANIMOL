using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.DevicePatch01
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class AnimolWindRouteRenderer : MonoBehaviour
    {
        [SerializeField] private Material windMaterial;
        [SerializeField, Range(0.2f, 0.8f)] private float corridorWidthCells = 0.56f;
        [SerializeField, Range(4, 24)] private int samplesPerQuarterTurn = 12;
        [SerializeField, Min(0.01f)] private float flowCellsPerSecond = 2.4f;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 5;

        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock properties;
        private float pathLength;
        private float elapsed;
        private float duration;
        private bool active;
        private bool externalProgress;
        private bool hasActionSequence;
        private long lastActionSequence;

        private static readonly int PathLengthId = Shader.PropertyToID("_PathLength");
        private static readonly int FlowOffsetId = Shader.PropertyToID("_FlowOffset");
        private static readonly int ActiveId = Shader.PropertyToID("_Active");

        public bool HasVisibleRange { get { return mesh != null && mesh.vertexCount > 0; } }
        public bool IsActive { get { return active; } }

        private struct Sample
        {
            public Vector2 point;
            public Vector2 tangent;
            public float distance;
            public Sample(Vector2 p, Vector2 t, float d) { point = p; tangent = t; distance = d; }
        }

        private void EnsureObjects()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (properties == null) properties = new MaterialPropertyBlock();
            if (mesh == null)
            {
                mesh = new Mesh { name = "ANIMOL Wind Route (owned runtime mesh)" };
                mesh.MarkDynamic();
                GetComponent<MeshFilter>().sharedMesh = mesh;
            }
            meshRenderer.sharedMaterial = windMaterial;
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }

        public void ConfigureMaterial(Material material, string layer, int order)
        {
            if (material == null) throw new ArgumentNullException("material");
            windMaterial = material;
            sortingLayerName = layer;
            sortingOrder = order;
            EnsureObjects();
            ApplyProperties();
        }

        // gridOrigin is the lower-left cell (0,0) corner in gridSpace local XY.
        // One logical cell has side cellSize in that space. gridSpace may be rotated,
        // uniformly scaled, or parented; mesh vertices are transformed into this object.
        // Positive Y is North. Native art outlet must align to emitter cell center.
        public void SetRoute(WindRoute route, Transform gridSpace, Vector2 gridOrigin, float cellSize)
        {
            if (route == null) throw new ArgumentNullException("route");
            if (gridSpace == null) throw new ArgumentNullException("gridSpace");
            if (!(cellSize > 0f) || float.IsInfinity(cellSize) || float.IsNaN(cellSize))
                throw new ArgumentOutOfRangeException("cellSize");
            EnsureObjects();
            if (windMaterial == null)
                throw new InvalidOperationException("Assign the patch wind material. No legacy visual fallback is used.");
            mesh.Clear();
            active = false; elapsed = 0f; duration = 0f; externalProgress = false;
            pathLength = 0f;
            if (route.cells.Count == 0) { meshRenderer.enabled = false; ApplyProperties(); return; }

            var samples = BuildSamples(route, Mathf.Clamp(samplesPerQuarterTurn, 4, 24));
            pathLength = samples[samples.Count - 1].distance;
            var vertices = new List<Vector3>(samples.Count * 2);
            var uv = new List<Vector2>(samples.Count * 2);
            var triangles = new List<int>((samples.Count - 1) * 6);
            float halfWidth = Mathf.Clamp(corridorWidthCells, 0.2f, 0.8f) * 0.5f;
            for (int i = 0; i < samples.Count; i++)
            {
                Sample s = samples[i];
                Vector2 normal = new Vector2(-s.tangent.y, s.tangent.x);
                Vector2 a = gridOrigin + (s.point - normal * halfWidth) * cellSize;
                Vector2 b = gridOrigin + (s.point + normal * halfWidth) * cellSize;
                vertices.Add(transform.InverseTransformPoint(gridSpace.TransformPoint(new Vector3(a.x, a.y, 0f))));
                vertices.Add(transform.InverseTransformPoint(gridSpace.TransformPoint(new Vector3(b.x, b.y, 0f))));
                uv.Add(new Vector2(s.distance, 0f)); uv.Add(new Vector2(s.distance, 1f));
                if (i == 0) continue;
                int p = (i - 1) * 2;
                triangles.Add(p); triangles.Add(p + 2); triangles.Add(p + 1);
                triangles.Add(p + 1); triangles.Add(p + 2); triangles.Add(p + 3);
            }
            mesh.indexFormat = vertices.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            meshRenderer.enabled = true;
            ApplyProperties();
        }

        private static Vector2 Direction(WindHeading h)
        {
            switch (h)
            {
                case WindHeading.North: return Vector2.up;
                case WindHeading.East: return Vector2.right;
                case WindHeading.South: return Vector2.down;
                case WindHeading.West: return Vector2.left;
                default: throw new ArgumentOutOfRangeException("h");
            }
        }

        private static Vector2 Center(WindGridCell c) { return new Vector2(c.x + 0.5f, c.y + 0.5f); }

        private static List<Sample> BuildSamples(WindRoute route, int turnSamples)
        {
            var result = new List<Sample>();
            Vector2 initial = Direction(route.emitterHeading);
            Vector2 emitter = Center(route.emitterCell);
            result.Add(new Sample(emitter, initial, 0f));
            result.Add(new Sample(emitter + initial * 0.5f, initial, 0.5f));
            float distance = 0.5f;
            foreach (WindRouteCell item in route.cells)
            {
                Vector2 center = Center(item.cell);
                Vector2 incoming = Direction(item.incoming);
                Vector2 outgoing = Direction(item.outgoing);
                if (!item.isCorner)
                {
                    distance += 1f;
                    result.Add(new Sample(center + outgoing * 0.5f, outgoing, distance));
                    continue;
                }
                Vector2 arcCenter = center - incoming * 0.5f + outgoing * 0.5f;
                Vector2 startRadial = -outgoing * 0.5f;
                float cross = incoming.x * outgoing.y - incoming.y * outgoing.x;
                float sign = cross > 0f ? 1f : -1f;
                for (int j = 1; j <= turnSamples; j++)
                {
                    float angle = sign * Mathf.PI * 0.5f * j / turnSamples;
                    float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                    Vector2 radial = new Vector2(startRadial.x * cos - startRadial.y * sin,
                        startRadial.x * sin + startRadial.y * cos);
                    Vector2 tangent = new Vector2(-radial.y, radial.x).normalized * sign;
                    result.Add(new Sample(arcCenter + radial, tangent,
                        distance + Mathf.PI * 0.25f * j / turnSamples));
                }
                distance += Mathf.PI * 0.25f;
            }
            return result;
        }

        // Explicit gate: call only after the host has accepted/committed the valid action.
        // A repeated sequence or rejected input cannot restart wind feedback.
        public bool PlayCommittedAction(bool actionCommitted, long actionSequence, float visualDuration)
        {
            if (!actionCommitted || !HasVisibleRange) return false;
            if (hasActionSequence && actionSequence <= lastActionSequence) return false;
            if (!(visualDuration > 0f) || float.IsNaN(visualDuration) || float.IsInfinity(visualDuration))
                throw new ArgumentOutOfRangeException("visualDuration");
            hasActionSequence = true; lastActionSequence = actionSequence;
            duration = visualDuration; elapsed = 0f; active = true; externalProgress = false;
            ApplyProperties();
            return true;
        }

        // Optional authoritative tween drive; calling this switches off the local clock.
        public void SetActionProgress01(float progress)
        {
            if (float.IsNaN(progress) || float.IsInfinity(progress))
                throw new ArgumentOutOfRangeException("progress");
            if (!active) return;
            externalProgress = true;
            elapsed = Mathf.Clamp01(progress) * duration;
            if (progress >= 1f) active = false;
            ApplyProperties();
        }

        public void CompleteAction(bool retainRange = true)
        {
            active = false; externalProgress = false;
            if (!retainRange) HideRoute(); else ApplyProperties();
        }

        public void HideRoute()
        {
            EnsureObjects();
            active = false; externalProgress = false; elapsed = 0f; duration = 0f; pathLength = 0f;
            mesh.Clear(); meshRenderer.enabled = false; ApplyProperties();
        }

        // Explicit pooling/session reset. Hiding/rebuilding a route alone does not reset
        // the sequence guard and therefore cannot replay the same committed action.
        public void ResetActionSequence() { hasActionSequence = false; lastActionSequence = 0; }

        private void Update()
        {
            if (!active || externalProgress) return;
            elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            if (elapsed >= duration) active = false;
            ApplyProperties();
        }

        private void ApplyProperties()
        {
            if (meshRenderer == null || properties == null) return;
            properties.SetFloat(PathLengthId, pathLength);
            properties.SetFloat(FlowOffsetId, elapsed * Mathf.Max(0.01f, flowCellsPerSecond));
            properties.SetFloat(ActiveId, active ? 1f : 0f);
            meshRenderer.SetPropertyBlock(properties);
        }

        private void OnDestroy()
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }
    }
}
