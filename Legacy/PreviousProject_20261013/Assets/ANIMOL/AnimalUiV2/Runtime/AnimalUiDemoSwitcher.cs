using UnityEngine;

namespace ANIMOL.AnimalUiV2
{
    /// <summary>Isolated demo only. Cycles screens through the otherwise disabled refresh button.</summary>
    public sealed class AnimalUiDemoSwitcher : MonoBehaviour
    {
        public AnimalUiPresenter[] Screens;
        private int _index;
        private void Start()
        {
            foreach (var screen in Screens)
            {
                var captured = screen;
                captured.View.Refresh.onClick.AddListener(() => { if (captured.Backend == null) Next(); });
                captured.Cancelled += (mode, loadout) => { if (captured.Backend == null) Next(); };
                var label = captured.View.Refresh.GetComponentInChildren<UnityEngine.UI.Text>();
                if (label != null) label.text = "화면\n전환";
            }
        }
        private void LateUpdate()
        {
            if (Screens == null || Screens.Length == 0) return;
            var screen = Screens[_index];
            if (screen != null && screen.Backend == null) screen.View.Refresh.interactable = true;
        }
        private void Next()
        {
            Screens[_index].gameObject.SetActive(false); _index = (_index + 1) % Screens.Length;
            Screens[_index].gameObject.SetActive(true);
        }
    }
}
