using UnityEngine;

namespace ANIMOL.Presentation
{
    // Presentation only. The same SpriteRenderer hosts every pose; puzzle rules own movement and victory.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StarwayCharacterVisual : MonoBehaviour
    {
        public const float PuzzleVisualScale = .85f;
        public StarwayCharacterCatalog catalog;
        public string characterId;
        public string costumeId;
        public SpriteRenderer visual;
        public string Direction { get; private set; } = "front";
        public string State { get; private set; } = "idle";
        public Sprite Illustration => catalog && catalog.Find(characterId) != null
            ? catalog.Find(characterId).IllustrationFor(costumeId) : null;
        public AnimationClip CurrentClip => motion?.clip;
        public int CurrentFrame { get; private set; }
        StarwayCharacterMotion motion;
        float elapsed;

        public static StarwayCharacterVisual Create(Transform parent, Vector2 position, int sortingOrder,
            StarwayCharacterCatalog source, string id, string costume = null)
        {
            var root = new GameObject("Player");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            var child = new GameObject("Visual");
            child.transform.SetParent(root.transform, false);
            child.transform.localScale = Vector3.one * PuzzleVisualScale;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            var player = child.AddComponent<StarwayCharacterVisual>();
            player.Initialize(source, id, costume);
            return player;
        }

        public bool Initialize(StarwayCharacterCatalog source, string id, string costume = null)
        {
            var definition = source ? source.Find(id) : null;
            if (definition == null) return false;
            var selected = string.IsNullOrEmpty(costume) ? definition.defaultCostumeId : costume;
            if (!definition.HasCostume(selected)) return false;
            catalog = source;
            characterId = id;
            costumeId = selected;
            visual = GetComponent<SpriteRenderer>();
            return PlayPose("front", "idle");
        }

        public bool SetCostume(string costume)
        {
            var definition = catalog ? catalog.Find(characterId) : null;
            if (definition == null || string.IsNullOrEmpty(costume) || !definition.HasCostume(costume)) return false;
            var previous = costumeId;
            costumeId = costume;
            if (PlayPose(Direction, State)) return true;
            costumeId = previous;
            return false;
        }

        public bool PlayPose(string direction, string state)
        {
            var definition = catalog ? catalog.Find(characterId) : null;
            if (definition == null) return false;
            var left = direction == "left";
            var nextDirection = left ? "side" : direction;
            var next = definition.Motion(nextDirection, state, costumeId);
            if (next == null || next.frames == null || next.frames.Length != 6 || next.fps <= 0) return false;
            visual.flipX = left;
            Direction = direction;
            State = state;
            motion = next;
            elapsed = 0;
            CurrentFrame = 0;
            visual.sprite = motion.frames[0];
            return visual.sprite;
        }

        public void SetMotion(Vector2Int direction, string state)
        {
            var nextDirection = direction.x < 0 ? "left" : direction.x > 0 ? "side" :
                direction.y > 0 ? "back" : direction.y < 0 ? "front" : Direction;
            if (State != state || Direction != nextDirection) PlayPose(nextDirection, state);
        }

        public void SetIdle() => PlayPose(Direction, "idle");

        public void Advance(float seconds)
        {
            if (motion == null || !visual || seconds <= 0) return;
            elapsed += seconds;
            var frame = Mathf.FloorToInt(elapsed * motion.fps);
            if (frame >= motion.frames.Length)
            {
                if (!motion.loop) { SetIdle(); return; }
                frame %= motion.frames.Length;
            }
            CurrentFrame = frame;
            visual.sprite = motion.frames[frame];
        }

        void Update() => Advance(Time.deltaTime);
    }
}
