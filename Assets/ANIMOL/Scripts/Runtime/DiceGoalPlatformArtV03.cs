using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01
{
    [CreateAssetMenu(menuName = "ANIMOL/Dice goal platform art V03")]
    public sealed class DiceGoalPlatformArtV03 : ScriptableObject
    {
        public Sprite ready, halfPressed, nearlyFlush, walkable;
        public Sprite frontRim, closingRim, lidLeft, lidRight;
        public Shader rigidClipShader;
        public Vector4 apertureCells = new Vector4(-.34f, -.34f, .34f, .34f);
        public string sourceSha256;
        public bool Validate(out string error)
        {
            error = null;
            if (ready == null || halfPressed == null || nearlyFlush == null || walkable == null
                || frontRim == null || closingRim == null || lidLeft == null || lidRight == null || rigidClipShader == null)
            { error = "R4 requires the receiver states, four animation layers and the rigid clipping shader."; return false; }
            return true;
        }
    }
}
