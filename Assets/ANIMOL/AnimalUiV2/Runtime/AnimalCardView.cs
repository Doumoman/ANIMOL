using System;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.AnimalUiV2
{
    public sealed class AnimalCardView : MonoBehaviour
    {
        public Button Button;
        public Image Portrait;
        public Text Name;
        public Text Badge;
        public Image SelectionMark;
        public Image LockMark;
        private string _id;
        private Action<string> _clicked;
        private void Awake() => Button.onClick.AddListener(() => _clicked?.Invoke(_id));
        public void Bind(AnimalDefinition definition, string badge, bool selected, bool locked, Action<string> clicked)
        {
            _id = definition?.Id; _clicked = clicked;
            Portrait.sprite = definition?.Portrait;
            Portrait.preserveAspect = true; Portrait.useSpriteMesh = false;
            Portrait.enabled = Portrait.sprite != null;
            Name.text = definition == null ? "선택 전" : definition.DisplayName;
            Badge.text = badge ?? "";
            if (SelectionMark != null) SelectionMark.gameObject.SetActive(selected);
            if (LockMark != null) LockMark.gameObject.SetActive(locked);
        }
    }
}
