using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI
{
    public sealed class MobileControlSettingsPresenter : MonoBehaviour
    {
        private UiNavigationService navigation;
        private readonly List<(Button, UnityEngine.Events.UnityAction)> bindings = new List<(Button, UnityEngine.Events.UnityAction)>();

        private void OnEnable()
        {
            navigation = GetComponent<UiNavigationService>();
            Bind("ControlSettingsButton", () => { Refresh(); navigation.Navigate("SC19_ControlSettings"); });
            Bind("ControlSizeDownButton", () => { MobileControlPreferences.SizeScale -= .1f; Refresh(); });
            Bind("ControlSizeUpButton", () => { MobileControlPreferences.SizeScale += .1f; Refresh(); });
            Bind("ControlPositionButton", () => { MobileControlPreferences.HorizontalOffset = MobileControlPreferences.HorizontalOffset >= .08f ? -.08f : MobileControlPreferences.HorizontalOffset + .04f; Refresh(); });
            Bind("ControlOpacityButton", () => { MobileControlPreferences.Opacity = MobileControlPreferences.Opacity <= .4f ? 1f : MobileControlPreferences.Opacity - .2f; Refresh(); });
            Bind("LanguageToggleButton", () => { MobileControlPreferences.Language = MobileControlPreferences.Language == UiLanguage.Korean ? UiLanguage.English : UiLanguage.Korean; Refresh(); });
            Bind("LargeTextToggleButton", () => { MobileControlPreferences.LargeText = !MobileControlPreferences.LargeText; Refresh(); });
            Bind("VibrationToggleButton", () => { MobileControlPreferences.Vibration = !MobileControlPreferences.Vibration; Refresh(); });
            Bind("ControlResetButton", () => { MobileControlPreferences.Reset(); Refresh(); });
            Bind("ControlSettingsBackButton", () => navigation.Back());
            Refresh();
        }

        private void OnDisable()
        {
            foreach (var item in bindings) item.Item1.onClick.RemoveListener(item.Item2);
            bindings.Clear();
        }

        private void Bind(string name, UnityEngine.Events.UnityAction action)
        {
            var button = Find<Button>(name);
            if (button == null) return;
            button.onClick.AddListener(action);
            bindings.Add((button, action));
        }

        private T Find<T>(string name) where T : Component
        {
            foreach (var item in GetComponentsInChildren<T>(true))
                if (item.name == name) return item;
            return null;
        }

        private void Refresh()
        {
            var summary = Find<Text>("ControlSettingsSummary");
            if (summary == null) return;
            SetText("ControlSettingsTitle", "조작·접근성", "Controls & Accessibility");
            SetButtonText("ControlSizeDownButton", "크기 −", "Size −");
            SetButtonText("ControlSizeUpButton", "크기 +", "Size +");
            SetButtonText("ControlPositionButton", "위치 변경", "Move controls");
            SetButtonText("ControlOpacityButton", "투명도", "Opacity");
            SetButtonText("LanguageToggleButton", "한국어 / English", "English / 한국어");
            SetButtonText("LargeTextToggleButton", "큰 글씨 A+", "Large text A+");
            SetButtonText("VibrationToggleButton", "진동 ◉", "Vibration ◉");
            SetButtonText("ControlResetButton", "초기화", "Reset");
            SetButtonText("ControlSettingsBackButton", "← 설정", "← Settings");
            summary.text = MobileControlPreferences.Localize(
                $"조작 크기 {MobileControlPreferences.SizeScale:0.0} · 위치 {MobileControlPreferences.HorizontalOffset:0.00} · 투명도 {MobileControlPreferences.Opacity:0.0}\n큰 글씨 {(MobileControlPreferences.LargeText ? "켜짐 ✓" : "꺼짐 ✕")} · 진동 {(MobileControlPreferences.Vibration ? "켜짐 ✓" : "꺼짐 ✕")} · 한국어/English",
                $"Control size {MobileControlPreferences.SizeScale:0.0} · offset {MobileControlPreferences.HorizontalOffset:0.00} · opacity {MobileControlPreferences.Opacity:0.0}\nLarge text {(MobileControlPreferences.LargeText ? "ON ✓" : "OFF ✕")} · vibration {(MobileControlPreferences.Vibration ? "ON ✓" : "OFF ✕")} · English/한국어");
            PlayerPrefs.Save();
        }

        private void SetText(string name, string korean, string english)
        {
            var text = Find<Text>(name);
            if (text != null) text.text = MobileControlPreferences.Localize(korean, english);
        }

        private void SetButtonText(string name, string korean, string english)
        {
            var button = Find<Button>(name);
            var text = button == null ? null : button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = MobileControlPreferences.Localize(korean, english);
        }
    }
}
