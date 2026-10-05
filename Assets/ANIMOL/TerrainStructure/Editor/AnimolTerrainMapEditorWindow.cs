using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Animol.TerrainStructure.Editor
{
    /// <summary>Saved 1x1 structure authoring. Host binding writes the existing map asset.</summary>
    public sealed class AnimolTerrainMapEditorWindow : EditorWindow
    {
        private const string Root = "Assets/ANIMOL/TerrainStructure";
        private static readonly string[] ThemeIds = { "T01", "T02", "T03", "T04", "T05" };
        private static readonly string[] ThemeLabels = { "T01 월궁", "T02 구름고래 목장", "T03 별가루 도서관", "T04 시간유리 온실", "T05 오로라 수정광산" };
        private delegate bool Mutation(out AnimolTerrainSavedMap candidate, out string error);
        [SerializeField] private AnimolTerrainMapDocument sample;
        [SerializeField] private int entryIndex, tool, viewX = -4, viewY = -4, originX, originY;
        [SerializeField] private float cellPixels = 24;
        [SerializeField] private bool showMask = true;
        [SerializeField] private string selectedId;
        private AnimolTerrainCatalogData catalog;
        private string status;
        private Vector2 listScroll;

        [MenuItem("ANIMOL/Terrain Structure/Saved Map Editor")]
        public static void Open() { GetWindow<AnimolTerrainMapEditorWindow>("ANIMOL Terrain Map"); }
        public static void OpenForCatalogEntry(string id)
        {
            var window = GetWindow<AnimolTerrainMapEditorWindow>("ANIMOL Terrain Map");
            window.LoadCatalog();
            var map = window.Map;
            if (map != null) window.entryIndex = Array.FindIndex(window.catalog.entries.Where(e => e.themeId == map.themeId).ToArray(), e => e.id == id);
            window.Focus();
        }
        private void OnEnable() { wantsMouseMove = true; LoadCatalog(); Undo.undoRedoPerformed += Refresh; }
        private void OnDisable() { Undo.undoRedoPerformed -= Refresh; }
        private void Refresh() { Repaint(); SceneView.RepaintAll(); }
        private AnimolTerrainSavedMap Map { get { return AnimolTerrainMapEditorBridge.IsBound ? AnimolTerrainMapEditorBridge.Read() : sample == null ? null : sample.data; } }

        private void LoadCatalog()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/editor_tile_catalog.json");
            catalog = json == null ? null : JsonUtility.FromJson<AnimolTerrainCatalogData>(json.text);
            if (!AnimolTerrainPlacementEngine.ValidateCatalog(catalog, out string error)) { status = error; catalog = null; }
        }

        private void OnGUI()
        {
            if (catalog == null)
            {
                EditorGUILayout.HelpBox("editor_tile_catalog.json을 포함한 v3 폴더가 필요합니다.", MessageType.Error);
                if (GUILayout.Button("카탈로그 다시 읽기")) LoadCatalog();
                return;
            }
            if (AnimolTerrainMapEditorBridge.IsBound)
                EditorGUILayout.LabelField("게임 맵: " + AnimolTerrainMapEditorBridge.Label, EditorStyles.boldLabel);
            else
            {
                EditorGUILayout.HelpBox("현재 독립 저장 샘플입니다. 게임 맵은 CampaignMapEditorWindow에서 Bridge.Bind로 연결합니다.", MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    sample = (AnimolTerrainMapDocument)EditorGUILayout.ObjectField("저장 샘플", sample, typeof(AnimolTerrainMapDocument), false);
                    if (GUILayout.Button("새 맵", GUILayout.Width(70))) NewSample();
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("원본 아트 초기화")) Run(AnimolTerrainSourceArtBuilder.Initialize);
                if (GUILayout.Button("검증")) Run(() => { if (!AnimolTerrainPlacementEngine.Resolve(catalog, Map, out var unused, out string error)) throw new InvalidOperationException(error); status = "저장 데이터와 셀 점유 검증 완료."; });
                if (GUILayout.Button("JSON 내보내기")) Run(Export);
                using (new EditorGUI.DisabledScope(AnimolTerrainMapEditorBridge.IsBound))
                    if (GUILayout.Button("샘플 JSON 불러오기")) Run(Import);
                if (GUILayout.Button("미리보기 씬")) Run(() => AnimolTerrainMapPreview.Create(catalog, Map));
            }
            AnimolTerrainSavedMap map;
            try { map = Map; }
            catch (Exception ex) { EditorGUILayout.HelpBox(ex.Message, MessageType.Error); return; }
            if (map == null) return;
            if (!AnimolTerrainPlacementEngine.Resolve(catalog, map, out var initial, out string mapError))
            {
                EditorGUILayout.HelpBox(mapError + " 기존 저장은 변경하지 않습니다.", MessageType.Error);
                return;
            }
            int theme = Array.IndexOf(ThemeIds, map.themeId);
            using (new EditorGUI.DisabledScope(AnimolTerrainMapEditorBridge.IsBound || map.baseCells.Count != 0 || map.placements.Count != 0))
            {
                int next = EditorGUILayout.Popup("맵 테마", Mathf.Max(0, theme), ThemeLabels);
                if (theme < 0 || next != theme)
                {
                    AnimolTerrainSavedMap changed = AnimolTerrainPlacementEngine.CloneMap(map);
                    changed.themeId = ThemeIds[next]; changed.revision++;
                    Commit(changed, "Change ANIMOL Map Theme"); map = Map; entryIndex = 0;
                }
            }
            AnimolTerrainCatalogEntry[] entries = catalog.entries.Where(e => e.themeId == map.themeId).ToArray();
            if (entries.Length == 0) { EditorGUILayout.HelpBox("맵 테마에 해당하는 부품이 없습니다.", MessageType.Error); return; }
            entryIndex = Mathf.Clamp(entryIndex, 0, entries.Length - 1);
            entryIndex = EditorGUILayout.Popup("부품", entryIndex, entries.Select(e => e.id + " · " + e.role + " · " + e.width + "×" + e.height).ToArray());
            AnimolTerrainCatalogEntry entry = entries[entryIndex];
            int solid = Count(entry.solidRows);
            EditorGUILayout.LabelField(entry.kind == "InteriorOverlay"
                ? "범위 " + entry.width + "×" + entry.height + " = " + entry.width * entry.height + "셀   추가 충돌 0   필요한 기존 고체 " + Count(entry.supportRows)
                : "범위 " + entry.width + "×" + entry.height + " = " + entry.width * entry.height + "셀   고체 " + solid + "   빈 셀 " + (entry.width * entry.height - solid));
            EditorGUILayout.LabelField(entry.kind == "InteriorOverlay" ? "내부 질감: 기존 동일 스타일 지형 위에 배치하며 충돌을 추가하지 않습니다." : "완성 스프라이트: 고체 소유 셀은 부품 전체 선택으로 이동·삭제합니다.", EditorStyles.miniLabel);
            tool = GUILayout.Toolbar(tool, new[] { "덩어리 배치", "1×1 칠하기", "1×1 지우기", "부품 선택" });
            using (new EditorGUILayout.HorizontalScope())
            {
                originX = EditorGUILayout.IntField("배치/이동 X", originX);
                originY = EditorGUILayout.IntField("Y", originY);
                if (GUILayout.Button("배치", GUILayout.Width(65))) Place(entry.id, new Vector2Int(originX, originY));
                if (GUILayout.Button("선택 이동", GUILayout.Width(80))) Mutate((out AnimolTerrainSavedMap m, out string e) => AnimolTerrainPlacementEngine.TryMove(catalog, Map, selectedId, new Vector2Int(originX, originY), out m, out e), "Move ANIMOL Structure");
                if (GUILayout.Button("선택 삭제", GUILayout.Width(80))) Mutate((out AnimolTerrainSavedMap m, out string e) => AnimolTerrainPlacementEngine.TryDelete(catalog, Map, selectedId, out m, out e), "Delete ANIMOL Structure");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                viewX = EditorGUILayout.IntField("보기 원점 X", viewX);
                viewY = EditorGUILayout.IntField("Y", viewY);
                cellPixels = EditorGUILayout.Slider("격자 크기", cellPixels, 12, 40);
                showMask = EditorGUILayout.ToggleLeft("점유 표시", showMask, GUILayout.Width(90));
            }
            Rect canvas = GUILayoutUtility.GetRect(500, 420, GUILayout.ExpandWidth(true));
            DrawCanvas(canvas, entry);
            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.Height(105));
            foreach (AnimolTerrainPlacement p in Map.placements)
                if (GUILayout.Button((p.instanceId == selectedId ? "● " : "") + p.catalogId + " @ (" + p.x + ", " + p.y + ") · " + p.instanceId)) { selectedId = p.instanceId; originX = p.x; originY = p.y; }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField("초록: 고체 · 점선: 빈 셀 · 파랑: 장식 받침 · 노랑: 배치 원점 · 굵은 선: 16셀 청크", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
        }

        private static int Count(string[] rows) { return rows == null ? 0 : rows.Sum(row => row == null ? 0 : row.Count(c => c == '#')); }
        private void NewSample()
        {
            string path = EditorUtility.SaveFilePanelInProject("새 ANIMOL 저장 샘플", "ANIMOL_TerrainMap_Sample", "asset", "샘플 저장 위치");
            if (string.IsNullOrEmpty(path)) return;
            sample = CreateInstance<AnimolTerrainMapDocument>(); sample.data.themeId = "T01";
            AssetDatabase.CreateAsset(sample, path); AssetDatabase.SaveAssets(); selectedId = null; entryIndex = 0;
        }
        private void Run(Action action)
        {
            try { action(); } catch (Exception ex) { status = ex.Message; Debug.LogError("ANIMOL Terrain Map: " + ex.Message); }
            Refresh();
        }
        private void Commit(AnimolTerrainSavedMap candidate, string name)
        {
            if (!AnimolTerrainPlacementEngine.Resolve(catalog, candidate, out var unused, out string error)) throw new InvalidOperationException(error);
            if (AnimolTerrainMapEditorBridge.IsBound) AnimolTerrainMapEditorBridge.Commit(candidate, name);
            else
            {
                if (sample == null) throw new InvalidOperationException("저장 샘플을 먼저 선택하세요.");
                Undo.RecordObject(sample, name); sample.data = candidate; EditorUtility.SetDirty(sample);
            }
            status = "저장 데이터 갱신 · revision " + candidate.revision;
        }
        private void Mutate(Mutation mutation, string name)
        {
            Run(() => { if (!mutation(out AnimolTerrainSavedMap candidate, out string error)) throw new InvalidOperationException(error); Commit(candidate, name); });
        }
        private void Place(string id, Vector2Int origin) { Mutate((out AnimolTerrainSavedMap m, out string e) => AnimolTerrainPlacementEngine.TryPlace(catalog, Map, id, origin, out m, out e), "Place ANIMOL Structure"); }
        private void Export()
        {
            if (!AnimolTerrainPlacementEngine.Resolve(catalog, Map, out var unused, out string error)) throw new InvalidOperationException(error);
            string path = EditorUtility.SaveFilePanel("ANIMOL 셀/덩어리 내보내기", "", "ANIMOL_TerrainMap", "json");
            if (!string.IsNullOrEmpty(path)) File.WriteAllText(path, JsonUtility.ToJson(Map, true));
        }
        private void Import()
        {
            string path = EditorUtility.OpenFilePanel("저장 샘플 불러오기", "", "json");
            if (string.IsNullOrEmpty(path)) return;
            Commit(JsonUtility.FromJson<AnimolTerrainSavedMap>(File.ReadAllText(path)), "Import ANIMOL Terrain Sample");
        }

        private void DrawCanvas(Rect viewport, AnimolTerrainCatalogEntry selected)
        {
            Event ev = Event.current;
            int cols = Mathf.Max(1, Mathf.FloorToInt(viewport.width / cellPixels));
            int rows = Mathf.Max(1, Mathf.FloorToInt(viewport.height / cellPixels));
            bool inside = viewport.Contains(ev.mousePosition);
            Vector2Int hover = new Vector2Int(viewX + Mathf.FloorToInt((ev.mousePosition.x - viewport.x) / cellPixels), viewY + rows - 1 - Mathf.FloorToInt((ev.mousePosition.y - viewport.y) / cellPixels));
            if (!AnimolTerrainPlacementEngine.Resolve(catalog, Map, out AnimolTerrainResolveResult resolved, out string error))
            { EditorGUI.HelpBox(viewport, error, MessageType.Error); return; }
            GUI.BeginClip(viewport);
            EditorGUI.DrawRect(new Rect(0, 0, viewport.width, viewport.height), new Color(.10f, .11f, .17f));
            foreach (AnimolTerrainPlacement p in resolved.placements) DrawArt(resolved.entriesById[p.catalogId], p.x, p.y, rows);
            // Ordinary cells in an arch stay visible above the structure artwork.
            foreach (AnimolTerrainBaseCell c in Map.baseCells)
                EditorGUI.DrawRect(CellRect(c.x, c.y, rows), new Color(.34f, .42f, .53f, .9f));
            foreach (AnimolTerrainPlacement p in resolved.overlays) DrawArt(resolved.entriesById[p.catalogId], p.x, p.y, rows);
            if (showMask)
            {
                foreach (AnimolTerrainPlacement p in Map.placements) DrawMask(resolved.entriesById[p.catalogId], p.x, p.y, rows, .26f);
                foreach (AnimolTerrainCellResult c in resolved.solids.Values)
                    EditorGUI.DrawRect(CellRect(c.cell.x, c.cell.y, rows), new Color(.22f, .72f, .4f, .15f));
            }
            for (int x = 0; x <= cols; x++)
                EditorGUI.DrawRect(new Rect(x * cellPixels, 0, (viewX + x) % 16 == 0 ? 2 : 1, rows * cellPixels), new Color(.55f, .65f, .75f, .28f));
            for (int y = 0; y <= rows; y++)
                EditorGUI.DrawRect(new Rect(0, y * cellPixels, cols * cellPixels, (viewY + rows - y) % 16 == 0 ? 2 : 1), new Color(.55f, .65f, .75f, .28f));
            if (inside && tool == 0)
            {
                bool allowed = AnimolTerrainPlacementEngine.TryPlace(catalog, Map, selected.id, hover, out var ghostCandidate, out string reason);
                if (allowed && AnimolTerrainMapEditorBridge.IsBound)
                    try { AnimolTerrainMapEditorBridge.Validate?.Invoke(ghostCandidate); }
                    catch (Exception ex) { allowed = false; reason = ex.Message; }
                DrawMask(selected, hover.x, hover.y, rows, allowed ? .65f : .8f, !allowed);
                GUI.Label(new Rect(6, 4, viewport.width - 12, 40), allowed ? selected.id + " @ " + hover : reason, EditorStyles.whiteMiniLabel);
            }
            if (inside && tool != 0) EditorGUI.DrawRect(CellRect(hover.x, hover.y, rows), new Color(1, .8f, .46f, .35f));
            GUI.EndClip();
            if (inside && ev.type == EventType.MouseDown && ev.button == 0 && !ev.alt)
            {
                if (tool == 0) Place(selected.id, hover);
                else if (tool == 1 || tool == 2)
                    Mutate((out AnimolTerrainSavedMap m, out string e) => AnimolTerrainPlacementEngine.TrySetBaseCell(catalog, Map, hover, tool == 1, selected.styleId, out m, out e), "Edit ANIMOL 1x1 Cell");
                else
                {
                    AnimolTerrainPlacement picked = Map.placements.LastOrDefault(p => { var d = resolved.entriesById[p.catalogId]; return hover.x >= p.x && hover.y >= p.y && hover.x < p.x + d.width && hover.y < p.y + d.height; });
                    selectedId = picked == null ? null : picked.instanceId;
                    if (picked != null) { originX = picked.x; originY = picked.y; }
                }
                ev.Use(); Repaint();
            }
            if (inside && ev.type == EventType.MouseMove) Repaint();
        }
        private Rect CellRect(int x, int y, int visibleRows) { return new Rect((x - viewX) * cellPixels, (visibleRows - 1 - y + viewY) * cellPixels, cellPixels, cellPixels); }
        private void DrawMask(AnimolTerrainCatalogEntry e, int x, int y, int visibleRows, float alpha, bool reject = false)
        {
            for (int localY = 0; localY < e.height; localY++) for (int localX = 0; localX < e.width; localX++)
            {
                int row = e.height - 1 - localY;
                bool solid = e.solidRows[row][localX] == '#'; bool support = e.supportRows[row][localX] == '#';
                Rect r = CellRect(x + localX, y + localY, visibleRows);
                Color c = reject ? new Color(.85f, .2f, .32f, alpha) : support ? new Color(.25f, .65f, .96f, alpha) : solid ? new Color(.22f, .72f, .4f, alpha) : new Color(.6f, .7f, .78f, alpha);
                if (solid || support || reject) EditorGUI.DrawRect(r, c);
                else
                {
                    for (float pos = 0; pos < cellPixels; pos += 8)
                    {
                        EditorGUI.DrawRect(new Rect(r.x + pos, r.y, 4, 1), c); EditorGUI.DrawRect(new Rect(r.x + pos, r.yMax - 1, 4, 1), c);
                        EditorGUI.DrawRect(new Rect(r.x, r.y + pos, 1, 4), c); EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y + pos, 1, 4), c);
                    }
                }
            }
            Rect anchor = CellRect(x, y, visibleRows);
            EditorGUI.DrawRect(new Rect(anchor.x + 2, anchor.yMax - 6, 4, 4), new Color(1, .8f, .46f));
        }
        private void DrawArt(AnimolTerrainCatalogEntry entry, int x, int y, int visibleRows)
        {
            AnimolTerrainArtFrameDefinition frame = AssetDatabase.LoadAssetAtPath<AnimolTerrainArtFrameDefinition>(Root + "/Generated/SourceFrames/" + entry.frameId + ".asset");
            if (frame == null || frame.Sprite == null) return;
            Sprite sprite = frame.Sprite;
            float displayedHeight = sprite.rect.height / sprite.pixelsPerUnit * frame.UniformScale;
            Rect dest = new Rect((x - viewX) * cellPixels, (visibleRows + viewY - y - frame.VisualOffset.y - displayedHeight) * cellPixels, sprite.rect.width / sprite.pixelsPerUnit * frame.UniformScale * cellPixels, displayedHeight * cellPixels);
            Rect src = sprite.rect;
            Rect uv = new Rect(src.x / sprite.texture.width, src.y / sprite.texture.height, src.width / sprite.texture.width, src.height / sprite.texture.height);
            // Canvas is an original-pixel guide. The scene preview uses the palette material.
            GUI.DrawTextureWithTexCoords(dest, sprite.texture, uv, true);
            if (frame.Material != null && frame.Material.HasProperty("_HasExclude") && frame.Material.GetFloat("_HasExclude") > .5f)
            {
                Vector4 exclusion = frame.Material.GetVector("_ExcludeRect");
                float left = Mathf.Max(uv.xMin, exclusion.x), right = Mathf.Min(uv.xMax, exclusion.z);
                float bottom = Mathf.Max(uv.yMin, exclusion.y), top = Mathf.Min(uv.yMax, exclusion.w);
                if (right > left && top > bottom)
                    EditorGUI.DrawRect(new Rect(dest.x + (left - uv.xMin) / uv.width * dest.width, dest.y + (uv.yMax - top) / uv.height * dest.height, (right - left) / uv.width * dest.width, (top - bottom) / uv.height * dest.height), new Color(.10f, .11f, .17f));
            }
        }
    }
}
