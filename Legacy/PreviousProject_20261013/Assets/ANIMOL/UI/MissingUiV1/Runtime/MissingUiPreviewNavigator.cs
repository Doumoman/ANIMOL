using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.MissingUiV1
{
    public sealed class MissingUiPreviewNavigator : MonoBehaviour
    {
        public MissingUiScreenView[] Screens;
        public string InitialScreenId;
        private readonly Stack<string> _history = new Stack<string>();
        private string _current;
        private void Start()
        {
            if (Screens == null || Screens.Length == 0) return;
            foreach (var screen in Screens) if (screen != null) screen.PreviewNavigate = Navigate;
            Navigate(string.IsNullOrEmpty(InitialScreenId) ? Screens[0].ScreenId : InitialScreenId);
        }
        public void Navigate(string id)
        {
            var back = id == "back";
            if (back) id = _history.Count == 0 ? InitialScreenId : _history.Pop();
            MissingUiScreenView target = null;
            foreach (var screen in Screens)
                if (screen != null && string.Equals(screen.ScreenId, id, StringComparison.Ordinal)) target = screen;
            if (target == null) return;
            if (!back && !string.IsNullOrEmpty(_current) && _current != id) _history.Push(_current);
            _current = id;
            foreach (var screen in Screens) if (screen != null) screen.gameObject.SetActive(screen == target);
        }
    }
}
