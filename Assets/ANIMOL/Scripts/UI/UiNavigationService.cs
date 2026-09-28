using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.UI
{
    public sealed class UiNavigationService : MonoBehaviour
    {
        [SerializeField] private Transform screenHost;
        [SerializeField] private string initialScreenId = "SC01_Lobby";
        private readonly Dictionary<string, GameObject> screens = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Stack<string> history = new Stack<string>();
        public string CurrentScreenId { get; private set; } = string.Empty;
        public event Action<string> ScreenChanged;

        private void Awake()
        {
            if (screenHost == null) screenHost = transform.Find("SafeArea/ScreenHost");
            RebuildIndex();
            ShowInitial();
        }

        public void RebuildIndex()
        {
            screens.Clear();
            if (screenHost == null) return;
            foreach (Transform child in screenHost) screens[child.name] = child.gameObject;
        }

        public bool Navigate(string screenId, bool rememberCurrent = true)
        {
            if (!screens.TryGetValue(screenId, out var next))
            {
                RebuildIndex();
                if (!screens.TryGetValue(screenId, out next)) return false;
            }
            if (rememberCurrent && !string.IsNullOrEmpty(CurrentScreenId) && CurrentScreenId != screenId) history.Push(CurrentScreenId);
            foreach (var item in screens) item.Value.SetActive(item.Value == next);
            CurrentScreenId = screenId;
            ScreenChanged?.Invoke(screenId);
            return true;
        }

        public bool Back()
        {
            if (history.Count == 0) return false;
            return Navigate(history.Pop(), false);
        }

        private void ShowInitial()
        {
            if (!Navigate(initialScreenId, false) && screenHost != null && screenHost.childCount > 0)
                Navigate(screenHost.GetChild(0).name, false);
        }
    }
}
