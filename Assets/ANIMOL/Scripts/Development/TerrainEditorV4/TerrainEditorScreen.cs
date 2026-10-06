using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ANIMOL.Development
{
    public sealed partial class TerrainEditorScreen : MonoBehaviour
    {
        public static TerrainEditorScreen Instance { get; private set; }
        public ITerrainEditorAdapter Adapter { get; private set; }
        public TerrainEditorViewState State = new TerrainEditorViewState();
        public Camera MapCamera { get; private set; }
        public RawImage Viewport { get; private set; }
        public RectTransform CanvasRect { get; private set; }
        public string Status { get; private set; } = "연결 중";
        protected internal Transform artRoot;
        private GameObject editArt;
        private RenderTexture target;
        private RectTransform top, palette, statusBar;
        private Text title, statusLabel;
        private RectTransform mapSettings;
        private Text mapSettingsText;
        private int lastWidth, lastHeight;
        public static Color Ink => Hex("1a1c2c");
        public static Color Panel => Hex("333c57");
        public static Color Paper => Hex("f4f4f4");
        public static Color Gold => Hex("ffcd75");
        public static Color Blue => Hex("41a6f6");
        public static Color Green => Hex("a7f070");
        public static Color Red => Hex("ef7d57");
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }

        public void Initialize(ITerrainEditorAdapter adapter, TerrainEditorViewState state = null)
        {
            Instance = this; Adapter = adapter; if (state != null) State = state;
            var canvasGo = new GameObject("Terrain Editor V4", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            CanvasRect = canvasGo.GetComponent<RectTransform>();
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.GetComponent<Canvas>().sortingOrder = 1000;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0;
            PanelRect("Background", CanvasRect, Ink, Vector2.zero, Vector2.one);
            top = PanelRect("Top", CanvasRect, Panel, new Vector2(0,.945f), Vector2.one);
            title = Label("Title", top, "ANIMOL · 개발 맵 편집", 19);
            Stretch(title.rectTransform, new Vector2(.01f,0), new Vector2(.34f,1));
            var mapButton=title.gameObject.AddComponent<Button>();title.raycastTarget=true;
            mapButton.onClick.AddListener(()=>{if(mapSettings!=null && !IsTesting){mapSettings.gameObject.SetActive(!mapSettings.gameObject.activeSelf);UpdateMapSettings();}});
            Button("Save", top, "저장", .35f, .45f, () => Run(() => { Adapter.Save(); SetStatus("저장 완료 · revision " + Adapter.Map.AuthoringRevision); }));
            Button("Undo", top, "되돌리기", .46f, .58f, () => Run(Adapter.Undo));
            Button("Redo", top, "다시 실행", .59f, .71f, () => Run(Adapter.Redo));
            Button("Exit", top, "나가기", .88f, .99f, () => Adapter.Exit());
            var view = new GameObject("Map viewport", typeof(RectTransform), typeof(RawImage));
            view.transform.SetParent(CanvasRect,false); Viewport = view.GetComponent<RawImage>();
            view.AddComponent<RectMask2D>();
            Stretch(Viewport.rectTransform, new Vector2(0,.28f), new Vector2(1,.945f));
            palette = PanelRect("Palette", CanvasRect, Panel, Vector2.zero, new Vector2(1,.25f));
            statusBar = PanelRect("Status", CanvasRect, Ink, new Vector2(0,.25f), new Vector2(1,.28f));
            statusLabel = Label("Status text", statusBar, "", 16);
            Stretch(statusLabel.rectTransform, new Vector2(.01f,0), new Vector2(.99f,1));
            var cameraObject = new GameObject("Edit render camera",typeof(Camera));
            cameraObject.transform.SetParent(transform,false); MapCamera = cameraObject.GetComponent<Camera>();
            MapCamera.orthographic = true; MapCamera.clearFlags = CameraClearFlags.SolidColor;
            MapCamera.backgroundColor = Ink; MapCamera.cullingMask = 1 << TerrainEditorArt.PreviewLayer;
            MapCamera.nearClipPlane = .1f; MapCamera.farClipPlane = 200;
            artRoot = new GameObject("Editor render world").transform; artRoot.SetParent(transform,false);
            if (EventSystem.current == null)
            { var es = new GameObject("Editor input", typeof(EventSystem), typeof(StandaloneInputModule)); es.transform.SetParent(transform,false); }
            if (adapter == null) { SetStatus("저장 어댑터 없음 · 편집 불가"); return; }
            adapter.Changed += Refresh;
            Refresh(); InitializePalette(); InitializeTools(); InitializeMapSettings(); InitializeTestMode(); SetStatus("연결 완료 · " + adapter.Map.StageId + " · revision " + adapter.Map.AuthoringRevision);
        }

        private void InitializeMapSettings()
        {
            mapSettings=PanelRect("Map configuration",Viewport.transform,Panel,new Vector2(.25f,.25f),new Vector2(.75f,.8f));
            mapSettingsText=Label("Map configuration details",mapSettings,"",18);Stretch(mapSettingsText.rectTransform,new Vector2(.04f,.55f),new Vector2(.96f,.96f));
            var units=Button("Configure cell units",mapSettings,"1셀 = 1unit 설정",.05f,.95f,()=>Run(()=>{Adapter.EditMapConfiguration("Units");UpdateMapSettings();}));
            Stretch(units.GetComponent<RectTransform>(),new Vector2(.05f,.37f),new Vector2(.95f,.51f));
            var edges=new[]{"Left","Right","Bottom","Top"};var labels=new[]{"왼쪽 +1","오른쪽 +1","아래 +1","위 +1"};
            for(int i=0;i<4;i++){var edge=edges[i];var b=Button("Expand "+edge,mapSettings,labels[i],.04f+i*.24f,.26f+i*.24f,()=>Run(()=>{Adapter.EditMapConfiguration(edge);UpdateMapSettings();}));Stretch(b.GetComponent<RectTransform>(),new Vector2(.04f+i*.24f,.18f),new Vector2(.26f+i*.24f,.32f));}
            var close=Button("Close map configuration",mapSettings,"닫기",.3f,.7f,()=>mapSettings.gameObject.SetActive(false));Stretch(close.GetComponent<RectTransform>(),new Vector2(.3f,.02f),new Vector2(.7f,.14f));
            mapSettings.gameObject.SetActive(false);
        }
        private void UpdateMapSettings()
        {mapSettingsText.text=Adapter.Map.StageId+" · "+Adapter.Map.ThemeId+"\n맵 범위(셀) "+Adapter.Map.CellBounds+"\n셀 크기 "+Adapter.Map.WorldUnitsPerCell+(Adapter.Map.WorldUnitsPerCell<=0?" (미설정 · 편집 미리보기 1unit)":" unit")+"\n1셀 단위 범위 확장";}

        public void Refresh()
        {
            // Construct the replacement first: a render failure leaves the last good preview intact.
            var next = TerrainEditorArt.BuildMap(Adapter, artRoot);
            if (editArt != null) { editArt.SetActive(false); Destroy(editArt); }
            editArt = next;
            title.text = Adapter.Map.StageId + "  ·  " + Adapter.Map.ThemeId + "  · EDIT";
            Refreshed();
        }
        partial void Refreshed();
        partial void UpdateTools();
        partial void DestroyTools();
        private void Update()
        {
            if (Viewport == null) return;
            if (lastWidth != Screen.width || lastHeight != Screen.height || target == null) Resize();
            MapCamera.transform.position = new Vector3(State.center.x, State.center.y, -50);
            MapCamera.orthographicSize = State.zoom;
            UpdateTools();
            UpdateTest();
        }
        public void Resize()
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4]; Viewport.rectTransform.GetWorldCorners(corners);
            int w = Mathf.Max(32, Mathf.RoundToInt(corners[2].x-corners[0].x)), h = Mathf.Max(32, Mathf.RoundToInt(corners[2].y-corners[0].y));
            if (target != null) { MapCamera.targetTexture = null; target.Release(); Destroy(target); }
            target = new RenderTexture(w,h,24) { name = "Terrain Editor viewport", filterMode = FilterMode.Point };
            target.Create(); MapCamera.targetTexture = target; MapCamera.aspect = (float)w/h; Viewport.texture = target;
            lastWidth = Screen.width; lastHeight = Screen.height;
            ResizeCards();
        }
        public Vector2 ScreenToLogical(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Viewport.rectTransform,screen,null,out var local);
            var rect = Viewport.rectTransform.rect;
            var uv = new Vector3((local.x-rect.xMin)/rect.width,(local.y-rect.yMin)/rect.height,50);
            return (Vector2)MapCamera.ViewportToWorldPoint(uv)/Adapter.Map.GetEditorPreviewUnitsPerCell();
        }
        public Vector2Int ScreenToCell(Vector2 screen)
        { var p = ScreenToLogical(screen); return new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y)); }
        public void SetStatus(string text) { Status = text; if (statusLabel != null) statusLabel.text = text; }
        public void Run(Action action) { try { action(); } catch(Exception ex) { SetStatus(ex.Message); } }
        private void OnDestroy()
        {
            DestroyTools();
            if(testMap!=null)Destroy(testMap);
            if(testTarget!=null){testTarget.Release();Destroy(testTarget);}
            if (Adapter != null) { Adapter.Changed -= Refresh; Adapter.Dispose(); }
            if (target != null) { target.Release(); Destroy(target); }
            if (Instance == this) Instance = null;
        }
        public static void Stretch(RectTransform r, Vector2 min, Vector2 max)
        { r.anchorMin=min;r.anchorMax=max;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero; }
        public RectTransform PanelRect(string name,Transform parent,Color color,Vector2 min,Vector2 max)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            go.GetComponent<Image>().color=color;var r=go.GetComponent<RectTransform>();Stretch(r,min,max);return r;
        }
        public Text Label(string name,Transform parent,string text,int size=18)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var label=go.GetComponent<Text>(); label.font=Adapter?.Font; label.text=text;label.fontSize=size;
            label.color=Paper;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
            return label;
        }
        public Button Button(string name,Transform parent,string text,float left,float right,Action action)
        {
            var r=PanelRect(name,parent,Ink,new Vector2(left,.12f),new Vector2(right,.88f));
            var image=r.GetComponent<Image>();if(Adapter?.ButtonSprite!=null){image.sprite=Adapter.ButtonSprite;image.type=Image.Type.Sliced;}
            var button=r.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>action());
            var colors=button.colors;colors.highlightedColor=Blue;colors.selectedColor=Gold;button.colors=colors;
            var label=Label("Label",r,text);label.alignment=TextAnchor.MiddleCenter;Stretch(label.rectTransform,Vector2.zero,Vector2.one);
            return button;
        }
    }
}
