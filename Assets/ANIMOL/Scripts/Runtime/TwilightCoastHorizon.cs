using UnityEngine;

namespace ANIMOL.Puzzle
{
    // World landmark: one core, distinct horizontal extensions, no camera/parallax component.
    public sealed class TwilightCoastHorizon : MonoBehaviour
    {
        public TwilightCoastArt art;
        [Min(0)] public int leftRepeats = 2, rightRepeats = 2;
        public bool separatedSun;
        Transform images;
        public void Assemble(int left, int right, bool separated = false)
        {
            leftRepeats = Mathf.Max(0, left); rightRepeats = Mathf.Max(0, right); separatedSun = separated;
            if (!images) images = transform.Find("Single sunset and extensions");
            if (images) { images.gameObject.SetActive(false); if (Application.isPlaying) Destroy(images.gameObject); else DestroyImmediate(images.gameObject); }
            images = new GameObject("Single sunset and extensions").transform; images.SetParent(transform, false);
            Image(separated ? "Horizon without sun" : "Single horizon core", separated ? art.horizonWithoutSun : art.horizonCore, Vector2.zero, -60);
            for (var k = 0; k < leftRepeats; k++) Image("Left repeat", art.horizonLeft, new Vector2(-k - 1, 0), -60);
            for (var k = 0; k < rightRepeats; k++) Image("Right repeat", art.horizonRight, new Vector2(12 + k, 0), -60);
            if (separated)
            {
                Image("Single setting sun", art.Prop("setting_sun_240x160"), new Vector2(4.5f, -1), -59);
                Image("Single sun reflection", art.Prop("sun_reflection_240x160"), new Vector2(4.5f, -2.725f), -58);
            }
        }
        SpriteRenderer Image(string name, Sprite sprite, Vector2 p, int order)
        {
            var root = new GameObject(name); root.transform.SetParent(images, false); root.transform.localPosition = p;
            var image = root.AddComponent<SpriteRenderer>(); image.sprite = sprite; image.sortingOrder = order; return image;
        }
    }
}
