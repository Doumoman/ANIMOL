using UnityEngine;
using ANIMOL.Presentation;
using ANIMOL.Art.EchoTheaterV03;

namespace ANIMOL.Puzzle
{
    // V3 mirrors the north/south labels of V03's 24-state sheet. Convert only for
    // art lookup; the authoritative faces remain in PuzzleDeviceSession.
    public sealed class EchoTheaterDiceFaceView : MonoBehaviour
    {
        EchoTheaterV03DiceVisualMap map;
        EchoTheaterV03VisualCatalog catalog;
        string manifestHash;
        Sprite[] rollFrames;
        SpriteRenderer image;
        Sprite settled;
        float rollTime;
        internal static bool TryArtState(int[] faces, out EchoTheaterV03DiceOrientation.State state, out string error)
        {
            state = default;
            if (faces == null || faces.Length != 6) { error = "Six dice faces required."; return false; }
            return EchoTheaterV03DiceOrientation.TryCreate(faces[0], faces[1], faces[3], faces[2],
                faces[4], faces[5], out state, out error);
        }

        static EchoTheaterV03DiceOrientation.Heading ArtHeading(Vector2Int direction) =>
            direction == Vector2Int.up ? EchoTheaterV03DiceOrientation.Heading.South :
            direction == Vector2Int.down ? EchoTheaterV03DiceOrientation.Heading.North :
            direction == Vector2Int.right ? EchoTheaterV03DiceOrientation.Heading.East :
            direction == Vector2Int.left ? EchoTheaterV03DiceOrientation.Heading.West :
                EchoTheaterV03DiceOrientation.Heading.None;

        public bool Configure(EchoTheaterV03DiceVisualMap map, EchoTheaterV03VisualCatalog catalog,
            string manifestHash, out string error)
        {
            error = null;
            var view = GetComponent<DeviceVisualView>();
            image = view && view.visual ? view.visual : GetComponentInChildren<SpriteRenderer>();
            if (!image || map == null || !catalog)
            { error = "Dice prefab or V03 orientation art is missing."; return false; }
            this.map = map; this.catalog = catalog; this.manifestHash = manifestHash;
            if (view) view.enabled = false;
            var animated = GetComponent<EchoTheaterV03AnimatedVisual>();
            if (animated) animated.enabled = false;
            image.sortingOrder = 30;
            return true;
        }
        public void Present(int[] faces)
        {
            EchoTheaterV03DiceOrientation.State state;
            string key, error;
            EchoTheaterV03VisualCatalog.Clip clip;
            if (!TryArtState(faces, out state, out error) ||
                !map.TryGetStableClip(state, out key, out error) ||
                !catalog.TryGetClip(key, manifestHash, out clip, out error))
            { Debug.LogError("V3 dice stable art: " + error, this); return; }
            settled = clip.GetFrame(0).Sprite;
            if (rollTime <= 0f) image.sprite = settled;
        }
        public void BeginRoll(Vector2Int heading, int[] beforeFaces, int[] afterFaces)
        {
            EchoTheaterV03DiceOrientation.State before, after;
            string key, error;
            EchoTheaterV03VisualCatalog.Clip clip;
            if (!TryArtState(beforeFaces, out before, out error) || !TryArtState(afterFaces, out after, out error) ||
                !map.TryGetCommittedRollClip(before, after, ArtHeading(heading), true, false, out key, out error) ||
                !catalog.TryGetClip(key, manifestHash, out clip, out error))
            { Debug.LogError("V3 dice roll art: " + error, this); return; }
            rollFrames = new Sprite[4];
            for (var frame = 0; frame < 4; frame++) rollFrames[frame] = clip.GetFrame(frame + 1).Sprite;
            rollTime = .16f;
            image.sprite = rollFrames[0];
        }
        void Update()
        {
            if (rollTime <= 0f) return;
            rollTime = Mathf.Max(0f, rollTime - Time.unscaledDeltaTime);
            if (rollTime <= 0f) { image.sprite = settled; return; }
            var frame = Mathf.Clamp(Mathf.FloorToInt((1f - rollTime / .16f) * 4f), 0, 3);
            image.sprite = rollFrames[frame];
        }
    }
}
