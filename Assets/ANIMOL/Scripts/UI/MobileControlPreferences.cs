using UnityEngine;

namespace ANIMOL.UI
{
    public enum UiLanguage { Korean, English }

    public static class MobileControlPreferences
    {
        private const string Prefix = "ANIMOL.M6.";
        public static float SizeScale { get => PlayerPrefs.GetFloat(Prefix + "ControlSize", 1f); set => PlayerPrefs.SetFloat(Prefix + "ControlSize", Mathf.Clamp(value, .75f, 1.35f)); }
        public static float HorizontalOffset { get => PlayerPrefs.GetFloat(Prefix + "ControlOffset", 0f); set => PlayerPrefs.SetFloat(Prefix + "ControlOffset", Mathf.Clamp(value, -.08f, .08f)); }
        public static float Opacity { get => PlayerPrefs.GetFloat(Prefix + "ControlOpacity", .82f); set => PlayerPrefs.SetFloat(Prefix + "ControlOpacity", Mathf.Clamp(value, .3f, 1f)); }
        public static bool LargeText { get => PlayerPrefs.GetInt(Prefix + "LargeText", 0) == 1; set => PlayerPrefs.SetInt(Prefix + "LargeText", value ? 1 : 0); }
        public static bool Vibration { get => PlayerPrefs.GetInt(Prefix + "Vibration", 1) == 1; set => PlayerPrefs.SetInt(Prefix + "Vibration", value ? 1 : 0); }
        public static UiLanguage Language { get => (UiLanguage)PlayerPrefs.GetInt(Prefix + "Language", 0); set => PlayerPrefs.SetInt(Prefix + "Language", (int)value); }
        public static void Reset()
        {
            SizeScale = 1f; HorizontalOffset = 0f; Opacity = .82f; LargeText = false; Vibration = true; Language = UiLanguage.Korean;
            PlayerPrefs.Save();
        }
        public static string Localize(string korean, string english) => Language == UiLanguage.English ? english : korean;
    }

}
