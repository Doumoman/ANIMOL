using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Typography
{
    /// <summary>Preserves existing Text references, layout and raycast surfaces; only substitutes the glyph mesh.</summary>
    [DefaultExecutionOrder(2000)]
    [RequireComponent(typeof(Text))]
    public sealed class PixelTextBridge : BaseMeshEffect
    {
        public PixelTypographyProfile Profile;
        // Opt-in for scroll content whose legacy Text metrics differ from the rendered TMP font.
        public bool SizeScrollContentToText;
        public Text Source { get; private set; }
        public TextMeshProUGUI Display { get; private set; }
        private string lastText;
        private Vector2 lastSize;
        private float lastRequested, lastScale;
        private TextAnchor lastAlignment;
        private bool lastRich;
        private bool lastSizeScrollContent;
        private LayoutElement scrollLayout;
        public bool Overflow { get; private set; }
        public float PhysicalPointSize => Display == null ? 0 : Display.fontSize * Display.canvas.scaleFactor;

        protected override void OnEnable() { base.OnEnable(); Source = GetComponent<Text>(); }
        protected override void OnDisable()
        {
            if (Display != null) Display.gameObject.SetActive(false);
            base.OnDisable();
        }
        protected override void OnDestroy()
        {
            if (Display != null) Destroy(Display.gameObject);
            base.OnDestroy();
        }
        public override void ModifyMesh(VertexHelper vertices)
        {
            if (IsActive() && Display != null && Display.gameObject.activeSelf) vertices.Clear();
        }
        private void LateUpdate() => Synchronize();
        public void Synchronize()
        {
            if (Profile == null || Profile.Font == null || Source == null) return;
            if (Display == null)
            {
                var child = new GameObject("Pixel TMP (presentation only)", typeof(RectTransform), typeof(LayoutElement));
                child.transform.SetParent(transform, false);
                var rect = (RectTransform)child.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                child.GetComponent<LayoutElement>().ignoreLayout = true;
                Display = child.AddComponent<TextMeshProUGUI>();
                Display.font = Profile.Font; Display.raycastTarget = false; Display.maskable = Source.maskable;
                Display.fontStyle = FontStyles.Normal; Display.enableAutoSizing = false;
                Display.textWrappingMode = TextWrappingModes.Normal;
                Display.overflowMode = TextOverflowModes.Overflow;
                Display.parseCtrlCharacters = false; Display.margin = Vector4.zero;
                Display.lineSpacing = 4;
                Source.SetVerticesDirty();
            }
            bool visible = Source.enabled;
            if (Display.gameObject.activeSelf != visible) { Display.gameObject.SetActive(visible); Source.SetVerticesDirty(); }
            if (!visible) return;
            Display.color = Source.color;
            float scale = Mathf.Max(.01f, Display.canvas != null ? Display.canvas.scaleFactor : 1);
            float requested = Source.resizeTextForBestFit ? Source.resizeTextMaxSize : Source.fontSize;
            var size = Source.rectTransform.rect.size;
            if (lastText == Source.text && lastSize == size && lastRequested == requested && lastScale == scale && lastAlignment == Source.alignment && lastRich == Source.supportRichText && lastSizeScrollContent == SizeScrollContentToText) return;
            lastText = Source.text; lastSize = size; lastRequested = requested; lastScale = scale; lastAlignment = Source.alignment; lastRich = Source.supportRichText;
            lastSizeScrollContent = SizeScrollContentToText;
            Display.text = Source.text; Display.richText = Source.supportRichText;
            Display.alignment = Alignment(Source.alignment);
            Display.lineSpacing = SizeScrollContentToText ? 12 : 4;
            // 16px source grid, integer physical magnification. Never fractional best-fit smoothing.
            int pixels = Mathf.Max(requested * scale >= 20 ? 32 : 16, Mathf.RoundToInt(requested * scale / 16f) * 16);
            for (; pixels > 16; pixels -= 16)
            {
                Display.fontSize = pixels / scale;
                var preferred = Display.GetPreferredValues(Display.text, Mathf.Max(1,size.x), Mathf.Infinity);
                if (preferred.x <= size.x + .5f && (SizeScrollContentToText || preferred.y <= size.y + .5f)) break;
            }
            Display.fontSize = pixels / scale;
            Display.text = BalancedParagraphs(Source.text, Mathf.Max(1,size.x));
            var measured = Display.GetPreferredValues(Display.text, Mathf.Max(1,size.x), Mathf.Infinity);
            if (SizeScrollContentToText)
            {
                // ContentSizeFitter uses this higher-priority measurement instead of legacy Text.
                // Measure after paragraph balancing, which can introduce additional wrapped lines.
                if (scrollLayout == null) scrollLayout = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
                scrollLayout.layoutPriority = 1;
                scrollLayout.preferredHeight = Mathf.Ceil(measured.y) + 2;
            }
            Overflow = measured.x > size.x + 1 || measured.y > size.y + 1;
            Profile.EnsurePointSampling();
        }
        private string BalancedParagraphs(string text, float width)
        {
            if (text.Contains("<")) return text; // Do not split rich-text tags.
            var lines = new List<string>();
            foreach(var paragraph in text.Split('\n')) {
                float natural = Display.GetPreferredValues(paragraph,Mathf.Infinity,Mathf.Infinity).x;
                var words=paragraph.Split(' ');
                int count=Mathf.CeilToInt(natural/width);
                if(count<2 || words.Length<3) { lines.Add(paragraph); continue; }
                if(count==2) {
                    string bestLeft=null,bestRight=null; float bestScore=float.PositiveInfinity;
                    for(int split=1;split<words.Length;split++) {
                        string left=string.Join(" ",words.Take(split)),right=string.Join(" ",words.Skip(split));
                        float a=Display.GetPreferredValues(left,Mathf.Infinity,Mathf.Infinity).x,b=Display.GetPreferredValues(right,Mathf.Infinity,Mathf.Infinity).x;
                        float score=Mathf.Max(a,b);
                        if(score<=width && score<bestScore) { bestScore=score;bestLeft=left;bestRight=right; }
                    }
                    if(bestLeft!=null) { lines.Add(bestLeft);lines.Add(bestRight);continue; }
                }
                float target=natural/count; string current="";
                foreach(var word in words) {
                    string next=current.Length==0 ? word : current+" "+word;
                    float nextWidth=Display.GetPreferredValues(next,Mathf.Infinity,Mathf.Infinity).x;
                    if(current.Length>0 && (nextWidth>width || (nextWidth>target && Display.GetPreferredValues(current,Mathf.Infinity,Mathf.Infinity).x>target*.75f))) {
                        lines.Add(current); current=word;
                    } else current=next;
                }
                lines.Add(current);
            }
            return string.Join("\n",lines);
        }
        private static TextAlignmentOptions Alignment(TextAnchor alignment) => alignment switch {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft, TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight, TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Midline, TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft, TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            _ => TextAlignmentOptions.BottomRight
        };
    }
}
