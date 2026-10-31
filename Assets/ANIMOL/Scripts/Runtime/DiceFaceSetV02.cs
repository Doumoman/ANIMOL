using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    [CreateAssetMenu(menuName = "ANIMOL/Dice face set V02")]
    public sealed class DiceFaceSetV02 : ScriptableObject
    {
        public string sourceSha256;
        public Sprite[] faces = new Sprite[6];
        public bool Validate(out string error)
        {
            error = null;
            if (faces == null || faces.Length != 6) { error = "Exactly six die face sprites are required."; return false; }
            for (int i = 0; i < 6; i++)
                if (faces[i] == null) { error = "Missing die face: " + (i + 1); return false; }
            return true;
        }
        public bool TryGet(int value, out Sprite face)
        {
            face = null;
            if (value < 1 || value > 6 || faces == null || faces.Length != 6) return false;
            face = faces[value - 1]; return face != null;
        }
    }

    public static class DiceFaceLayoutV02
    {
        // Authored faces have 224 visible pixels inside a 256-pixel canvas.
        public const float HintVisibleCoverage = .98f;
        public const float HintWidth = HintVisibleCoverage * 256f / 224f;
        public const float GoalWidth = .60f;
        // Full-rect Sprite import: normalize the image's world width, preserving its aspect ratio.
        public static void Place(Transform root, Sprite sprite, Vector3 center, float cellWorldSize, float widthInCells)
        {
            root.position = center; root.rotation = Quaternion.identity; root.localScale = Vector3.one;
            float parentWidth = root.TransformVector(Vector3.right * sprite.bounds.size.x).magnitude;
            if (parentWidth > .00001f) root.localScale = Vector3.one * (cellWorldSize * widthInCells / parentWidth);
        }
    }
}
