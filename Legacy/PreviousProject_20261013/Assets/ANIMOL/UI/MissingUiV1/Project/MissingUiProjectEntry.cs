using System.Collections;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.MissingUiV1.Project
{
    // Matches the existing Phase1-3 installation pipeline: survives rebuilding legacy scenes/prefabs.
    [DefaultExecutionOrder(1100)]
    public sealed class MissingUiProjectEntry : MonoBehaviour
    {
        public const string ResourceRoot="ANIMOLMissingUiV1/";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHooks() => SceneManager.sceneLoaded-=Install;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() { SceneManager.sceneLoaded-=Install;SceneManager.sceneLoaded+=Install; }
        public static void Install(Scene scene,LoadSceneMode mode)
        {
            foreach(var root in scene.GetRootGameObjects())
            foreach(var navigation in root.GetComponentsInChildren<UiNavigationService>(true))
                if(navigation.GetComponent<MissingUiProjectEntry>()==null)navigation.gameObject.AddComponent<MissingUiProjectEntry>();
        }
        private IEnumerator Start()
        {
            yield return null; yield return null;
            var art=Resources.Load<MissingUiArt>(ResourceRoot+"Art"); if(art==null)yield break;
            var screen=transform.Find("SafeArea/ScreenHost/SC11_Store");
            var prefab=Resources.Load<GameObject>(ResourceRoot+"Store");
            if(screen!=null && prefab!=null && screen.GetComponentInChildren<MissingStoreView>(true)==null)
            {
                foreach(Transform child in screen)child.gameObject.SetActive(false);
                var instance=Instantiate(prefab,screen,false);instance.name="MissingStore";instance.SetActive(true);
            }
            foreach(var name in new[]{"ProductDetailModal","ServiceErrorModal","RewardedAdModal","ConfirmExitModal","MultiplayerPauseModal"})
            {
                var modal=transform.Find("SafeArea/ModalHost/"+name);if(modal!=null)MissingCommonModalSkin.Apply(modal,art);
            }
        }
    }
}
