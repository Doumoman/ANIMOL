using System.Collections;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.MissingUiV1.Project
{
    [DefaultExecutionOrder(1101)]
    public sealed class MissingUtilityEntry:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHooks()=>SceneManager.sceneLoaded-=Install;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register(){SceneManager.sceneLoaded-=Install;SceneManager.sceneLoaded+=Install;}
        public static void Install(Scene scene,LoadSceneMode mode)
        {
            foreach(var root in scene.GetRootGameObjects())
            foreach(var nav in root.GetComponentsInChildren<UiNavigationService>(true))
                if(nav.transform.Find("SafeArea/ScreenHost/SC13_Settings")!=null&&nav.GetComponent<MissingUtilityEntry>()==null)nav.gameObject.AddComponent<MissingUtilityEntry>();
        }
        private IEnumerator Start()
        {
            yield return null;yield return null;
            var modalHost=transform.Find("SafeArea/ModalHost");
            var reset=Resources.Load<GameObject>(MissingUiProjectEntry.ResourceRoot+"ControlReset");
            if(modalHost!=null&&reset!=null&&modalHost.Find(MissingControlReset.DialogName)==null)
            {
                var instance=Instantiate(reset,modalHost,false);instance.name=MissingControlReset.DialogName;instance.SetActive(false);
            }
            Add("SC13_Settings","Settings");Add("SC19_ControlSettings","Controls");Add("SC20_AccountAndSave","Account");
        }
        private void Add(string screenId,string resource)
        {
            var screen=transform.Find("SafeArea/ScreenHost/"+screenId);
            var prefab=Resources.Load<GameObject>(MissingUiProjectEntry.ResourceRoot+resource);
            if(screen==null||prefab==null||screen.GetComponentInChildren<MissingUtilityView>(true)!=null)return;
            foreach(Transform child in screen)child.gameObject.SetActive(false);
            var instance=Instantiate(prefab,screen,false);instance.name="Missing"+resource;instance.SetActive(true);
        }
    }
}
