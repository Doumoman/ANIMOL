using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.MissingUiV1.Project
{
    public sealed class MissingUtilityArt : ScriptableObject
    {
        public MissingUiArt Common;
        public Sprite Music, Sound, Mute, Vibration, Controls, TextSize, Language, Account, LocalSave, HandLeft, HandRight, Opacity, Device;
        public Sprite SliderRail, SliderFill, SliderThumb, ToggleOff, ToggleOn, ToggleKnob;
        // References to the actual authored HUD images. Only visuals are copied; no gameplay controls/listeners.
        public Image[] HudSources;
    }
}
