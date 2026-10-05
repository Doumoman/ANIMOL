using System;
using System.Collections.Generic;
using System.Linq;
using ANIMOL.Development;
using ANIMOL.Gameplay;
using Animol.TerrainStructure;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public sealed partial class TerrainEditorAdapter
    {
        private readonly Dictionary<string,TerrainEditorThumbnail> thumbnails=new Dictionary<string,TerrainEditorThumbnail>();
        public int ThumbnailBuildCount { get; private set; }
        public TerrainEditorThumbnail Thumbnail(TerrainEditorPart part)
        {
            if(part.marker.HasValue)return new TerrainEditorThumbnail{diagnostic="기존 "+part.name+" marker에는 Sprite 미리보기 리소스가 없습니다.",sourceHash=part.id};
            var asset=part.terrain!=null?(UnityEngine.Object)Terrain.Frame(part.terrain.frameId):part.obj;
            var hash=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(asset)).ToString();
            var key=part.id+":"+hash;
            if(thumbnails.TryGetValue(key,out var cached))return cached;
            var result=new TerrainEditorThumbnail {sourceHash=hash};
            var preview=new PreviewRenderUtility();
            try
            {
                var parent=new GameObject("Safe silhouette render · no scripts");
                GameObject art;
                if(part.terrain!=null)
                    art=StageTerrainStructureRuntime.CreateArt(parent.transform,new AnimolTerrainPlacement {instanceId=part.id,catalogId=part.id},part.terrain,Terrain.Frame(part.terrain.frameId),1,0);
                else
                {
                    art=TerrainEditorArt.CopySprites(part.obj.Prefab,parent.transform);
                    if(art.GetComponentsInChildren<SpriteRenderer>().Length==0 && (part.obj.GhostSprite!=null || part.obj.Icon!=null))
                    { var renderer=art.AddComponent<SpriteRenderer>();renderer.sprite=part.obj.GhostSprite!=null?part.obj.GhostSprite:part.obj.Icon; }
                }
                TerrainEditorArt.SetLayer(parent);
                preview.AddSingleGO(parent);
                var renderers=parent.GetComponentsInChildren<SpriteRenderer>();
                if(renderers.Length==0)throw new InvalidOperationException("미리보기 Sprite 없음: "+part.id);
                var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                preview.camera.orthographic=true;preview.camera.orthographicSize=Mathf.Max(bounds.extents.x,bounds.extents.y)*1.1f;
                preview.camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-50);
                preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=100;
                preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=Color.clear;
                preview.camera.cullingMask=1<<TerrainEditorArt.PreviewLayer;
                // EndStaticPreview flattens alpha in this Unity version. Read an explicit RGBA
                // render target instead; the source shader's cutoff/exclusion remains authoritative.
                var rt=RenderTexture.GetTemporary(192,192,24,RenderTextureFormat.ARGB32);
                var previous=RenderTexture.active;
                try
                {
                    preview.camera.targetTexture=rt;preview.camera.aspect=1;preview.camera.Render();RenderTexture.active=rt;
                    result.color=new Texture2D(192,192,TextureFormat.RGBA32,false);
                    result.color.ReadPixels(new Rect(0,0,192,192),0,0);result.color.Apply();
                }
                finally {preview.camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
                result.color.name=part.id+" color";result.color.filterMode=FilterMode.Point;
                var pixels=result.color.GetPixels32();var ink=(Color32)TerrainEditorScreen.Paper;int opaque=0;
                for(int i=0;i<pixels.Length;i++) { if(pixels[i].a>8){pixels[i]=ink;opaque++;}else pixels[i]=new Color32(0,0,0,0); }
                if(opaque==0)throw new InvalidOperationException("투명 미리보기: "+part.id);
                result.silhouette=new Texture2D(192,192,TextureFormat.RGBA32,false){name=part.id+" silhouette",filterMode=FilterMode.Point};
                result.silhouette.SetPixels32(pixels);result.silhouette.Apply();ThumbnailBuildCount++;
            }
            catch(Exception ex){result.diagnostic=ex.Message;}
            finally{preview.Cleanup();}
            thumbnails.Add(key,result);return result;
        }
        private void DisposeThumbnails()
        {
            foreach(var value in thumbnails.Values)
            {if(value.color!=null)UnityEngine.Object.DestroyImmediate(value.color);if(value.silhouette!=null)UnityEngine.Object.DestroyImmediate(value.silhouette);}
            thumbnails.Clear();
        }
    }
}
