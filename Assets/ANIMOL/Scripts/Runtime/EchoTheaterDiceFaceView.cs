using UnityEngine;
using ANIMOL.Presentation;
using ANIMOL.Art.EchoTheaterV03;

namespace ANIMOL.Puzzle
{
    // Authored V3 maps use both the V03 face convention and its north/south mirror.
    // Resolve the convention from the full committed orientation for art lookup only.
    public sealed class EchoTheaterDiceFaceView : MonoBehaviour
    {
        EchoTheaterV03DiceVisualMap map;
        EchoTheaterV03VisualCatalog catalog;
        string manifestHash;
        Sprite[] rollFrames;
        SpriteRenderer image;
        Sprite settled;
        public Sprite CommittedSprite => settled;
        float rollTime;
        internal static bool TryArtState(int[] faces, out EchoTheaterV03DiceOrientation.State state,
            out bool mirrored, out string error)
        {
            state = default;
            mirrored = false;
            if (faces == null || faces.Length != 6) { error = "Six dice faces required."; return false; }
            if (EchoTheaterV03DiceOrientation.TryCreate(faces[0], faces[1], faces[2], faces[3],
                faces[4], faces[5], out state, out error)) return true;
            if (!EchoTheaterV03DiceOrientation.TryCreate(faces[0], faces[1], faces[3], faces[2],
                faces[4], faces[5], out state, out error)) return false;
            mirrored = true;
            return true;
        }

        static EchoTheaterV03DiceOrientation.Heading ArtHeading(Vector2Int direction, bool mirrored) =>
            direction == Vector2Int.up ? mirrored ? EchoTheaterV03DiceOrientation.Heading.South :
                EchoTheaterV03DiceOrientation.Heading.North :
            direction == Vector2Int.down ? mirrored ? EchoTheaterV03DiceOrientation.Heading.North :
                EchoTheaterV03DiceOrientation.Heading.South :
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
            bool mirrored;
            string key, error;
            EchoTheaterV03VisualCatalog.Clip clip;
            if (!TryArtState(faces, out state, out mirrored, out error) ||
                !map.TryGetStableClip(state, out key, out error) ||
                !catalog.TryGetClip(key, manifestHash, out clip, out error))
            { Debug.LogError("V3 dice stable art: " + error, this); return; }
            settled = clip.GetFrame(0).Sprite;
            if (rollTime <= 0f) image.sprite = settled;
        }
        public void ResetToFaces(int[] faces)
        {
            rollTime = 0f;
            rollFrames = null;
            Present(faces);
        }
        public void BeginRoll(Vector2Int heading, int[] beforeFaces, int[] afterFaces)
        {
            EchoTheaterV03DiceOrientation.State before, after;
            bool beforeMirrored, afterMirrored;
            string key, error;
            EchoTheaterV03VisualCatalog.Clip clip;
            if (!TryArtState(beforeFaces, out before, out beforeMirrored, out error) ||
                !TryArtState(afterFaces, out after, out afterMirrored, out error))
            { Debug.LogError("V3 dice roll art: " + error, this); return; }
            if (beforeMirrored != afterMirrored)
            { Debug.LogError("V3 dice roll art: face convention changed during a roll.", this); return; }
            if (!map.TryGetCommittedRollClip(before, after, ArtHeading(heading, beforeMirrored), true, false,
                    out key, out error) ||
                !catalog.TryGetClip(key, manifestHash, out clip, out error))
            { Debug.LogError("V3 dice roll art: " + error, this); return; }
            // Cache the real settled face before a matching die is consumed.
            Present(afterFaces);
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
