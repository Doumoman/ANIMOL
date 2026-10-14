using UnityEngine;

namespace ANIMOL.Puzzle
{
    public sealed class ClockDomeRoom : MonoBehaviour
    {
        public ClockDomeArt art;
        [Min(0)] public int columnRows = 4;
        public bool splitCap;
        Transform architecture;
        public void Assemble(int rows, bool split = false)
        {
            columnRows = Mathf.Max(0, rows); splitCap = split;
            if (!architecture) architecture = transform.Find("Fixed-width architecture");
            if (architecture) { architecture.gameObject.SetActive(false); if (Application.isPlaying) Destroy(architecture.gameObject); else DestroyImmediate(architecture.gameObject); }
            architecture = new GameObject("Fixed-width architecture").transform; architecture.SetParent(transform, false);
            if (split) for (var k = 0; k < 3; k++) Image("dome_cap_segment_" + new[] { "left", "center", "right" }[k] + "_320x400", new Vector2(k * 4, 0), -60);
            else Image("dome_cap_960x400", Vector2.zero, -60);
            for (var row = 0; row < columnRows; row++)
            { Image("column_left_repeat_160x80", new Vector2(0, -5 - row), -60); Image("column_right_repeat_160x80", new Vector2(10, -5 - row), -60); }
            Image("column_left_foot_160x80", new Vector2(0, -5 - columnRows), -60);
            Image("column_right_foot_160x80", new Vector2(10, -5 - columnRows), -60);
            Image("clock_240x240", TheaterTopology.PropPosition(new Vector2(360, 78), new Vector2(240, 240), new Vector2(.5f, .5f)), -55);
            foreach (var x in new[] { 24, 856 })
            {
                var image = Image("pendulum_80x240", TheaterTopology.PropPosition(new Vector2(x, 400), new Vector2(80, 240), new Vector2(.5f, 1)), -50);
                var animation = image.gameObject.AddComponent<ClockDomeAnimation>(); animation.sequence = art.pendulum; animation.visual = image;
            }
        }
        SpriteRenderer Image(string id, Vector2 p, int order)
        {
            var root = new GameObject(id); root.transform.SetParent(architecture, false); root.transform.localPosition = p;
            var image = root.AddComponent<SpriteRenderer>(); image.sprite = art.Part(id); image.sortingOrder = order; return image;
        }
    }
}
