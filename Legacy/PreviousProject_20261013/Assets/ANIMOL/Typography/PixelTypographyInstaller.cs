using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Typography
{
    /// <summary>Applies to existing common UI prefab instances without rewriting dirty prefabs or presenter references.</summary>
    public sealed class PixelTypographyInstaller : MonoBehaviour
    {
        public PixelTypographyProfile Profile;
        private float nextScan;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { SceneManager.sceneLoaded -= ApplyScene; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install() { SceneManager.sceneLoaded -= ApplyScene; SceneManager.sceneLoaded += ApplyScene; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyCurrent() => ApplyScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        private static void ApplyScene(Scene scene, LoadSceneMode mode)
        {
            var prefab = Resources.Load<GameObject>("ANIMOLPixelTypography");
            if (prefab == null) return;
            var profile = prefab.GetComponent<PixelTypographyInstaller>().Profile;
            if (profile == null || (!profile.CommonUiApproved && !profile.PilotScenes.Contains(scene.name))) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                    if (canvas.transform.Find("SafeArea") != null && canvas.GetComponentInChildren<PixelTypographyInstaller>(true) == null)
                        Instantiate(prefab, canvas.transform, false).GetComponent<PixelTypographyInstaller>().Scan();
        }
        private void Update() { if (Time.unscaledTime >= nextScan) { nextScan = Time.unscaledTime + .25f; Scan(); } }
        public void Scan()
        {
            if (Profile == null) return;
            var canvas = GetComponentInParent<Canvas>(); if (canvas == null) return;
            foreach (var text in canvas.GetComponentsInChildren<Text>(true))
            {
                // InputField owns its editable text and caret; leave it to a deliberate TMP_InputField migration.
                if (text.GetComponentInParent<InputField>() != null) continue;
                var bridge = text.GetComponent<PixelTextBridge>() ?? text.gameObject.AddComponent<PixelTextBridge>();
                bridge.Profile = Profile;
            }
            Profile.EnsurePointSampling();
        }
    }
}
