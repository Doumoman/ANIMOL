using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.Gameplay;
using ANIMOL.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ANIMOL.Editor
{
    /// <summary>All writes are confined to the representative specimen and its generated geometry.</summary>
    public static class MoonGraphicsQaBuilder
    {
        public const string Root = "Assets/ANIMOL/GraphicsQA";
        public const string ScenePath = Root + "/MoonGraphicsQA.unity";
        private static Material material;
        private static Material atlasMaterial;
        private static readonly Color Ink = Hex("172A3D"), Stone = Hex("294259"), Shade = Hex("20354C"), Jade = Hex("78BBAE"), Light = Hex("C8E5CB"), Gold = Hex("DCB16D");
        private static int meshIndex;

        [MenuItem("ANIMOL/Graphics QA/Build Moon Representative Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit play mode first.");
            // Do not close or save any user scene with unsaved edits.
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("An open scene has unsaved changes; preserve it before building QA.");
            Directory.CreateDirectory(Root + "/Meshes"); AssetDatabase.Refresh(); meshIndex = 0;
            material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/PixelGeometry.mat");
            if (material == null) { material = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material, Root + "/PixelGeometry.mat"); }
            atlasMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/MoonAtlasPalette.mat");
            if (atlasMaterial == null) { atlasMaterial = new Material(Shader.Find("ANIMOL/QA/MoonAtlasPalette")); AssetDatabase.CreateAsset(atlasMaterial, Root + "/MoonAtlasPalette.mat"); }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(PortraitWorldCameraPolicy)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 3, -10);
            camera.orthographic = true; camera.aspect = 1080f / 1920; camera.orthographicSize = 12f / (2 * camera.aspect);
            camera.backgroundColor = Hex("101E32"); camera.clearFlags = CameraClearFlags.SolidColor;
            var before = new GameObject("Original moon atlas presentation");
            var after = new GameObject("Refined moon geometry presentation");
            var physics = new GameObject("QA terrain collision");
            // Rectangular terrain uses all nine directions. Shared edges have no texture speckles or single pixels.
            TerrainBlock(before.transform, after.transform, physics.transform, -6, -7, 12, 8, "Floor");
            TerrainBlock(before.transform, after.transform, physics.transform, -6, 1, 2, 3, "West terrace");
            TerrainBlock(before.transform, after.transform, physics.transform, 4, 1, 2, 3, "East terrace");
            TerrainBlock(before.transform, after.transform, physics.transform, 4, 7, 2, 2, "Upper terrace");
            // Two thin return ledges reuse the original one-way collision rules below.
            Background(after.transform);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Gameplay/DevPlayer.prefab"));
            player.name = "QA Rabbit Player"; player.transform.position = MoonGraphicsQaScene.Spawn;
            var oldRenderer = player.GetComponentInChildren<Renderer>();
            var rabbit = Rabbit(player.transform); oldRenderer.enabled = false;
            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/UI_CommonRoot.prefab");
            var ui = (GameObject)PrefabUtility.InstantiatePrefab(rootPrefab);
            var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/SC15_GameplayHud.prefab"), ui.transform.Find("SafeArea/ScreenHost"));
            var pause = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/UI/PauseModal.prefab"), ui.transform.Find("SafeArea/ModalHost"));
            pause.SetActive(false); if (ui.GetComponentInChildren<DevMobileInputRouter>(true) == null) ui.AddComponent<DevMobileInputRouter>();
            Header(ui.transform.Find("SafeArea"), hud.GetComponentInChildren<Text>(true).font);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            new GameObject("Gameplay feedback", typeof(GameplayFeedbackDirector));
            var qa = new GameObject("Moon representative QA", typeof(MoonGraphicsQaScene)).GetComponent<MoonGraphicsQaScene>();
            var devices = new List<StageMapObjectPlacement>();
            devices.Add(Placement("MOON_LANTERN_STEP", "QA-M1", -3, 2));
            devices.Add(Placement("MOON_JADE_BALANCE", "QA-M2-L", -1, 3, "QA-M2-R"));
            devices.Add(Placement("MOON_JADE_BALANCE", "QA-M2-R", 2, 4, "QA-M2-L"));
            devices.Add(Placement("MOON_PHASE_STAIR", "QA-M6", 1, 6));
            devices.Add(Placement("TILE_DROP_PLATFORM", "QA-OneWay-Lower", -4, 1));
            devices.Add(Placement("TILE_DROP_PLATFORM", "QA-OneWay-Return", -2, 3));
            devices.Add(Placement("TILE_DROP_PLATFORM", "QA-OneWay-Upper", -1, 4));
            devices.Add(Placement("TILE_DROP_PLATFORM", "QA-OneWay-Plate", 0, 5));
            qa.Configure(devices.ToArray(), before, after, rabbit, oldRenderer, ui.GetComponent<Canvas>(), material, ReadInkBounds(), atlasMaterial);
            before.SetActive(false);
            foreach(var oldMesh in AssetDatabase.FindAssets("t:Mesh",new[]{Root+"/Meshes"}).Select(AssetDatabase.GUIDToAssetPath))
                if(int.TryParse(Path.GetFileNameWithoutExtension(oldMesh).Replace("qa_",string.Empty),out var index) && index>=meshIndex) AssetDatabase.DeleteAsset(oldMesh);
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene, ScenePath);
            PortraitGameViewSetup.Use1080x1920();
            Debug.Log("[GraphicsQA] Built isolated scene; no campaign assets, shared prefab or build settings changed.");
        }

        private static MoonGraphicsQaScene.SpriteInkBounds[] ReadInkBounds()
        {
            const string path = "Assets/ANIMOL/Atlases/ANIMOL_Master_Animations_32.png";
            var readable = new Texture2D(2,2); readable.LoadImage(File.ReadAllBytes(path));
            var pixels = readable.GetPixels32(); var result = new List<MoonGraphicsQaScene.SpriteInkBounds>();
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            {
                var rect = sprite.rect; int left=(int)rect.width, bottom=(int)rect.height, right=-1, top=-1;
                for(int y=0;y<rect.height;y++) for(int x=0;x<rect.width;x++)
                {
                    if(pixels[((int)rect.y+y)*readable.width+(int)rect.x+x].a<16) continue;
                    left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);
                }
                if(right>=left)
                {
                    // Ignore narrow hanging cords when aligning the actual platform surface.
                    for(int row=top;row>=bottom;row--)
                    {
                        int count=0;for(int col=left;col<=right;col++) if(pixels[((int)rect.y+row)*readable.width+(int)rect.x+col].a>=16) count++;
                        if(count>=(right-left+1)*.7f){top=row;break;}
                    }
                    result.Add(new MoonGraphicsQaScene.SpriteInkBounds {sprite=sprite,bounds=new Rect((left-sprite.pivot.x)/32f,(bottom-sprite.pivot.y)/32f,(right-left+1)/32f,(top-bottom+1)/32f)});
                }
            }
            UnityEngine.Object.DestroyImmediate(readable);return result.ToArray();
        }

        private static StageMapObjectPlacement Placement(string typeId, string id, int x, int y, string link = null)
        {
            var registry = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>("Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset");
            var type = registry.Find(typeId); var settings = type.DefaultSettings.Clone();
            if (link != null) { settings.EditorSetLinkedInstanceIds(new[] { link }); settings.EditorSetPathCells(new[] { new Vector2Int(x,y),new Vector2Int(x,y-1) }); }
            return new StageMapObjectPlacement(id, type.Kind, x, y, typeId, type.Prefab, settings);
        }

        private static void TerrainBlock(Transform before, Transform after, Transform physics, int x, int y, int width, int height, string name)
        {
            var solid = new GameObject(name, typeof(BoxCollider2D)); solid.transform.SetParent(physics, false);
            solid.transform.position = new Vector2(x + width / 2f, y + height / 2f); solid.GetComponent<BoxCollider2D>().size = new Vector2(width, height);
            var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/ANIMOL/Atlases/ANIMOL_Master_Terrain_32.png").OfType<Sprite>().ToDictionary(s => s.name);
            for (var j = 0; j < height; j++) for (var i = 0; i < width; i++)
            {
                var direction = (j == height - 1 ? "N" : j == 0 ? "S" : "") + (i == 0 ? "W" : i == width-1 ? "E" : "");
                if (direction.Length == 0) direction = "C";
                var old = new GameObject(name + " moon_" + direction, typeof(SpriteRenderer)); old.transform.SetParent(before, false);
                old.transform.position = new Vector2(x + i + .5f, y + j + .5f); old.GetComponent<SpriteRenderer>().sprite = sprites["moon_" + direction];
                var b = new PixelMesh(); b.Rect(0, 0, 32, 32, Stone);
                // Continuous courses: accents only on exposed boundaries, never random interior flecks.
                if (j == height-1) { b.Rect(0, 27, 32, 5, Jade); b.Rect(0, 30, 32, 2, Light); b.Rect(0, 25, 32, 2, Ink); }
                if (j == 0) { b.Rect(0, 0, 32, 2, Ink); b.Rect(0, 2, 32, 2, Shade); }
                if (i == 0) { b.Rect(0, 0, 2, 30, Ink); b.Rect(2, 3, 2, j == height-1 ? 22 : 29, Shade); }
                if (i == width-1) { b.Rect(30, 0, 2, 30, Ink); b.Rect(28, 3, 2, j == height-1 ? 22 : 29, Shade); }
                var tile = Make(name + " QA_" + direction, b, after, new Vector3(x+i, y+j, 0), 0);
            }
            // Large architectural courses, deliberately not one motif repeated in every tile.
            var courses = new PixelMesh();
            for (var row = 1; row < height; row+=2)
            {
                courses.Rect(3, row*32-1, width*32-6, 1, Shade);
                for (var col = 2 + (row%4==1 ? 0:1); col < width; col+=3) courses.Rect(col*32, row*32-31, 1, 30, Shade);
            }
            Make(name + " masonry courses", courses, after, new Vector3(x,y,0), 1);
        }

        private static void Background(Transform parent)
        {
            var backdrop = new PixelMesh();
            backdrop.Rect(-192, -240, 384, 760, Hex("101E32"));
            backdrop.Rect(-180, 34, 360, 260, Hex("15283D"));
            // Moon framed by architecture; no isolated stars or repeated dirt marks.
            for (int row=-37; row<=37; row++)
            { var half=(int)Mathf.Sqrt(38*38-row*row); backdrop.Rect(-89-half, 322+row, half*2, 1, Hex("758F96")); }
            for (int row=-32; row<=32; row++)
            { var half=(int)Mathf.Sqrt(33*33-row*row); backdrop.Rect(-80-half, 329+row, half*2, 1, Hex("172D42")); }
            // Columns terminate exactly at the floor top, with bases resting on it.
            foreach (var px in new[] {-166, -46, 70, 166})
            {
                backdrop.Rect(px-8,32,16,207,Hex("243C50")); backdrop.Rect(px-6,32,3,207,Hex("304A5C"));
                backdrop.Rect(px-13,32,26,8,Hex("304A5C")); backdrop.Rect(px-12,231,24,8,Hex("304A5C"));
            }
            backdrop.Rect(-183,239,366,12,Hex("2C4558"));
            backdrop.Rect(-175,251,350,4,Hex("425F69"));
            for (int step=0; step<8; step++) backdrop.Rect(-191+step*5,255+step*3,382-step*10,3,Hex(step%2==0 ? "233A50":"1D3248"));
            backdrop.Rect(-7,278,14,10,Hex("425F69"));
            // Lantern brackets attach to pillars, with continuous suspension cords.
            foreach (var px in new[] {-166,166})
            {
                var inward=px<0?1:-1;
                backdrop.Rect(Mathf.Min(px,px+inward*26),208,26,3,Hex("617B7C"));
                var lx=px+inward*26; backdrop.Rect(lx,183,2,25,Hex("617B7C"));
                backdrop.Rect(lx-7,162,16,22,Hex("304651")); backdrop.Rect(lx-5,165,12,15,Hex("AD9161"));
                backdrop.Rect(lx-2,166,5,13,Hex("D0BA7E")); backdrop.Rect(lx-8,181,18,3,Hex("617B7C"));
            }
            // Rail is anchored above the floor and behind every gameplay object.
            backdrop.Rect(-180,45,360,3,Hex("2C4558")); backdrop.Rect(-180,65,360,3,Hex("2C4558"));
            for(int px=-180;px<=180;px+=24) backdrop.Rect(px,32,3,36,Hex("2C4558"));
            Make("Moon palace anchored architecture",backdrop,parent,Vector3.zero,-30);
            // Foreground fascia remains quiet underneath the touch controls.
            var fascia=new PixelMesh(); fascia.Rect(-192,-360,384,382,Hex("172B3E")); fascia.Rect(-192,15,384,3,Hex("42636C"));
            Make("Foreground foundation",fascia,parent,Vector3.zero,2);
        }

        private static GameObject Rabbit(Transform parent)
        {
            var b=new PixelMesh();
            b.Rect(-11,-18,23,29,Ink); b.Rect(-9,-15,19,24,Hex("B8C9C0")); b.Rect(-9,-9,18,19,Hex("EDF0D8"));
            b.Rect(-9,9,7,14,Ink); b.Rect(3,8,7,15,Ink); b.Rect(-7,9,3,12,Hex("EDF0D8")); b.Rect(5,9,3,12,Hex("EDF0D8"));
            b.Rect(-6,12,2,7,Hex("C6A69B")); b.Rect(6,12,2,7,Hex("C6A69B"));
            b.Rect(5,1,3,4,Ink); b.Rect(9,-4,3,2,Hex("BA907E")); b.Rect(-11,-11,23,4,Jade); b.Rect(-12,-14,6,4,Gold);
            b.Rect(-10,-19,9,3,Light); b.Rect(3,-19,10,3,Light); b.Rect(-14,-10,5,6,Light);
            return Make("QA ivory rabbit",b,parent,new Vector3(0,0,0),15);
        }

        private static void Header(Transform safeArea, Font font)
        {
            var root=new GameObject("QAHeader",typeof(RectTransform)).GetComponent<RectTransform>(); root.SetParent(safeArea,false);
            root.SetAsFirstSibling();
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            var panel=new GameObject("HeaderPanel",typeof(RectTransform),typeof(Image)); panel.transform.SetParent(root,false);
            var rt=panel.GetComponent<RectTransform>(); rt.anchorMin=new Vector2(0,1);rt.anchorMax=Vector2.one;rt.pivot=new Vector2(.5f,1);rt.sizeDelta=new Vector2(0,218);rt.anchoredPosition=Vector2.zero;
            panel.GetComponent<Image>().color=new Color(.04f,.09f,.15f,.93f);panel.GetComponent<Image>().raycastTarget=false;
            Label(root,"Title","월궁  /  달빛 회랑",font,44,new Vector2(48,-48),new Vector2(860,60),Light);
            Label(root,"Clock","연습     스태미나 100",font,28,new Vector2(50,-113),new Vector2(850,40),Jade);
            Label(root,"State","달빛  밟기 가능     월상  수평",font,26,new Vector2(50,-160),new Vector2(900,38),Gold);
            Label(root,"Hint","연습 구간  ·  기록 / 보상 없음",font,24,new Vector2(50,-230),new Vector2(900,40),Hex("8BA5AE"));
            // Keep the existing pause button above this decorative header.
            var oldPause=safeArea.Find("ScreenHost/SC15_GameplayHud/PauseButton");if(oldPause!=null) oldPause.SetAsLastSibling();
        }
        private static void Label(Transform parent,string name,string value,Font font,int size,Vector2 pos,Vector2 dimensions,Color color)
        {
            var text=new GameObject(name,typeof(RectTransform),typeof(Text)).GetComponent<Text>();text.transform.SetParent(parent,false);
            text.font=font;text.fontSize=size;text.text=value;text.color=color;text.raycastTarget=false;text.alignment=TextAnchor.MiddleLeft;
            var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=dimensions;
        }
        private static GameObject Make(string name, PixelMesh pixels, Transform parent, Vector3 position,int order)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var path=$"{Root}/Meshes/qa_{meshIndex++:0000}.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}pixels.Apply(mesh);EditorUtility.SetDirty(mesh);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;return go;
        }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#"+value,out var color);return color; }
        public sealed class PixelMesh
        {
            private readonly List<Vector3> vertices=new List<Vector3>();private readonly List<int> indices=new List<int>();private readonly List<Color> colors=new List<Color>();
            public void Rect(int x,int y,int width,int height,Color color)
            {
                if(width<=0||height<=0)return;var i=vertices.Count;
                vertices.Add(new Vector3(x/32f,y/32f));vertices.Add(new Vector3(x/32f,(y+height)/32f));vertices.Add(new Vector3((x+width)/32f,(y+height)/32f));vertices.Add(new Vector3((x+width)/32f,y/32f));
                indices.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});for(int j=0;j<4;j++)colors.Add(QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color);
            }
            public void Apply(Mesh mesh){mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.SetColors(colors);mesh.SetUVs(0,vertices.Select(_=>Vector2.zero).ToList());mesh.RecalculateBounds();}
        }
    }
}
