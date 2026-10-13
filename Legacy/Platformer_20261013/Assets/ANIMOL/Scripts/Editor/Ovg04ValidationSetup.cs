using System;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    // DEV fixture only: every marker/device placement uses the current Scene editor.
    // This is not a runtime loader, production generator, or a QA JSON import path.
    public static class Ovg04ValidationSetup
    {
        public const string MapPath = "Assets/ANIMOL/Data/Development/ObstacleGraphicsV1/DEV-OVG04-JOIN.asset";
        public const string Evidence = "Docs/Validation/OVG04";

        public static void Prepare()
        {
            Directory.CreateDirectory(Evidence);
            ObstacleArtV1Builder.Install();
            if (AssetDatabase.LoadAssetAtPath<StageMapDefinition>(MapPath) != null)
                throw new InvalidOperationException("OVG04 DEV fixture already exists; preserve its edits.");
            var map = ScriptableObject.CreateInstance<StageMapDefinition>();
            map.EditorInitializeIdentity("DEV-OVG04-JOIN", "T01");
            map.EditorInitializeBoundsFromAuthoredContent(1);
            map.EditorTrySetCellBounds(new RectInt(-4, -6, 32, 18), false, out _);
            for (int x = -4; x < 28; x++) if (x is not (16 or 17))
                map.FreeShapeTerrain.cells.Add(new Animol.TerrainStructure.FreeShapeCell { x=x, y=-1, styleId="T01_A" });
            map.EditorMarkCollisionDataSynchronized();
            AssetDatabase.CreateAsset(map, MapPath);
            TerrainEditorSceneEditor.Open(map);
            Place("marker:START", new Vector2Int(0, 0));
            Place("marker:BUBBLE", new Vector2Int(3, 0));
            Place("marker:CHECK", new Vector2Int(6, 0));
            Place("marker:BUBBLE", new Vector2Int(10, 0));
            Place("marker:BUBBLE", new Vector2Int(14, 0));
            Place("marker:EXIT", new Vector2Int(24, 0));
            Place("C02", new Vector2Int(21, 1));
            Place("C03", new Vector2Int(19, 0));
            Place("C05", new Vector2Int(12, 2));
            var validation=StageMapMarkerValidation.ValidateLocalPlay(map);
            if (!validation.IsValid) throw new InvalidOperationException(string.Join(" | ",validation.Errors));
            TerrainEditorSceneEditor.Adapter.Save();
            File.WriteAllText(Evidence+"/authored-map.json",EditorJsonUtility.ToJson(map,true));
            CaptureEditor(map,Evidence+"/scene-authored.png");
            TerrainEditorSceneEditor.Close();
            Debug.Log("OVG04: v3 art installed; current Scene editor authored and validated DEV map.");
        }

        public static string Place(string part, Vector2Int cell)
        {
            TerrainEditorSceneEditor.SelectPart(part);
            TerrainEditorSceneEditor.UpdateCandidate(cell, false);
            if (!TerrainEditorSceneEditor.GhostValid) throw new InvalidOperationException(TerrainEditorSceneEditor.Status);
            TerrainEditorSceneEditor.Commit();
            return TerrainEditorSceneEditor.State.instanceId;
        }

        public static void CaptureSavedDevelopmentMap()
        {
            var map=AssetDatabase.LoadAssetAtPath<StageMapDefinition>(MapPath);
            TerrainEditorSceneEditor.Open(map);
            try{CaptureEditor(map,Evidence+"/scene-authored.png");}
            finally{TerrainEditorSceneEditor.Close();}
        }

        public static void CaptureEditor(StageMapDefinition map,string path)
        {
            var adapter = TerrainEditorSceneEditor.Adapter;
            var root = new GameObject("OVG04 evidence only");
            var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
            ANIMOL.Development.TerrainEditorArt.BuildMap(adapter,root.transform);
            // This offscreen evidence copy uses a normal Camera, rather than the
            // SceneView preview camera that reserves layer 31 for editor artwork.
            foreach(var child in root.GetComponentsInChildren<Transform>())child.gameObject.layer=0;
            var cameraObject=new GameObject("OVG04 evidence camera");cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=preview;camera.orthographic=true;
            camera.cullingMask=~0;
            camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(preview);
            camera.orthographicSize=10;camera.aspect=16f/9f;camera.transform.position=new Vector3(11,3,-20);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.12f,.2f);
            Capture(camera,path,1280,720);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
        }

        public static void Capture(Camera camera,string path,int width=288,int height=512)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var prior=camera.targetTexture;var active=RenderTexture.active;
            var target=new RenderTexture(width,height,24);var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally { camera.targetTexture=prior;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
