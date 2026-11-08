using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Animol.MapHudCrayonUI;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using William.NotebookUI;

public static class MapHudCrayonFinalAudit
{
    static void Need(bool yes,string message) {if(!yes)throw new InvalidOperationException(message);}
    static string PathOf(Transform transform) => transform.parent ? PathOf(transform.parent)+"/"+transform.name : transform.name;
    public static object Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Exit Play Mode first");
        const string main="Assets/ANIMOL/Prefabs/UI/Main/NotebookCanvas.prefab";
        const string game="Assets/ANIMOL/Prefabs/UI/Gameplay/GameplayCanvas.prefab";
        var canvas=AssetDatabase.LoadAssetAtPath<GameObject>(main);var gameplay=AssetDatabase.LoadAssetAtPath<GameObject>(game);
        var root=canvas.GetComponentInChildren<NotebookRoot>(true);var hud=gameplay.GetComponentInChildren<NotebookGameplayHud>(true);
        Need(AssetDatabase.GetDependencies("Assets/ANIMOL/Scenes/Main/NotebookUI.unity").Contains(main),"Main scene does not use the modified prefab");
        Need(AssetDatabase.GetAssetPath(((NotebookProjectGateway)root.gateway).gameplayCanvasPrefab)==game,"Runtime entry uses a different HUD");
        var mapping=new List<object>();
        foreach(var item in new[]{canvas,gameplay})
        {
            foreach(var transform in item.GetComponentsInChildren<Transform>(true))
                Need(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing script: "+PathOf(transform));
            foreach(var binding in item.GetComponentsInChildren<CrayonImageBinding>(true))
            {
                Need(binding.sourceImage&&binding.decoration&&binding.scope&&!binding.decoration.raycastTarget,"Missing skin reference");
                Need(binding.sourceImage.transform.Cast<Transform>().Count(t=>t.name=="Prepainted Crayon Decoration")==1,"Duplicate decoration");
                foreach(var state in binding.states)
                    Need(state.source&&state.frameA&&state.frameB&&state.frameC&&state.animatedMaterial&&state.stillMaterial,"Missing A/B/C/state reference");
                mapping.Add(new{prefab=AssetDatabase.GetAssetPath(item),source=PathOf(binding.sourceImage.transform),
                    decoration=PathOf(binding.decoration.transform),frameA=AssetDatabase.GetAssetPath(binding.decoration.sprite),
                    material=AssetDatabase.GetAssetPath(binding.decoration.material),states=binding.states.Length});
            }
        }
        foreach(var panel in new[]{root.mainScreen,root.settingsScreen,root.modal,hud.pauseOverlay,hud.clearOverlay,hud.modalBackdrop})
            Need(panel.GetComponentsInChildren<CrayonImageBinding>(true).Length==0,"Protected screen was skinned");
        Need(hud.costNumber.digits.Length==10,"Missing numeric glyph");
        foreach(var digit in hud.costNumber.digits)
        {
            Need(digit&&digit.rect.size==new Vector2(220,280),"Invalid digit dimensions");
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(digit));
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            Need(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&settings.spriteMeshType==SpriteMeshType.FullRect
                &&importer.spritePixelsPerUnit==100&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed
                &&importer.filterMode==FilterMode.Bilinear&&importer.wrapMode==TextureWrapMode.Clamp,"Wrong numeral import settings");
        }
        var texts=root.stageScreen.GetComponentsInChildren<TMP_Text>(true).Where(t=>AssetDatabase.GetAssetPath(t.font).StartsWith("Assets/ANIMOL/CrayonNumerals/Generated/")).ToArray();
        File.WriteAllText("Docs/Validation/MapHudCrayon/ObjectMapping.json",JsonConvert.SerializeObject(new{images=mapping,numericLabels=texts.Select(t=>PathOf(t.transform)).ToArray()},Formatting.Indented));
        return new{mapBindings=root.stageScreen.GetComponentsInChildren<CrayonImageBinding>(true).Length,hudBindings=hud.GetComponentsInChildren<CrayonImageBinding>(true).Length,
            numericLabels=texts.Length,digits=hud.costNumber.digits.Length,missingSkinReferences=0,missingScripts=0,protectedScreens=6};
    }
}
