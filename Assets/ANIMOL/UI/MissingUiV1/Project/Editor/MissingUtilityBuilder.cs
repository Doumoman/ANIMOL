using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.MissingUiV1.Project.Editor
{
    public static class MissingUtilityBuilder
    {
        [MenuItem("ANIMOL/Missing UI V1/Build Project Task 2 Settings and Account")]
        public static void Build()
        {
            var root=MissingUiProjectBuilder.ResourcesPath;
            var common=AssetDatabase.LoadAssetAtPath<MissingUiArt>(root+"/Art.asset");
            if(common==null)throw new InvalidOperationException("Build Task 1 first. Existing common art must be reused.");
            var path=root+"/UtilityArt.asset";var art=AssetDatabase.LoadAssetAtPath<MissingUtilityArt>(path);
            if(art==null){art=ScriptableObject.CreateInstance<MissingUtilityArt>();AssetDatabase.CreateAsset(art,path);}
            art.Common=common;
            art.Music=Sprite("settings/UI_Icon_Music");art.Sound=Sprite("settings/UI_Icon_Sound");art.Mute=Sprite("settings/UI_Icon_Mute");
            art.Vibration=Sprite("settings/UI_Icon_Vibration");art.Controls=Sprite("settings/UI_Icon_TouchControls");art.TextSize=Sprite("settings/UI_Icon_TextSize");
            art.Language=Sprite("settings/UI_Icon_Language");art.Account=Sprite("settings/UI_Icon_Account");art.LocalSave=Sprite("settings/UI_Icon_LocalSave");
            art.HandLeft=Sprite("settings/UI_Icon_HandLeft");art.HandRight=Sprite("settings/UI_Icon_HandRight");art.Opacity=Sprite("settings/UI_Icon_Opacity");art.Device=Sprite("settings/UI_Control_LayoutPreview");
            art.SliderRail=Sprite("common/UI_Common_SliderRail");art.SliderFill=Sprite("common/UI_Common_SliderFill");art.SliderThumb=Sprite("common/UI_Common_SliderThumb");
            art.ToggleOff=Sprite("common/UI_Common_ToggleTrack_Off");art.ToggleOn=Sprite("common/UI_Common_ToggleTrack_On");art.ToggleKnob=Sprite("common/UI_Common_ToggleKnob");
            var hud=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/SC15_GameplayHud.prefab");
            if(hud==null)throw new InvalidOperationException("Existing HUD is required.");
            var names=new[]{"SwipeMoveRegion","JumpActionButton","SpecialActionButton","AbilityActionButton","AnimalChangeButton","DropActionButton"};
            art.HudSources=names.Select(n=>hud.GetComponentsInChildren<Image>(true).Single(i=>i.name==n)).ToArray();
            EditorUtility.SetDirty(art);AssetDatabase.SaveAssetIfDirty(art);
            var preview=EditorSceneManager.NewPreviewScene();var container=new GameObject("Task2BuildRoot");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(container,preview);
            try
            {
                foreach(MissingUtilityScreen kind in Enum.GetValues(typeof(MissingUtilityScreen)))
                {
                    var view=MissingUtilityView.Build(container.transform,art,kind);
                    PrefabUtility.SaveAsPrefabAsset(view.gameObject,root+"/"+kind+".prefab");UnityEngine.Object.DestroyImmediate(view.gameObject);
                }
                var modal=MissingControlReset.Build(container.transform,art);PrefabUtility.SaveAsPrefabAsset(modal.gameObject,root+"/ControlReset.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(container);EditorSceneManager.ClosePreviewScene(preview);}
            Debug.Log("MISSING_UI_TASK2: Settings, Controls, Account and reset confirmation generated. Existing sources/scenes/store/phase assets were not regenerated.");
        }
        private static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(MissingUiProjectBuilder.Root+"/Sprites/"+name+".png")??throw new InvalidOperationException("Missing art "+name);
    }
}
