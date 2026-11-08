using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Animol.Starway;
using Animol.Starway.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WilliamParadox.Story;
using Object = UnityEngine.Object;

public static class StoryCrayonApply
{
    const string Ui="Assets/ANIMOL/Prefabs/UI/Story/StoryDialogueUI.prefab";
    const string Host="Assets/WilliamParadoxStoryV14/Prefabs/StoryPreviewCanvas.prefab";
    const string Scene="Assets/WilliamParadoxStoryV14/Scenes/RanchStoryPreview.unity";
    static void Need(bool yes,string message){if(!yes)throw new InvalidOperationException(message);}
    static string Geometry(RectTransform t)=>t.anchorMin.ToString("R")+t.anchorMax.ToString("R")+t.pivot.ToString("R")+t.anchoredPosition.ToString("R")+t.sizeDelta.ToString("R")+t.localScale.ToString("R")+t.localRotation.ToString("R");
    static string Buttons(GameObject root)=>string.Join("\n",root.GetComponentsInChildren<Button>(true).Select(EditorJsonUtility.ToJson));
    static string Owner(StoryDialogueView view){var json=JObject.Parse(EditorJsonUtility.ToJson(view));json.Remove("crayonSkin");return json.ToString();}
    static void External(GameObject host,StoryDialogueView view)
    {
        var lore=host.GetComponentInChildren<RanchOptionalLore>(true);
        if(!lore)return;
        var scope=view.GetComponent<StoryCrayonSkin>();Undo.RecordObject(scope,"Connect host story popup decorations");
        var background=lore.panel.transform.Find("Backdrop").GetComponent<Image>();
        var frames=new List<StoryCrayonFrame>{StoryCrayonSkinInstaller.ApplyExternal((RectTransform)lore.panel.transform,null,scope,true,background)};
        foreach(var button in new[]{lore.clockButton,lore.roomButton,lore.nextButton,lore.closeButton})
            frames.Add(StoryCrayonSkinInstaller.ApplyExternal((RectTransform)button.transform,button,scope,true));
        scope.SetExternalFrames(frames.ToArray());
        PrefabUtility.RecordPrefabInstancePropertyModifications(scope);EditorUtility.SetDirty(scope);
    }
    static object Apply(GameObject host)
    {
        var view=host.GetComponentsInChildren<StoryDialogueView>(true).Single();
        var rects=host.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name!="CrayonOutline"&&t.name!="CrayonQuietFill").ToDictionary(t=>t,Geometry);
        string owner=Owner(view),buttons=Buttons(host);
        var labels=host.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t=>new{label=t,font=t.font,material=t.fontSharedMaterial,text=t.text,size=t.fontSize,style=t.fontStyle}).ToArray();
        int initial=host.GetComponentsInChildren<StoryCrayonFrame>(true).Length;
        Selection.objects=new Object[]{view.gameObject};
        Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
        StoryCrayonSkinInstaller.ApplySelected();External(host,view);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);
        int count=host.GetComponentsInChildren<StoryCrayonFrame>(true).Length;
        Need(count>=4,"Missing story decoration");
        Undo.PerformUndo();Need(host.GetComponentsInChildren<StoryCrayonFrame>(true).Length==initial,"Story Undo changed the wrong scope");
        if(initial==0)Need(!host.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="CrayonOutline"||t.name=="CrayonQuietFill"),"Story Undo left children");
        Undo.PerformRedo();Check(host,count);
        StoryCrayonSkinInstaller.ApplySelected();External(host,view);Undo.FlushUndoRecordObjects();Check(host,count);
        Need(owner==Owner(view),"Existing dialogue data/event/host references changed");
        Need(buttons==Buttons(host),"Existing button input, SpriteSwap or event changed");
        Need(rects.All(p=>p.Key&&Geometry(p.Key)==p.Value),"Existing rectangle changed");
        Need(labels.All(t=>t.label.font==t.font&&t.label.fontSharedMaterial==t.material&&t.label.text==t.text&&t.label.fontSize==t.size&&t.label.fontStyle==t.style),"Existing TMP changed");
        Need(!view.GetComponentsInChildren<StoryNeonPanel>(true).Any(p=>p.enabled),"Legacy neon still enabled");
        return new{frames=count,external=count-4,originalRectangles=rects.Count,originalLabels=labels.Length,undoRedo=true,idempotent=true};
    }
    static void Check(GameObject host,int count)
    {
        var frames=host.GetComponentsInChildren<StoryCrayonFrame>(true);Need(frames.Length==count,"Duplicate or lost story frame");
        foreach(var frame in frames)
        {
            var outline=frame.transform.Find("CrayonOutline");var fill=frame.transform.Find("CrayonQuietFill");
            Need(outline&&fill,"Story frame lost its children");
            Need(frame.transform.Cast<Transform>().Count(t=>t.name=="CrayonOutline")==1,"Duplicate outline child");
            var image=outline.GetComponent<Image>();Need(image&&image.sprite&&image.material&&!image.raycastTarget&&!fill.GetComponent<Image>().raycastTarget,"Invalid story outline");
            var reference=new SerializedObject(frame).FindProperty("decoration").objectReferenceValue;
            Need(reference==image,"Redo lost the decoration reference");
        }
    }
    public static object Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Exit Play Mode first");
        var results=new List<object>();
        foreach(string path in new[]{Ui,Host})
        {
            var current=PrefabStageUtility.GetCurrentPrefabStage();
            var stage=current!=null&&current.assetPath==path?current:PrefabStageUtility.OpenPrefab(path);
            typeof(PrefabStage).GetProperty("autoSave",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(stage,false);
            var result=Apply(stage.prefabContentsRoot);
            typeof(PrefabStage).GetMethod("SavePrefab",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(stage,null);
            Need(!stage.scene.isDirty,"Prefab was not saved");
            results.Add(new{path,result});StageUtility.GoToMainStage();
        }
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Additive);
        try
        {
            var host=scene.GetRootGameObjects().Single(g=>g.GetComponent<StoryRunner>());
            var result=Apply(host);Need(EditorSceneManager.SaveScene(scene),"Scene was not saved");results.Add(new{path=Scene,result});
        }
        finally{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
        return results;
    }
}
