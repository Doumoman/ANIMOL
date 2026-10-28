using UnityEngine;
using ANIMOL.Presentation;

namespace ANIMOL.Puzzle
{
    // V3 uses the opposite north/south chirality from the older baked 24-state sheet.
    // These labels show the authoritative six-face state without remapping its numbers.
    public sealed class EchoTheaterDiceFaceView : MonoBehaviour
    {
        TextMesh top, north, east;
        int[] shown;
        float rollTime;
        Vector2Int rollHeading;
        static Sprite square;

        static TextMesh Label(Transform parent, string name, Vector3 position, int size)
        {
            var child = new GameObject(name); child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            var label = child.AddComponent<TextMesh>(); label.fontSize = size; label.characterSize = .09f;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.color = new Color(.19f, .13f, .25f);
            child.GetComponent<MeshRenderer>().sortingOrder = 32;
            return label;
        }
        public void Configure()
        {
            var view = GetComponent<DeviceVisualView>();
            if (view) view.enabled = false;
            var animated = GetComponent<ANIMOL.Art.EchoTheaterV03.EchoTheaterV03AnimatedVisual>();
            if (animated) animated.enabled = false;
            if (!square) square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1),
                new Vector2(.5f, .5f), 1f);
            var image = view && view.visual ? view.visual : GetComponentInChildren<SpriteRenderer>();
            if (image) { image.sprite = square; image.color = new Color(.96f, .89f, .72f); image.sortingOrder = 30;
                image.transform.localScale = Vector3.one * .78f; }
            top = Label(transform, "Top face", new Vector3(0, .18f, -.01f), 54);
            north = Label(transform, "North face", new Vector3(-.18f, -.17f, -.01f), 29);
            east = Label(transform, "East face", new Vector3(.18f, -.17f, -.01f), 29);
        }
        public void Present(int[] faces)
        {
            if (faces == null || faces.Length != 6) return;
            if (shown != null && shown[0] != faces[0]) rollTime = .15f;
            shown = (int[])faces.Clone();
            top.text = faces[0].ToString(); north.text = faces[2].ToString(); east.text = faces[4].ToString();
        }
        public void BeginRoll(Vector2Int heading)
        {
            rollHeading = heading;
            rollTime = .15f;
        }
        void Update()
        {
            if (rollTime <= 0f) { transform.localScale = Vector3.one; transform.localRotation = Quaternion.identity; return; }
            rollTime = Mathf.Max(0f, rollTime - Time.unscaledDeltaTime);
            transform.localScale = Vector3.one * (1f + .08f * Mathf.Sin(rollTime / .15f * Mathf.PI));
            var angle = 70f * rollTime / .15f;
            transform.localRotation = Quaternion.Euler(rollHeading.y * angle, -rollHeading.x * angle, 0f);
        }
    }
}
