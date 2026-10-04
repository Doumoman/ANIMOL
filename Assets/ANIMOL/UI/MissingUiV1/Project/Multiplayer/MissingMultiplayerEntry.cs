using System.Collections;
using System.Collections.Generic;
using ANIMOL.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.MissingUiV1.Project.Multiplayer
{
    [DefaultExecutionOrder(1102)]
    public sealed class MissingMultiplayerEntry:MonoBehaviour
    {
        public const string ResourceRoot="ANIMOLMissingUiV1/Multiplayer/";
        private UiNavigationService navigation;
        private MissingMultiplayerView room;
        private readonly List<(GameObject child,bool active)> originalRoom=new List<(GameObject,bool)>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHooks()=>SceneManager.sceneLoaded-=Install;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register(){SceneManager.sceneLoaded-=Install;SceneManager.sceneLoaded+=Install;}
        public static void Install(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="Lobby")return;
            foreach(var root in scene.GetRootGameObjects())
            foreach(var nav in root.GetComponentsInChildren<UiNavigationService>(true))
                if(nav.GetComponent<MissingMultiplayerEntry>()==null&&nav.GetComponent<MultiplayerUiPresenter>()!=null)nav.gameObject.AddComponent<MissingMultiplayerEntry>();
        }
        private IEnumerator Start()
        {
            yield return null;yield return null;yield return null;
            navigation=GetComponent<UiNavigationService>();var host=transform.Find("SafeArea/ScreenHost");if(host==null)yield break;
            foreach(MissingMultiplayerScreen kind in System.Enum.GetValues(typeof(MissingMultiplayerScreen)))
            {
                var prefab=Resources.Load<GameObject>(ResourceRoot+kind);if(prefab==null)continue;
                var screen=host.Find(MissingMultiplayerView.ScreenId(kind));
                if(screen!=null)
                {
                    if(screen.GetComponentInChildren<MissingMultiplayerView>(true)!=null)continue;
                    foreach(Transform child in screen){if(kind==MissingMultiplayerScreen.Room)originalRoom.Add((child.gameObject,child.gameObject.activeSelf));child.gameObject.SetActive(false);}
                    var instance=Instantiate(prefab,screen,false);instance.name="MissingMultiplayer"+kind;instance.SetActive(true);
                    if(kind==MissingMultiplayerScreen.Room)room=instance.GetComponent<MissingMultiplayerView>();
                }
                else
                {
                    var instance=Instantiate(prefab,host,false);instance.name=MissingMultiplayerView.ScreenId(kind);instance.SetActive(false);
                }
            }
            navigation.RebuildIndex();navigation.ScreenChanged+=OnScreenChanged;OnScreenChanged(navigation.CurrentScreenId);
        }
        private void OnScreenChanged(string id)
        {
            if(id!="SC06_MatchRoom"||room==null)return;
            // Existing SC06 also hosts isolated development previews, including coop. Preserve that
            // route and its own policy; never present its generated participants as operational data.
            bool development=GetComponent<MultiplayerUiPresenter>().PreviewParticipantCount>0;
            foreach(var item in originalRoom)if(item.child!=null)item.child.SetActive(development&&item.active);
            room.gameObject.SetActive(!development);
        }
        private void OnDestroy(){if(navigation!=null)navigation.ScreenChanged-=OnScreenChanged;}
    }
}
