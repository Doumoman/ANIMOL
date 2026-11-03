using UnityEngine;

namespace ANIMOL.Art.RanchDevicesV13
{
    // Presentation only. No input, collision, occupancy, cost, turn or gameplay event is generated here.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class RanchDeviceVisualV13 : MonoBehaviour
    {
        [SerializeField] RanchDevicesV13Catalog catalog;
        [SerializeField] string inspectionAutoplayKey = "";
        SpriteRenderer owned;
        RanchV13Clip clip;
        string currentKey;
        float elapsed;
        public string CurrentKey { get { return currentKey; } }
        public bool Finished { get; private set; }
        void Awake() { owned = GetComponent<SpriteRenderer>(); }
        void OnEnable()
        {
            if (owned == null) owned = GetComponent<SpriteRenderer>();
            Clear();
            if (!string.IsNullOrEmpty(inspectionAutoplayKey)) SetState(inspectionAutoplayKey, true);
        }
        void OnDisable() { Clear(); }
        public bool Configure(RanchDevicesV13Catalog value, out string error)
        {
            Clear(); catalog = value;
            if (catalog == null) { error = "Catalog required."; return false; }
            return catalog.Validate(out error);
        }
        public void ConfigureInspectionAutoplay(string key) { inspectionAutoplayKey = key; }
        public bool SetState(string key, bool restart = false)
        {
            string error;
            if (catalog == null || !catalog.Validate(out error)) return Fail("Configure a current complete V13 catalog before SetState.");
            RanchV13Clip found;
            if (!catalog.TryClip(key, out found)) return Fail("Unknown V13 visual key: " + key);
            if (!restart && currentKey == key && clip != null) return true;
            if (owned == null) owned = GetComponent<SpriteRenderer>();
            currentKey = key; clip = found; elapsed = 0; Finished = false;
            owned.sprite = clip.frames[0]; owned.enabled = true; return true;
        }
        void Update()
        {
            if (clip == null) return;
            elapsed += Time.deltaTime;
            int frame = Mathf.FloorToInt(elapsed * clip.fps);
            if (clip.loop) frame %= clip.frames.Length;
            else { Finished = frame >= clip.frames.Length; frame = Mathf.Min(frame, clip.frames.Length - 1); }
            owned.sprite = clip.frames[frame];
        }
        public void Clear()
        {
            clip = null; currentKey = null; elapsed = 0; Finished = false;
            if (owned == null) owned = GetComponent<SpriteRenderer>();
            if (owned != null) { owned.sprite = null; owned.enabled = false; }
        }
        bool Fail(string error) { Clear(); Debug.LogError(error, this); return false; }
    }
}
