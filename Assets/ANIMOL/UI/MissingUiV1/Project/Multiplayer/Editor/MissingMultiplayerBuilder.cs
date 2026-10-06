using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.MissingUiV1.Project.Multiplayer.Editor
{
    public static class MissingMultiplayerBuilder
    {
        public const string Root="Assets/ANIMOL/UI/MissingUiV1/Project/Resources/ANIMOLMissingUiV1/Multiplayer";
        public static void Build()
        {
            var utility=AssetDatabase.LoadAssetAtPath<MissingUtilityArt>("Assets/ANIMOL/UI/MissingUiV1/Project/Resources/ANIMOLMissingUiV1/UtilityArt.asset");
            if(utility==null)throw new InvalidOperationException("Task 2 utility art is required.");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var path=Root+"/MultiplayerArt.asset";var art=AssetDatabase.LoadAssetAtPath<MissingMultiplayerArt>(path);
            if(art==null){art=ScriptableObject.CreateInstance<MissingMultiplayerArt>();AssetDatabase.CreateAsset(art,path);}
            art.Utility=utility;art.Casual=Sprite("UI_Multi_Art_Casual");art.Ranked=Sprite("UI_Multi_Art_Ranked");art.Private=Sprite("UI_Multi_Art_Private");
            art.ModeFrame=Sprite("UI_Multi_ModeCardFrame");art.PlayerFrame=Sprite("UI_Multi_PlayerCardFrame");art.Avatar=Sprite("UI_Multi_AvatarEmpty");
            art.Enter=Sprite("UI_Icon_EnterRoom");art.Paste=Sprite("UI_Icon_PasteCode");art.Invite=Sprite("UI_Icon_Invite");art.Copy=Sprite("UI_Icon_CopyCode");
            EditorUtility.SetDirty(art);AssetDatabase.SaveAssetIfDirty(art);
            var scene=EditorSceneManager.NewPreviewScene();var container=new GameObject("MissingMultiplayerBuildRoot");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(container,scene);
            try
            {
                foreach(MissingMultiplayerScreen kind in Enum.GetValues(typeof(MissingMultiplayerScreen)))
                {
                    var view=MissingMultiplayerView.Build(container.transform,art,kind);
                    // UiButtonFeedback is a secondary class in UiPolishRuntime.cs, so Unity cannot
                    // serialize its MonoScript reference. Attach that existing component at runtime.
                    foreach(var feedback in view.GetComponentsInChildren<ANIMOL.UI.UiButtonFeedback>(true))UnityEngine.Object.DestroyImmediate(feedback);
                    PrefabUtility.SaveAsPrefabAsset(view.gameObject,Root+"/"+kind+".prefab");UnityEngine.Object.DestroyImmediate(view.gameObject);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(container);EditorSceneManager.ClosePreviewScene(scene);}
            Debug.Log("MISSING_UI_TASK3: Five operational UI frames generated; no original scene, phase adapter or policy regenerated.");
        }
        private static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ANIMOL/UI/MissingUiV1/Sprites/multiplayer/"+name+".png")??throw new InvalidOperationException(name);
    }
}
