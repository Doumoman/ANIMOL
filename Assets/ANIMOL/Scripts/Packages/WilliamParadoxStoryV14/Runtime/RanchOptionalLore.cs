using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WilliamParadox.Story
{
    /// <summary>Optional environment reading. Never changes a story node, save flag or puzzle result.</summary>
    public sealed class RanchOptionalLore : MonoBehaviour
    {
        [Serializable] public sealed class Entry { public string id, visualMark; public string[] fragments; }
        [Serializable] private sealed class Catalog { public Entry[] lore; }
        public TextAsset catalog;
        public StoryRunner runner;
        public Button clockButton, roomButton, nextButton, closeButton;
        public GameObject panel;
        public TMP_Text body;
        private Entry entry;
        private int fragment;
        private void Awake()
        {
            clockButton.onClick.AddListener(() => Open("LORE_CLOCK"));
            roomButton.onClick.AddListener(() => Open("LORE_ROOM"));
            nextButton.onClick.AddListener(Next);
            closeButton.onClick.AddListener(Close);
            runner.onNodeChanged.AddListener(OnNodeChanged);
            Close();
        }
        public void Open(string id)
        {
            if (catalog == null) return;
            entry = Array.Find(JsonUtility.FromJson<Catalog>(catalog.text).lore ?? Array.Empty<Entry>(), e => e.id == id);
            if (entry == null || entry.fragments == null || entry.fragments.Length == 0) return;
            fragment = 0; panel.SetActive(true); Show();
        }
        private void Next() { if (entry != null && fragment + 1 < entry.fragments.Length) { fragment++; Show(); } }
        private void Show()
        {
            body.richText = false;
            body.text = entry.visualMark + "\n\n" + entry.fragments[fragment];
            nextButton.interactable = fragment + 1 < entry.fragments.Length;
        }
        private void OnNodeChanged(string unused)
        {
            Close();
            bool environment = runner.CurrentSceneId == "R01" || runner.CurrentSceneId == "R04" || runner.CurrentSceneId == "R08";
            clockButton.gameObject.SetActive(environment); roomButton.gameObject.SetActive(environment);
        }
        public void Close() { if (panel != null) panel.SetActive(false); entry = null; }
        private void OnDisable() { Close(); }
        private void OnDestroy() { if (runner != null) runner.onNodeChanged.RemoveListener(OnNodeChanged); }
    }
}
