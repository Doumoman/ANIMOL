using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.UI
{
    public sealed class MobileControlLayoutApplier : MonoBehaviour
    {
        private static readonly HashSet<string> AdjustableNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "SwipeMoveRegion", "AnimalGroundButton", "AnimalGliderButton", "JumpActionButton", "SpecialActionButton", "AbilityActionButton"
        };

        private void Start() => Apply();

        public void Apply()
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (!AdjustableNames.Contains(rect.name)) continue;
                rect.localScale = Vector3.one * MobileControlPreferences.SizeScale;
                var side = rect.name.StartsWith("Animal", StringComparison.Ordinal) || rect.name == "SwipeMoveRegion" ? -1f : 1f;
                rect.anchoredPosition = new Vector2(side * MobileControlPreferences.HorizontalOffset * 1920f, rect.anchoredPosition.y);
                var image = rect.GetComponent<Image>();
                if (image != null)
                {
                    var color = image.color;
                    color.a = MobileControlPreferences.Opacity;
                    image.color = color;
                }
            }
            foreach (var label in GetComponentsInChildren<Text>(true))
                label.resizeTextForBestFit = MobileControlPreferences.LargeText;
            SetControlText("SwipeMoveRegion", "↔ 좌우 스와이프 이동", "↔ Swipe to move");
            SetControlText("AnimalGroundButton", "지상", "Ground");
            SetControlText("AnimalGliderButton", "공중", "Glider");
            SetControlText("JumpActionButton", "점프", "Jump");
            SetControlText("SpecialActionButton", "특수\n미연결", "Special\nUnavailable");
            SetControlText("AbilityActionButton", "능력\n미연결", "Ability\nUnavailable");
        }

        private void SetControlText(string controlName, string korean, string english)
        {
            foreach (var text in GetComponentsInChildren<Text>(true))
                if (text.transform.parent != null && text.transform.parent.name == controlName)
                    text.text = MobileControlPreferences.Localize(korean, english);
        }
    }
}
