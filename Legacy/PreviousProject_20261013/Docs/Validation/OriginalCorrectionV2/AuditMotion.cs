var cycle=UnityEngine.Object.FindFirstObjectByType<ANIMOL.FiveThemeMenu.MenuThemeCycle>();
if(!UnityEngine.Application.isPlaying||cycle==null)throw new System.Exception("Open the real Lobby in Play first");
var rows=new System.Collections.Generic.List<object>();
var old=UnityEngine.RenderTexture.active;var read=new UnityEngine.Texture2D(352,704,UnityEngine.TextureFormat.RGBA32,false);
var sheets=new System.Collections.Generic.Dictionary<int,UnityEngine.Texture2D>();
foreach(int direction in new[]{-1,1}){var tex=new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false);tex.LoadImage(System.IO.File.ReadAllBytes(UnityEditor.AssetDatabase.GetAssetPath(direction==1?cycle.Catalog.rabbitRight:cycle.Catalog.rabbitLeft)));sheets[direction]=tex;}
try
{
    foreach(int theme in Enumerable.Range(0,5))foreach(double p in new[]{.2,.5,.8})foreach(int cam in new[]{-1,1})foreach(int rabbit in new[]{-1,1})foreach(int frame in Enumerable.Range(0,8))
    {
        cycle.InspectionCameraDirection=cam;cycle.InspectionRabbitDirection=rabbit;cycle.InspectionFrame=frame;cycle.RenderAt(theme*5+p*5);
        UnityEngine.RenderTexture.active=cycle.NativeFrame;read.ReadPixels(new UnityEngine.Rect(0,0,352,704),0,0);read.Apply();
        var actual=read.GetPixels32();var source=sheets[rabbit].GetPixels32();int x=ANIMOL.FiveThemeMenu.FantasyBackgroundPolicy.RabbitX(p,rabbit),lost=0,opaque=0,clipped=0;
        for(int y=0;y<96;y++)for(int xx=0;xx<64;xx++)
        {
            var expected=source[y*512+frame*64+xx];if(expected.a==0)continue;opaque++;
            var got=actual[(704-436-96+y)*352+x+xx];if(got.r!=expected.r||got.g!=expected.g||got.b!=expected.b)lost++;
            var rect=cycle.Output.rectTransform;var position=rect.TransformPoint(new UnityEngine.Vector3((x+xx+.5f-176)/352*rect.rect.width,(352-436-96+y+.5f)/704*rect.rect.height,0));
            if(position.x<0||position.x>=UnityEngine.Screen.width||position.y<0||position.y>=UnityEngine.Screen.height)clipped++;
        }
        rows.Add(new{theme=theme+1,p,cam,rabbit,frame,opaque,lost,clipped});
        if(frame==0)System.IO.File.WriteAllBytes("Docs/Validation/OriginalCorrectionV2/Native_T0"+(theme+1)+"_p"+p.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"_c"+cam+"_r"+rabbit+".png",read.EncodeToPNG());
    }
}
finally
{
    cycle.InspectionCameraDirection=null;cycle.InspectionRabbitDirection=null;cycle.InspectionFrame=null;cycle.InspectionTime=null;UnityEngine.RenderTexture.active=old;
    UnityEngine.Object.Destroy(read);foreach(var sheet in sheets.Values)UnityEngine.Object.Destroy(sheet);
    System.IO.File.WriteAllText("Docs/Validation/OriginalCorrectionV2/MotionAudit-"+UnityEngine.Screen.height+".json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
}
return new{samples=rows.Count};
