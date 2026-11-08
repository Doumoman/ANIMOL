using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Animol.MapHudCrayonUI;
using Animol.MapHudCrayonUI.Editor;
using Animol.CrayonNumerals.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using William.NotebookUI;
using Object = UnityEngine.Object;

public static class MapHudCrayonApply
{
    const string Main = "Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab";
    const string Gameplay = "Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab";
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static string Hash(string text) { using(var sha=SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text))); }
    static string Trees(params GameObject[] roots) => Hash(string.Join("\n", roots.Where(r=>r).SelectMany(r=>r.GetComponentsInChildren<Component>(true))
        .Where(c=>c).Select(c=>c.GetInstanceID()+":"+EditorJsonUtility.ToJson(c))));
    static string Geometry(RectTransform t) => t.anchorMin.ToString("R")+t.anchorMax.ToString("R")+t.pivot.ToString("R")+
        t.anchoredPosition.ToString("R")+t.sizeDelta.ToString("R")+t.localScale.ToString("R")+t.localRotation.ToString("R");
    static Dictionary<RectTransform,string> Rects(GameObject root) => root.GetComponentsInChildren<RectTransform>(true)
        .Where(t=>t.name!="Prepainted Crayon Decoration"&&t.name!="Prepainted Crayon Pickup Decoration").ToDictionary(t=>t,Geometry);
    static string Buttons(GameObject root) => Hash(string.Join("\n",root.GetComponentsInChildren<Button>(true).Select(EditorJsonUtility.ToJson)));
    static void Save(PrefabStage stage)
    {
        typeof(PrefabStage).GetMethod("SavePrefab",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(stage,null);
        Require(!stage.scene.isDirty,"Prefab remains dirty after explicit save");
    }
    public static object Map()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Exit Play Mode");
        var stage=PrefabStageUtility.OpenPrefab(Main);
        typeof(PrefabStage).GetProperty("autoSave",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(stage,false);
        var root=stage.prefabContentsRoot.GetComponentInChildren<NotebookRoot>(true);
        Selection.objects=new Object[]{root.gameObject};
        var owner=EditorJsonUtility.ToJson(root);
        Func<string> protectedNow=()=>Trees(root.mainScreen,root.settingsScreen,root.modal);
        var before=protectedNow();var rects=Rects(root.gameObject);var buttons=Buttons(root.gameObject);
        int initialCount=root.stageScreen.GetComponentsInChildren<CrayonImageBinding>(true).Length;
        CrayonMapHudInstaller.ApplyToSelectedMap();
        var count=root.stageScreen.GetComponentsInChildren<CrayonImageBinding>(true).Length;
        Require(count>0,"No actual map bindings");
        Undo.PerformUndo();
        Require(root.stageScreen.GetComponentsInChildren<CrayonImageBinding>(true).Length==initialCount,"Map decoration Undo failed");
        Undo.PerformRedo();
        Require(root.stageScreen.GetComponentsInChildren<CrayonImageBinding>(true).Length==count,"Map decoration Redo failed");
        var labels=root.stageScreen.GetComponentsInChildren<TMP_Text>(true);
        var oldFonts=labels.Select(t=>t.font).ToArray();
        CrayonNumeralsInstaller.ApplyToSelectedMapNumbers();
        int numeric=labels.Count(t=>AssetDatabase.GetAssetPath(t.font).StartsWith("Assets/ANIMOL/CrayonNumerals/Generated/"));
        Require(numeric>0,"Map font was not connected");
        Undo.PerformUndo();
        Require(labels.Select(t=>t.font).SequenceEqual(oldFonts),"Numeric font Undo failed");
        Undo.PerformRedo();
        Require(labels.Count(t=>AssetDatabase.GetAssetPath(t.font).StartsWith("Assets/ANIMOL/CrayonNumerals/Generated/"))==numeric,"Numeric font Redo failed");
        CrayonMapHudInstaller.ApplyToSelectedMap(); CrayonNumeralsInstaller.ApplyToSelectedMapNumbers();
        Require(root.stageScreen.GetComponentsInChildren<CrayonImageBinding>(true).Length==count,"Reapplication duplicated map bindings");
        Require(owner==EditorJsonUtility.ToJson(root),"NotebookRoot references changed");
        // New decoration children are omitted from the comparison of pre-existing rectangles.
        Require(before==protectedNow(),"Protected map sibling changed");
        Require(rects.All(pair=>pair.Key&&Geometry(pair.Key)==pair.Value),"A pre-existing map rectangle changed");
        Require(buttons==Buttons(root.gameObject),"Map button connections changed");
        foreach(var binding in root.stageScreen.GetComponentsInChildren<CrayonImageBinding>(true))
        {
            Require(binding.sourceImage&&binding.decoration&&!binding.decoration.raycastTarget,"Incomplete map binding");
            Require(binding.decoration.transform.parent==binding.sourceImage.transform,"Wrong decoration parent");
            Require(binding.sourceImage.transform.Cast<Transform>().Count(t=>t.name=="Prepainted Crayon Decoration")==1,"Duplicate map decoration survived");
        }
        Save(stage);
        var result=new {prefab=Main,bindings=count,numericLabels=numeric,protectedHierarchyHash=before,undoRedo=true,idempotent=true};
        UnityEditor.SceneManagement.StageUtility.GoToMainStage(); return result;
    }
    public static object Hud()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Exit Play Mode");
        var stage=PrefabStageUtility.OpenPrefab(Gameplay);
        typeof(PrefabStage).GetProperty("autoSave",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(stage,false);
        var hud=stage.prefabContentsRoot.GetComponentInChildren<NotebookGameplayHud>(true);
        Selection.objects=new Object[]{hud.gameObject};
        var owner=EditorJsonUtility.ToJson(hud);var before=Trees(hud.pauseOverlay,hud.clearOverlay,hud.modalBackdrop);
        var buttons=Buttons(hud.gameObject);var number=hud.costNumber;var value=number.Value;
        var rects=Rects(hud.gameObject);
        var area=number.area;var places=number.places.ToArray();var height=number.maximumDigitHeight;var gap=number.gap;
        var geometry=places.Select(p=>EditorJsonUtility.ToJson(p.rectTransform)).ToArray();
        var oldDigits=number.digits.ToArray();var indices=places.Select(p=>Array.IndexOf(oldDigits,p.sprite)).ToArray();
        int initialCount=hud.GetComponentsInChildren<CrayonImageBinding>(true).Length;
        CrayonMapHudInstaller.ApplyToSelectedGameplayHud();
        int count=hud.GetComponentsInChildren<CrayonImageBinding>(true).Length;
        Require(count>0,"No actual HUD bindings");
        Undo.PerformUndo();Require(hud.GetComponentsInChildren<CrayonImageBinding>(true).Length==initialCount,"HUD skin Undo failed");
        Undo.PerformRedo();Require(hud.GetComponentsInChildren<CrayonImageBinding>(true).Length==count,"HUD skin Redo failed");
        CrayonNumeralsInstaller.ApplyToSelectedGameplayHud();
        Require(number.Value==value&&number.area==area&&number.places.SequenceEqual(places)&&number.maximumDigitHeight==height&&number.gap==gap,"Numeric data/layout changed");
        for(int i=0;i<places.Length;i++)
        {
            Require(geometry[i]==EditorJsonUtility.ToJson(places[i].rectTransform),"A digit place moved");
            if(indices[i]>=0) Require(places[i].sprite==number.digits[indices[i]],"Displayed digit changed");
        }
        Undo.PerformUndo();Require(number.digits.SequenceEqual(oldDigits),"HUD digit Undo failed");
        Undo.PerformRedo();
        CrayonMapHudInstaller.ApplyToSelectedGameplayHud(); CrayonNumeralsInstaller.ApplyToSelectedGameplayHud();
        Require(hud.GetComponentsInChildren<CrayonImageBinding>(true).Length==count,"HUD reapplication duplicated bindings");
        Require(owner==EditorJsonUtility.ToJson(hud),"HUD game/service references changed");
        Require(before==Trees(hud.pauseOverlay,hud.clearOverlay,hud.modalBackdrop),"HUD popup changed");
        Require(buttons==Buttons(hud.gameObject),"HUD button connections changed");
        Require(rects.All(pair=>pair.Key&&Geometry(pair.Key)==pair.Value),"A pre-existing HUD rectangle changed");
        Require(number.Value==value,"HUD Value reset");
        foreach(var binding in hud.GetComponentsInChildren<CrayonImageBinding>(true))
            Require(binding.sourceImage.transform.Cast<Transform>().Count(t=>t.name=="Prepainted Crayon Decoration")==1,"Duplicate HUD decoration survived");
        Save(stage);
        var result=new {prefab=Gameplay,bindings=count,valuePreserved=value,displayedDigitIndices=indices,protectedHierarchyHash=before,undoRedo=true,idempotent=true};
        UnityEditor.SceneManagement.StageUtility.GoToMainStage();return result;
    }
}
