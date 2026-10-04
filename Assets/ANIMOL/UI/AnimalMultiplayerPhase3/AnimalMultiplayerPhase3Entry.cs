using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.UI;
using ANIMOL.PortraitArtV1;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.AnimalMultiplayerPhase3
{
    [DefaultExecutionOrder(1100)]
    public sealed class AnimalMultiplayerPhase3Entry : MonoBehaviour
    {
        private readonly List<(Button button, Button.ButtonClickedEvent original)> bindings = new List<(Button, Button.ButtonClickedEvent)>();
        private PortraitEntryController lobby;
        public AnimalMultiplayerPhase3Host Host { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => SceneManager.sceneLoaded -= Install;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() { SceneManager.sceneLoaded -= Install; SceneManager.sceneLoaded += Install; }
        private static void Install(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Lobby") return;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var nav in root.GetComponentsInChildren<UiNavigationService>(true))
                if (nav.GetComponent<MultiplayerUiPresenter>() != null && nav.GetComponent<AnimalMultiplayerPhase3Entry>() == null)
                    nav.gameObject.AddComponent<AnimalMultiplayerPhase3Entry>();
        }
        private IEnumerator Start()
        {
            yield return null; yield return null;
            var nav = GetComponent<UiNavigationService>();
            var screenHost = nav.transform.Find("SafeArea/ScreenHost");
            var prefab = Resources.Load<GameObject>(AnimalMultiplayerPhase3Host.ResourcePath);
            if (screenHost == null || prefab == null) yield break;
            var instance = Instantiate(prefab, screenHost, false);
            instance.name = AnimalMultiplayerPhase3Host.ScreenId;
            Host = instance.GetComponent<AnimalMultiplayerPhase3Host>(); instance.SetActive(false);
            nav.RebuildIndex();
            var hub = screenHost.Find("SC05_CompetitiveHub");
            if (hub == null) yield break;
            // Only these existing pre-entry actions change destination. Legacy DEV room/Ready stays intact.
            foreach (string name in new[] { "CompetitiveMatchButton", "RankedMatchButton", "CompetitivePrivateButton" })
            {
                var button = hub.GetComponentsInChildren<Button>(true).SingleOrDefault(b => b.name == name);
                if (button == null) continue;
                bindings.Add((button, button.onClick));
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => Host.OpenUnconfigured());
            }
            lobby = nav.GetComponentInChildren<PortraitEntryController>(true);
            if (lobby != null) { lobby.CompetitiveBrowsingAvailable = true; lobby.RefreshState(); }
        }
        private void OnDestroy()
        {
            foreach (var binding in bindings) if (binding.button != null) binding.button.onClick = binding.original;
            if (lobby != null) lobby.CompetitiveBrowsingAvailable = false;
        }
    }
}
