using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.UI;
public static class RestoreMapHudGeometry
{
    [Serializable] public class Data { public Entry[] entries; public Field[] serializedFields; }
    [Serializable] public class Entry { public string asset,path,fileId; public Vector2 anchorMin,anchorMax,size,pivot,anchored; public Vector3 position,scale; public Quaternion rotation; }
    [Serializable] public class Field { public string asset,path,component,field; public double value; }
    public static object Run()
    {
        var data=JsonConvert.DeserializeObject<Data>(File.ReadAllText("Temp/maphud-geometry-restore.json"));
        foreach(var asset in data.entries.Select(e=>e.asset).Concat(data.serializedFields.Select(f=>f.asset)).Distinct())
        {
            var stage=PrefabStageUtility.OpenPrefab(asset);
            typeof(PrefabStage).GetProperty("autoSave",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(stage,false);
            foreach(var entry in data.entries.Where(e=>e.asset==asset).OrderBy(e=>e.path.Split('/').Length))
            {
                var target=(entry.path.Length==0?stage.prefabContentsRoot.transform:stage.prefabContentsRoot.transform.Find(entry.path)) as RectTransform;
                if(!target) throw new InvalidOperationException("Missing original rectangle: "+entry.path);
                target.anchorMin=entry.anchorMin;target.anchorMax=entry.anchorMax;target.pivot=entry.pivot;
                target.localRotation=entry.rotation;target.localScale=entry.scale;target.localPosition=entry.position;
                // RectTransform serializes anchoredPosition independently of the inherited
                // Transform position. Apply the authored anchors last so x/y stay authoritative.
                target.sizeDelta=entry.size;target.anchoredPosition=entry.anchored;
                EditorUtility.SetDirty(target);
            }
            foreach(var entry in data.serializedFields.Where(e=>e.asset==asset))
            {
                var target=entry.path.Length==0?stage.prefabContentsRoot.transform:stage.prefabContentsRoot.transform.Find(entry.path);
                Component component=entry.component=="CanvasScaler"?(Component)target.GetComponent<CanvasScaler>():target.GetComponent<TMP_Text>();
                var serialized=new UnityEditor.SerializedObject(component);var property=serialized.FindProperty(entry.field);
                if(property.propertyType==UnityEditor.SerializedPropertyType.Float)property.floatValue=(float)entry.value;
                else property.intValue=(int)entry.value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            typeof(PrefabStage).GetMethod("SavePrefab",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(stage,null);
            StageUtility.GoToMainStage();
        }
        return new {restoredOriginalSerializedRectangles=data.entries.Length,restoredEditorFields=data.serializedFields.Length};
    }
}
