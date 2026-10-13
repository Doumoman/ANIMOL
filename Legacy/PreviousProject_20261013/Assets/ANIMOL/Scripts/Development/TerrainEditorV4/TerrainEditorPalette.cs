using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ANIMOL.Development
{
    public sealed partial class TerrainEditorScreen
    {
        public readonly List<TerrainEditorPart> Parts=new List<TerrainEditorPart>();
        public IReadOnlyList<TerrainEditorPart> VisibleParts { get; private set; }=Array.Empty<TerrainEditorPart>();
        public TerrainEditorPart SelectedPart => Parts.FirstOrDefault(p=>p.id==State.partId);
        private RectTransform filterRow,cardViewport,cardContent;
        private GridLayoutGroup cardGrid;
        private Text paletteCount;
        private string[] themes={"CURRENT","ALL","T01","T02","T03","T04","T05","COMMON"};
        private string[] categories={"ALL","Platform","BigBridge","Wall/Ceiling","Mass","Interior","Special","Hazard","Object","Decoration"};
        private Button themeButton,styleButton,categoryButton;
        public void InitializePalette()
        {
            Parts.Clear();
            foreach(var e in Adapter.Terrain.Catalog.entries)
            {
                var category=e.kind=="InteriorOverlay"?"Interior":e.role.IndexOf("Bridge",StringComparison.OrdinalIgnoreCase)>=0?"BigBridge":
                    e.role.IndexOf("Wall",StringComparison.OrdinalIgnoreCase)>=0 || e.role.IndexOf("Ceiling",StringComparison.OrdinalIgnoreCase)>=0?"Wall/Ceiling":
                    e.role.IndexOf("Mass",StringComparison.OrdinalIgnoreCase)>=0 || e.role.IndexOf("Body",StringComparison.OrdinalIgnoreCase)>=0?"Mass":"Platform";
                Parts.Add(new TerrainEditorPart{id=e.id,name=e.styleId.Substring(e.styleId.Length-1)+" "+ShortRole(e.role),theme=e.themeId,style=e.styleId.Substring(e.styleId.Length-1),category=category,size=new Vector2(e.width,e.height),terrain=e});
            }
            if(Adapter.Objects!=null)
                foreach(var def in Adapter.Objects.Types.Where(d=>d!=null))
                {
                    var category=def.Layer==ANIMOL.Core.StageMapLayer.Decoration?"Decoration":def.Kind==ANIMOL.Core.StageMapObjectKind.Spike || def.Kind==ANIMOL.Core.StageMapObjectKind.Pounder || def.Kind==ANIMOL.Core.StageMapObjectKind.Hole?"Hazard":
                        def.Kind==ANIMOL.Core.StageMapObjectKind.PlayerStart || def.Kind==ANIMOL.Core.StageMapObjectKind.Checkpoint || def.Kind==ANIMOL.Core.StageMapObjectKind.Exit || def.Kind==ANIMOL.Core.StageMapObjectKind.BubbleCandidate?"Object":"Special";
                    Parts.Add(new TerrainEditorPart{id=def.StableTypeId,name=def.DisplayName,theme=string.IsNullOrEmpty(def.ThemeId)?"COMMON":def.ThemeId,style="",category=category,size=def.FootprintCells,obj=def});
                }
            Parts.AddRange(Adapter.Markers);
            filterRow=PanelRect("Filters",palette,Panel,new Vector2(0,.76f),Vector2.one);
            themeButton=Button("Theme",filterRow,"",.005f,.15f,()=>{State.theme=Cycle(themes,State.theme);RebuildPalette();});
            categoryButton=Button("Category",filterRow,"",.16f,.32f,()=>{State.category=Cycle(categories,State.category);RebuildPalette();});
            styleButton=Button("Style",filterRow,"",.33f,.43f,()=>{State.style=Cycle(new[]{"ALL","A","B","C","D"},State.style);RebuildPalette();});
            var search=Input("Search",filterRow,"검색 / stable ID",new Vector2(.44f,.15f),new Vector2(.70f,.85f));
            search.SetTextWithoutNotify(State.query);search.onValueChanged.AddListener(value=>{State.query=value;RebuildPalette();});
            paletteCount=Label("Registry count",filterRow,"",14);Stretch(paletteCount.rectTransform,new Vector2(.71f,0),new Vector2(.93f,1));
            Button("Palette size",filterRow,"높이",.94f,.995f,()=>{State.paletteHeight=State.paletteHeight<.3f?.4f:State.paletteHeight<.5f?.06f:.25f;LayoutPalette();});
            cardViewport=PanelRect("Palette scroll",palette,Ink,Vector2.zero,new Vector2(1,.75f));
            cardViewport.gameObject.AddComponent<RectMask2D>();
            var scroll=cardViewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=true;scroll.vertical=false;scroll.scrollSensitivity=25;
            cardContent=new GameObject("Silhouette cards",typeof(RectTransform)).GetComponent<RectTransform>();cardContent.SetParent(cardViewport,false);
            cardContent.anchorMin=new Vector2(0,0);cardContent.anchorMax=new Vector2(0,1);cardContent.pivot=new Vector2(0,.5f);
            cardGrid=cardContent.gameObject.AddComponent<GridLayoutGroup>();cardGrid.constraint=GridLayoutGroup.Constraint.FixedRowCount;cardGrid.constraintCount=2;
            cardGrid.startAxis=GridLayoutGroup.Axis.Vertical;cardGrid.spacing=new Vector2(6,4);cardGrid.padding=new RectOffset(6,6,4,4);
            scroll.viewport=cardViewport;scroll.content=cardContent;
            LayoutPalette();RebuildPalette();
            var duplicate=Parts.GroupBy(p=>p.id).FirstOrDefault(g=>g.Count()>1);
            if(duplicate!=null)SetStatus("중복 registry ID: "+duplicate.Key);
        }
        private static string ShortRole(string role)
        {return role.Replace("Platform","발판").Replace("Bridge","다리").Replace("WallCorner","L벽").Replace("Wall","벽").Replace("Ceiling","천장").Replace("Mass","몸체").Replace("FillPatch","질감").Replace("Source","");}
        private static string Cycle(string[] items,string value) => items[(Array.IndexOf(items,value)+1)%items.Length];
        public void LayoutPalette()
        {
            Stretch(palette,Vector2.zero,new Vector2(1,State.paletteHeight));
            Stretch(statusBar,new Vector2(0,State.paletteHeight),new Vector2(1,State.paletteHeight+.035f));
            Stretch(Viewport.rectTransform,new Vector2(0,State.paletteHeight+.035f),new Vector2(1,.945f));
            if(cardViewport!=null)cardViewport.gameObject.SetActive(State.paletteHeight>.1f);
            if(filterRow!=null)Stretch(filterRow,new Vector2(0,State.paletteHeight<.1f?0:.76f),Vector2.one);
            Canvas.ForceUpdateCanvases();ResizeCards();Resize();
        }
        private void ResizeCards()
        {
            if(cardGrid==null)return;
            var height=Mathf.Max(44,(cardViewport.rect.height-12)/2);cardGrid.cellSize=new Vector2(Mathf.Max(116,height*1.25f),height);
            cardContent.sizeDelta=new Vector2(Mathf.Ceil(VisibleParts.Count/2f)*(cardGrid.cellSize.x+6)+12,0);
        }
        public void RebuildPalette()
        {
            if(cardContent==null)return;
            for(int i=cardContent.childCount-1;i>=0;i--){var child=cardContent.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
            var theme=State.theme=="CURRENT"?Adapter.Map.ThemeId:State.theme;
            VisibleParts=Parts.Where(p=>(theme=="ALL" || p.theme==theme || State.theme=="CURRENT" && p.theme=="COMMON") &&
                (State.style=="ALL" || p.style==State.style) && (State.category=="ALL" || p.category==State.category) &&
                (string.IsNullOrWhiteSpace(State.query) || (p.id+" "+p.name).IndexOf(State.query,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
            themeButton.GetComponentInChildren<Text>().text="테마 "+State.theme;
            categoryButton.GetComponentInChildren<Text>().text=CategoryLabel(State.category);
            styleButton.GetComponentInChildren<Text>().text="스타일 "+State.style;
            paletteCount.text=$"{VisibleParts.Count} / {Parts.Count} · 60+45+6";
            foreach(var part in VisibleParts)
            {
                var selected=part.id==State.partId;
                var rect=PanelRect(part.id,cardContent,selected?Hex("5d275d"):Panel,Vector2.zero,Vector2.one);
                var button=rect.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>SelectPart(part.id));
                var outline=rect.gameObject.AddComponent<Outline>();outline.effectColor=selected?Gold:Panel;outline.effectDistance=new Vector2(2,-2);
                var shortName=part.name.Length>6?part.name.Substring(0,5)+"…":part.name;
                var label=Label("Name",rect,(selected?"✓ ":"")+shortName+" "+part.size.x+"×"+part.size.y,12);
                label.alignment=TextAnchor.MiddleCenter;Stretch(label.rectTransform,Vector2.zero,new Vector2(1,.28f));
                var area=new GameObject("Picture area",typeof(RectTransform)).GetComponent<RectTransform>();area.SetParent(rect,false);
                Stretch(area,new Vector2(.04f,.29f),new Vector2(.96f,.96f));
                var picture=new GameObject("Silhouette",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();picture.transform.SetParent(area,false);
                Stretch(picture.rectTransform,Vector2.zero,Vector2.one);picture.raycastTarget=false;
                picture.gameObject.AddComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
                var thumb=Adapter.Thumbnail(part);picture.texture=selected?thumb.color:thumb.silhouette;
                var hover=rect.gameObject.AddComponent<TerrainEditorCard>();hover.Initialize(this,part,thumb,picture,selected);
                if(part.IsOverlay){var badge=Label("Collision zero",rect,"충돌 0",11);badge.color=Blue;Stretch(badge.rectTransform,new Vector2(.68f,.68f),Vector2.one);}
                if(!string.IsNullOrEmpty(thumb.diagnostic)){picture.gameObject.SetActive(false);label.text="! "+part.name+" "+part.size.x+"×"+part.size.y;Stretch(label.rectTransform,new Vector2(.04f,.05f),new Vector2(.96f,.95f));}
            }
            ResizeCards();
        }
        public void SelectPart(string id)
        {State.partId=id;State.tool="Place";State.instanceId="";RebuildPalette();PartSelected();SetStatus(Describe(SelectedPart));}
        partial void PartSelected();
        public string Describe(TerrainEditorPart p)
        {
            if(p==null)return "부품을 선택하세요";
            var e=p.terrain;
            return p.id+" · "+p.size.x+"×"+p.size.y+(e==null?" · "+p.name:e.kind=="Structure"?$" · 고체 {e.solidCount} / 빈 {e.voidCount}":$" · 추가 충돌 0 / 받침 {e.supportCount}");
        }
        private string CategoryLabel(string value)
        {var labels=new[]{"전체 분류","발판","큰 다리","벽 / 천장","큰 몸체","내부 질감","특수 발판","장애물","맵 객체","장식"};return labels[Mathf.Max(0,Array.IndexOf(categories,value))];}
        public InputField Input(string name,Transform parent,string placeholder,Vector2 min,Vector2 max)
        {
            var rect=PanelRect(name,parent,Ink,min,max);var field=rect.gameObject.AddComponent<InputField>();
            var text=Label("Text",rect,"",16);Stretch(text.rectTransform,new Vector2(.03f,0),new Vector2(.97f,1));field.textComponent=text;
            var hint=Label("Placeholder",rect,placeholder,16);hint.color=Hex("94b0c2");Stretch(hint.rectTransform,new Vector2(.03f,0),new Vector2(.97f,1));field.placeholder=hint;
            return field;
        }
    }
    public sealed class TerrainEditorCard : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        private TerrainEditorScreen screen;private TerrainEditorPart part;private TerrainEditorThumbnail thumb;private RawImage image;private bool selected;
        public void Initialize(TerrainEditorScreen s,TerrainEditorPart p,TerrainEditorThumbnail t,RawImage i,bool value){screen=s;part=p;thumb=t;image=i;selected=value;}
        public void OnPointerEnter(PointerEventData data){image.texture=thumb.color;screen.SetStatus(string.IsNullOrEmpty(thumb.diagnostic)?screen.Describe(part):thumb.diagnostic);}
        public void OnPointerExit(PointerEventData data){image.texture=selected?thumb.color:thumb.silhouette;}
    }
}
