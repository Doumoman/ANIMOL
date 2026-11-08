#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using ANIMOL.UI.Crayon;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using William.NotebookUI;

namespace ANIMOL.Tests
{
    public sealed class BattlePreviewIsolationPlayTests
    {
        const string Main="Assets/ANIMOL/Scenes/Main/NotebookUI.unity";
        const string Battle="Assets/ANIMOL/Scenes/Battle/ANIMOL_StarRain_Battle.unity";
        const string Wallet="WilliamNotebookUI.Wallet",Prefs="WilliamNotebookUI.Preferences";
        bool hadWallet,hadPrefs;string wallet,prefs;
        [SetUp] public void Preserve()
        {hadWallet=PlayerPrefs.HasKey(Wallet);wallet=PlayerPrefs.GetString(Wallet);hadPrefs=PlayerPrefs.HasKey(Prefs);prefs=PlayerPrefs.GetString(Prefs);}
        [UnityTearDown] public IEnumerator Restore()
        {
            var root=Object.FindFirstObjectByType<NotebookRoot>(FindObjectsInactive.Include);
            if(root)Object.Destroy(root.GetComponentInParent<Canvas>(true).rootCanvas.gameObject);
            yield return null;
            if(hadWallet)PlayerPrefs.SetString(Wallet,wallet);else PlayerPrefs.DeleteKey(Wallet);
            if(hadPrefs)PlayerPrefs.SetString(Prefs,prefs);else PlayerPrefs.DeleteKey(Prefs);PlayerPrefs.Save();
        }
        [UnityTest] public IEnumerator PreviewThenMainCannotAppearAfterStageEntry()
        {yield return Mixed(Battle,Main);}
        [UnityTest] public IEnumerator MainThenPreviewCannotAppearAfterStageEntry()
        {yield return Mixed(Main,Battle);}
        static IEnumerator Mixed(string first,string second)
        {
            EditorSceneManager.LoadSceneInPlayMode(first,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var operation=EditorSceneManager.LoadSceneAsyncInPlayMode(second,new LoadSceneParameters(LoadSceneMode.Additive));
            while(!operation.isDone)yield return null;yield return null;
            var preview=SceneManager.GetSceneByPath(Battle);var main=SceneManager.GetSceneByPath(Main);
            Assert.That(preview.GetRootGameObjects().All(g=>!g.activeInHierarchy),Is.True,"A preview canvas/camera/input root leaked into the game");
            Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(main));
            var root=Object.FindFirstObjectByType<NotebookRoot>();Assert.That(root,Is.Not.Null);
            root.SetReducedMotion(true);root.OpenStages();root.nodes.First(n=>n.stageId=="SW01").button.onClick.Invoke();
            var gateway=(NotebookProjectGateway)root.gateway;bool approved=false;
            gateway.RequestStageEntry("SW01",1,"preview-isolation",value=>approved=value);Assert.That(approved,Is.True);
            gateway.OpenAcceptedStage("SW01","preview-isolation");
            NotebookMapDataPreviewBridge bridge=null;
            for(int i=0;i<60;i++)
            {yield return null;bridge=Object.FindFirstObjectByType<NotebookMapDataPreviewBridge>();if(bridge&&bridge.Ready)break;}
            Assert.That(bridge&&bridge.Ready,Is.True);Assert.That(bridge.sourceMap.name,Is.EqualTo("SW00_MapData"));
            Assert.That(bridge.StageId,Is.EqualTo("SW01"));Assert.That(bridge.gameObject.scene,Is.EqualTo(main));
            Assert.That(Object.FindFirstObjectByType<NotebookGameplayHud>(),Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<BattleCrayonArtPreview>(),Is.Null);
            Assert.That(preview.GetRootGameObjects().All(g=>!g.activeInHierarchy),Is.True);
        }
        [UnityTest] public IEnumerator StandalonePreviewStillWorksAfterLeavingMain()
        {
            EditorSceneManager.LoadSceneInPlayMode(Main,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            EditorSceneManager.LoadSceneInPlayMode(Battle,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
            var preview=Object.FindFirstObjectByType<BattleCrayonArtPreview>();Assert.That(preview,Is.Not.Null);
            Assert.That(preview.CurrentScreen,Is.EqualTo(BattleCrayonArtPreview.Screen.Start));
            Assert.That(preview.startButton.IsInteractable(),Is.True);
        }
    }
}
#endif
