using UnityEngine;
using Animol.Art.StarwayRetouch;
using Animol.Art.TheaterRetouch;
using Animol.Art.SanctumRetouch;
using Animol.Art.CoastRetouch;

namespace ANIMOL.Presentation
{
    // Presentation only. The same SpriteRenderer hosts every pose; puzzle rules own movement and victory.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StarwayCharacterVisual : MonoBehaviour
    {
        public const float PuzzleVisualScale = .85f;
        public const float RetouchVisualScale = 1f;
        public StarwayCharacterCatalog catalog;
        public string characterId;
        public string costumeId;
        public SpriteRenderer visual;
        public StarwayPushPresentation PushPresenter { get; private set; }
        public TheaterPushPresentation TheaterPushPresenter { get; private set; }
        public SanctumPushPresentation SanctumPushPresenter { get; private set; }
        public CoastPushPresentation CoastPushPresenter { get; private set; }
        public bool HasActivePushPresenter => PushPresenter && PushPresenter.isActiveAndEnabled ||
            TheaterPushPresenter && TheaterPushPresenter.isActiveAndEnabled ||
            SanctumPushPresenter && SanctumPushPresenter.isActiveAndEnabled ||
            CoastPushPresenter && CoastPushPresenter.isActiveAndEnabled;
        public bool PushPoseActive => PushPresenter && PushPresenter.isActiveAndEnabled && PushPresenter.PoseActive ||
            TheaterPushPresenter && TheaterPushPresenter.isActiveAndEnabled && TheaterPushPresenter.PoseActive ||
            SanctumPushPresenter && SanctumPushPresenter.isActiveAndEnabled && SanctumPushPresenter.PoseActive ||
            CoastPushPresenter && CoastPushPresenter.isActiveAndEnabled && CoastPushPresenter.PoseActive;
        public bool PushContactEstablished => PushPresenter && PushPresenter.isActiveAndEnabled && PushPresenter.ContactEstablished ||
            TheaterPushPresenter && TheaterPushPresenter.isActiveAndEnabled && TheaterPushPresenter.ContactEstablished ||
            SanctumPushPresenter && SanctumPushPresenter.isActiveAndEnabled && SanctumPushPresenter.ContactEstablished ||
            CoastPushPresenter && CoastPushPresenter.isActiveAndEnabled && CoastPushPresenter.ContactEstablished;
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
            player.PushPresenter = root.AddComponent<StarwayPushPresentation>();
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
            if (!PushPresenter) PushPresenter = GetComponentInParent<StarwayPushPresentation>();
            if (!TheaterPushPresenter) TheaterPushPresenter = GetComponentInParent<TheaterPushPresentation>();
            if (!SanctumPushPresenter) SanctumPushPresenter = GetComponentInParent<SanctumPushPresentation>();
            if (!CoastPushPresenter) CoastPushPresenter = GetComponentInParent<CoastPushPresentation>();
            var costumeData = System.Array.Find(definition.costumes ?? System.Array.Empty<StarwayCharacterCostume>(),
                item => item != null && item.id == selected);
            var coastContacts = definition.CoastContactsFor(selected);
            var coastAssetSet = costumeData != null ? costumeData.assetSet : id;
            var coastActive = coastContacts && coastContacts.characterId == id &&
                coastContacts.assetSet == coastAssetSet && coastContacts.costume == selected &&
                (id == "haerin" || id == "stranger" || id == "danu" || id == "eden");
            var coastRest = coastActive && costumeData != null ? costumeData.restingLocalPosition : definition.restingLocalPosition;
            var coastX = coastActive ? (costumeData != null ? costumeData.coastGraphicsScaleXFactor :
                definition.coastGraphicsScaleXFactor) : 1f;
            if (coastX <= 0f || coastX > 1f) coastX = 1f;
            var theaterContacts = definition.theaterPushContacts;
            var theaterActive = theaterContacts && theaterContacts.characterId == id &&
                (id == "seri" || id == "roan" || id == "mion" || id == "piko");
            var sanctumContacts = definition.sanctumPushContacts;
            var sanctumActive = sanctumContacts && sanctumContacts.characterId == id &&
                (id == "sein" || id == "daon" || id == "lumi" || id == "soyul" || id == "doyun");
            if (theaterActive && !TheaterPushPresenter)
            {
                TheaterPushPresenter = transform.parent.gameObject.AddComponent<TheaterPushPresentation>();
                TheaterPushPresenter.enabled = false;
            }
            if (sanctumActive && !SanctumPushPresenter)
            {
                SanctumPushPresenter = transform.parent.gameObject.AddComponent<SanctumPushPresentation>();
                SanctumPushPresenter.enabled = false;
            }
            if (coastActive && !CoastPushPresenter)
            {
                CoastPushPresenter = transform.parent.gameObject.AddComponent<CoastPushPresentation>();
                CoastPushPresenter.enabled = false;
            }
            if (TheaterPushPresenter) { TheaterPushPresenter.ResetPresentation(); TheaterPushPresenter.enabled = false; }
            if (SanctumPushPresenter) { SanctumPushPresenter.ResetPresentation(); SanctumPushPresenter.enabled = false; }
            if (CoastPushPresenter) { CoastPushPresenter.ResetPresentation(); CoastPushPresenter.enabled = false; }
            if (PushPresenter)
            {
                PushPresenter.ResetPresentation();
                var contacts = definition.pushContacts;
                var active = contacts && contacts.characterId == id &&
                    (id == "choa" || id == "uram" || id == "yeoul" || id == "tori");
                PushPresenter.enabled = false;
                var visualScale = active ? RetouchVisualScale : PuzzleVisualScale;
                transform.localScale = new Vector3(visualScale * coastX, visualScale, visualScale);
                transform.localPosition = active || theaterActive || sanctumActive || coastActive
                    ? coastRest : Vector3.zero;
                PushPresenter.SetContacts(active ? contacts : null);
                if (active)
                {
                    var grid = transform.parent.parent;
                    var tile = grid ? grid.TransformVector(Vector3.right).magnitude : 1f;
                    PushPresenter.SetApproachLimit(definition.pushApproachLimit);
                    PushPresenter.Configure(transform, visual, contacts, tile, null,
                        definition.restingLocalPosition);
                }
            }
            if (TheaterPushPresenter)
            {
                TheaterPushPresenter.SetContacts(theaterActive ? theaterContacts : null);
                if (theaterActive)
                {
                    var grid = transform.parent.parent;
                    var tile = grid ? grid.TransformVector(Vector3.right).magnitude : 1f;
                    TheaterPushPresenter.SetApproachLimit(definition.pushApproachLimit);
                    TheaterPushPresenter.Configure(transform, visual, theaterContacts, tile, null,
                        definition.restingLocalPosition);
                }
            }
            if (SanctumPushPresenter)
            {
                SanctumPushPresenter.SetContacts(sanctumActive ? sanctumContacts : null);
                if (sanctumActive)
                {
                    var grid = transform.parent.parent;
                    var tile = grid ? grid.TransformVector(Vector3.right).magnitude : 1f;
                    SanctumPushPresenter.SetApproachLimit(definition.pushApproachLimit);
                    SanctumPushPresenter.Configure(transform, visual, sanctumContacts, tile, null,
                        definition.restingLocalPosition);
                }
            }
            if (CoastPushPresenter)
            {
                CoastPushPresenter.SetContacts(coastActive ? coastContacts : null);
                if (coastActive)
                {
                    var grid = transform.parent.parent;
                    var tile = grid ? grid.TransformVector(Vector3.right).magnitude : 1f;
                    CoastPushPresenter.SetApproachLimit(definition.pushApproachLimit);
                    CoastPushPresenter.Configure(transform, visual, coastContacts, tile, null, coastRest);
                }
            }
            return PlayPose("front", "idle");
        }

        public void BeginPushPresentation(Collider2D target, Vector2 worldDirection, Transform visualContact)
        {
            if (PushPresenter && PushPresenter.isActiveAndEnabled)
                PushPresenter.BeginPush(target, worldDirection, visualContact);
            else if (TheaterPushPresenter && TheaterPushPresenter.isActiveAndEnabled)
                TheaterPushPresenter.BeginPush(target, worldDirection, visualContact);
            else if (SanctumPushPresenter && SanctumPushPresenter.isActiveAndEnabled)
                SanctumPushPresenter.BeginPush(target, worldDirection, visualContact);
            else if (CoastPushPresenter && CoastPushPresenter.isActiveAndEnabled)
                CoastPushPresenter.BeginPush(target, worldDirection, visualContact);
        }

        public void EndPushPresentation()
        {
            if (PushPresenter && PushPresenter.isActiveAndEnabled) PushPresenter.EndPush();
            if (TheaterPushPresenter && TheaterPushPresenter.isActiveAndEnabled) TheaterPushPresenter.EndPush();
            if (SanctumPushPresenter && SanctumPushPresenter.isActiveAndEnabled) SanctumPushPresenter.EndPush();
            if (CoastPushPresenter && CoastPushPresenter.isActiveAndEnabled) CoastPushPresenter.EndPush();
        }

        public void ResetPushPresentation()
        {
            if (PushPresenter) PushPresenter.ResetPresentation();
            if (TheaterPushPresenter) TheaterPushPresenter.ResetPresentation();
            if (SanctumPushPresenter) SanctumPushPresenter.ResetPresentation();
            if (CoastPushPresenter) CoastPushPresenter.ResetPresentation();
        }

        public bool SetCostume(string costume)
        {
            var definition = catalog ? catalog.Find(characterId) : null;
            if (definition == null || string.IsNullOrEmpty(costume) || !definition.HasCostume(costume)) return false;
            if (definition.CoastContactsFor(costume))
            {
                var previousDirection = Direction;
                var previousState = State;
                if (!Initialize(catalog, characterId, costume)) return false;
                return PlayPose(previousDirection, previousState);
            }
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
            if (next == null && state == "push")
            {
                next = definition.Motion(nextDirection, "move", costumeId);
                state = "move";
            }
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
