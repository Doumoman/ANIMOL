using UnityEngine;

namespace ANIMOL.Art.EchoRailCartV01
{
    // Art-only, on a dedicated visual child. Host owns movement, collision, turns and rollback.
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class EchoRailCartVisualV01 : MonoBehaviour
    {
        [SerializeField] EchoRailCartArtCatalogV01 catalog;
        SpriteRenderer owned;
        public bool Configure(EchoRailCartArtCatalogV01 value, out string error)
        {
            catalog = value; owned = GetComponent<SpriteRenderer>();
            if (catalog == null) { error = "Rail-cart catalog required."; return false; }
            return catalog.Validate(out error);
        }
        public bool ShowCart(string heading, bool contact = false)
        {
            if (heading != "north" && heading != "east" && heading != "south" && heading != "west") return Fail("Use a cardinal heading.");
            return Apply("cart/" + heading + (contact ? "/contact" : "/idle"));
        }
        public bool ApplyContactProgress(string heading, float normalizedProgress)
        {
            if (float.IsNaN(normalizedProgress) || float.IsInfinity(normalizedProgress)) return Fail("Finite contact progress required.");
            return ShowCart(heading, normalizedProgress >= 1f / 3f && normalizedProgress < 2f / 3f);
        }
        public bool ShowRail(string connectionKey) { return Apply("rail/" + connectionKey); }
        bool Apply(string key)
        {
            if (owned == null) owned = GetComponent<SpriteRenderer>();
            Sprite s;
            if (catalog == null || !catalog.TrySprite(key, out s)) return Fail("Missing rail-cart art key: " + key);
            owned.sprite = s; return true;
        }
        bool Fail(string message)
        {
            if (owned == null) owned = GetComponent<SpriteRenderer>();
            if (owned != null) owned.sprite = null;
            Debug.LogError(message, this); return false;
        }
    }
}
