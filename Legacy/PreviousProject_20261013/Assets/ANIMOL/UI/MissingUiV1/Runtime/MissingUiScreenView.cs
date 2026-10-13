using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.MissingUiV1
{
    [DisallowMultipleComponent]
    public sealed class MissingUiScreenView : MonoBehaviour
    {
        public string ScreenId;
        public string ExistingScreenReference;
        public bool ReadOnlyDesignPreview = true;
        public MissingUiHostBridge Host;
        public MissingUiBinding[] Bindings;
        public GameObject PreviewResetPanel;
        public GameObject PreviewImagePanel;
        public Image PreviewImage;
        public MissingUiControlDiagram ControlDiagram;
        [NonSerialized] public Action<string> PreviewNavigate;
        private readonly Dictionary<string, string> _localValues = new Dictionary<string, string>();
        private readonly HashSet<int> _wired = new HashSet<int>();
        private bool _initialized;
        private bool _modalOpen;

        private void OnEnable()
        {
            if (ReadOnlyDesignPreview && !_initialized) { foreach (var pair in MissingUiDesignDefaults.Create()) _localValues[pair.Key] = pair.Value; _initialized = true; }
            Wire(); RefreshCapabilities(); if (ReadOnlyDesignPreview && ControlDiagram != null) ControlDiagram.Apply(_localValues);
        }
        private void Wire()
        {
            if (Bindings == null) return;
            foreach (var binding in Bindings)
            {
                if (binding == null || binding.Control == null) continue;
                if (!_wired.Add(binding.GetInstanceID())) continue;
                var button = binding.Control as Button;
                if (button != null)
                {
                    var captured = binding;
                    button.onClick.AddListener(() => Invoke(captured));
                }
                var slider = binding.Control as Slider;
                if (slider != null)
                {
                    var captured = binding;
                    slider.onValueChanged.AddListener(value => OnLocalValue(captured, value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
                }
                var toggle = binding.Control as Toggle;
                if (toggle != null)
                {
                    var captured = binding;
                    toggle.onValueChanged.AddListener(value => OnLocalValue(captured, value ? "true" : "false"));
                }
                var input = binding.Control as InputField;
                if (input != null)
                {
                    var captured = binding;
                    input.onValueChanged.AddListener(value => OnLocalValue(captured, value));
                }
                else if (binding.Kind == "input")
                {
                    var property = binding.Control.GetType().GetProperty("onValueChanged");
                    var changed = property == null ? null : property.GetValue(binding.Control, null) as UnityEvent<string>;
                    var captured = binding;
                    if (changed != null) changed.AddListener(value => OnLocalValue(captured, value));
                }
            }
        }
        public void RefreshCapabilities()
        {
            if (Bindings == null) return;
            foreach (var binding in Bindings)
                if (binding != null && binding.Control != null) binding.Control.interactable = IsAllowed(binding);
        }
        private bool IsAllowed(MissingUiBinding binding)
        {
            if (string.IsNullOrEmpty(binding.Action)) return false;
            if (_modalOpen && binding.Kind != "preview-modal") return false;
            if (ReadOnlyDesignPreview)
                return !binding.RequiresHost && binding.DesignEnabled;
            return Host != null && Host.CanInvoke(ScreenId, binding.SemanticKey, binding.Action);
        }
        private void OnLocalValue(MissingUiBinding binding, string value)
        {
            if (!IsAllowed(binding)) return;
            _localValues[binding.SemanticKey] = value;
            if (ReadOnlyDesignPreview && ControlDiagram != null) ControlDiagram.Apply(_localValues);
            if (!ReadOnlyDesignPreview) Invoke(binding);
        }
        public string GetLocalValue(string semanticKey)
        {
            if (string.IsNullOrEmpty(semanticKey)) return string.Empty;
            string value;
            return _localValues.TryGetValue(semanticKey, out value) ? value : string.Empty;
        }
        public void Invoke(MissingUiBinding binding)
        {
            if (binding == null || !IsAllowed(binding)) return;
            if (ReadOnlyDesignPreview)
            {
                const string prefix = "navigate:";
                if (binding.Action.StartsWith(prefix, StringComparison.Ordinal))
                    PreviewNavigate?.Invoke(binding.Action.Substring(prefix.Length));
                else if (binding.Action == "back") PreviewNavigate?.Invoke("back");
                else if (binding.Action == "reset-settings" || binding.Action == "reset-controls")
                {
                    if (PreviewResetPanel != null) PreviewResetPanel.SetActive(true);
                    SetModalOpen(true, PreviewResetPanel);
                }
                else if (binding.Action == "local:cancel-reset")
                {
                    if (PreviewResetPanel != null) PreviewResetPanel.SetActive(false);
                    SetModalOpen(false, null);
                }
                else if (binding.Action == "local:confirm-reset") ResetDesignWidgets();
                else if (binding.Action == "local:inspect-image")
                {
                    if (PreviewImage != null) PreviewImage.sprite = binding.InspectSprite;
                    if (PreviewImagePanel != null) PreviewImagePanel.SetActive(true);
                    SetModalOpen(true, PreviewImagePanel);
                }
                else if (binding.Action == "local:close-image")
                {
                    if (PreviewImagePanel != null) PreviewImagePanel.SetActive(false);
                    SetModalOpen(false, null);
                }
                else if (!string.IsNullOrEmpty(binding.LocalChoiceValue))
                {
                    _localValues[binding.LocalGroupKey] = binding.LocalChoiceValue;
                    foreach (var option in Bindings)
                    {
                        if (option == null || option.LocalGroupKey != binding.LocalGroupKey || option.Control == null) continue;
                        var visual = option.Control.GetComponent<MissingUiChoiceVisual>(); if (visual != null) visual.Apply(option == binding);
                    }
                    if (ControlDiagram != null) ControlDiagram.Apply(_localValues);
                }
                return; // No IAP, network, storage, room mutation or simulated service acceptance.
            }
            Host.Invoke(new MissingUiAction { screenId = ScreenId, semanticKey = binding.SemanticKey,
                action = binding.Action, localValue = string.IsNullOrEmpty(binding.LocalChoiceValue) ? GetLocalValue(binding.SemanticKey) : binding.LocalChoiceValue });
            RefreshCapabilities();
        }
        private void ResetDesignWidgets()
        {
            _localValues.Clear();
            foreach (var pair in MissingUiDesignDefaults.Create()) _localValues[pair.Key] = pair.Value;
            if (Bindings != null) foreach (var binding in Bindings)
            {
                if (binding == null || binding.Control == null) continue;
                var choice = binding.Control.GetComponent<MissingUiChoiceVisual>(); if (choice != null) choice.Apply(GetLocalValue(binding.LocalGroupKey) == binding.LocalChoiceValue);
                var slider = binding.Control as Slider;
                float value;
                if (slider != null && float.TryParse(GetLocalValue(binding.SemanticKey), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value)) slider.SetValueWithoutNotify(value);
                var toggle = binding.Control as Toggle;
                if (toggle != null) { toggle.SetIsOnWithoutNotify(GetLocalValue(binding.SemanticKey) == "true"); var visual = toggle.GetComponent<MissingUiToggleVisual>(); if (visual != null) visual.Refresh(); }
                if (binding.Kind == "input")
                {
                    var property = binding.Control.GetType().GetProperty("text");
                    if (property != null && property.CanWrite) property.SetValue(binding.Control, string.Empty, null);
                }
            }
            if (PreviewResetPanel != null) PreviewResetPanel.SetActive(false);
            SetModalOpen(false, null);
            if (ControlDiagram != null) ControlDiagram.Apply(_localValues);
        }
        private void SetModalOpen(bool open, GameObject panel)
        {
            _modalOpen = open; RefreshCapabilities();
            if (EventSystem.current == null) return;
            if (open && panel != null) { var button = panel.GetComponentInChildren<Button>(false); if (button != null) EventSystem.current.SetSelectedGameObject(button.gameObject); }
            else EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
