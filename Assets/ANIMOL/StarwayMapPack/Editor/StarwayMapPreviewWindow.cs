using UnityEditor;
using UnityEngine;

namespace ANIMOL.StarwayMaps.Editor
{
    public sealed class StarwayMapPreviewWindow : EditorWindow
    {
        private StarwayMapInput[] inputs;
        private string error;
        private int selected;
        private Vector2 focus;
        private bool drawLabels = true;

        [MenuItem("ANIMOL/Starway 20/3 Preview Maps and 7x11 Focus")]
        public static void Open()
        {
            StarwayMapPreviewWindow w = GetWindow<StarwayMapPreviewWindow>("Starway20 7x11");
            w.minSize = new Vector2(480, 680); w.Show();
        }

        private void OnEnable() { Reload(); }
        private void Reload()
        {
            try { inputs = StarwayPackCommands.LoadAndValidateInputs(); error = null; focus = CellVector(inputs[selected].map.start); }
            catch (System.Exception ex) { inputs = null; error = ex.Message; }
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Reload source data")) Reload();
            if (inputs == null) { EditorGUILayout.HelpBox(error ?? "No data", MessageType.Error); return; }
            string[] names = new string[inputs.Length];
            for (int i = 0; i < names.Length; i++) names[i] = inputs[i].map.id + "  " + inputs[i].map.title;
            int next = EditorGUILayout.Popup(selected, names);
            if (next != selected) { selected = next; focus = CellVector(inputs[selected].map.start); }
            StarwayMapInput input = inputs[selected]; StarwayMapDto m = input.map;
            EditorGUILayout.LabelField(m.difficultyName + "  |  " + m.size.width + " x " + m.size.height + "  |  reference " + input.referenceSuccessfulInputs + " inputs");
            EditorGUILayout.HelpBox("배치/카메라 미리보기입니다. 실제 게임 시뮬레이션이 아닙니다. 맵 클릭으로 초점을 옮깁니다.", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Focus Start")) focus = CellVector(m.start);
            if (GUILayout.Button("Focus Exit")) focus = CellVector(m.exit);
            drawLabels = GUILayout.Toggle(drawLabels, "Labels");
            EditorGUILayout.EndHorizontal();
            focus = EditorGUILayout.Vector2Field("Desired focus cell", focus);
            Vector2 clamped = StarwayCameraMath.ClampCenter(focus,
                StarwayCameraMath.CellBounds(m.regionCells, Vector2.zero, Vector2.one), new Vector2(7, 11));
            EditorGUILayout.LabelField("Visible cells: 7 x 11  |  clamped center: " + clamped.x.ToString("0.##") + ", " + clamped.y.ToString("0.##"));
            EditorGUILayout.LabelField("P start | E exit | B bubble | Y sheep | G grass | C chick | N nest");
            EditorGUILayout.LabelField("W wind | R reflector | > conveyor | = brake | D gate | T/O star");
            Rect area = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            float scale = Mathf.Min((area.width - 16) / m.size.width, (area.height - 16) / m.size.height);
            if (scale < 4f) return;
            Rect board = new Rect(area.center.x - m.size.width * scale * .5f, area.center.y - m.size.height * scale * .5f, m.size.width * scale, m.size.height * scale);
            EditorGUI.DrawRect(board, new Color(.07f, .10f, .15f));
            foreach (StarwayCell c in m.blockedCells) EditorGUI.DrawRect(CellRect(c, board, scale, m.size.height), new Color(.24f, .29f, .35f));
            foreach (StarwayCell c in m.walkableCells)
            {
                Rect r = CellRect(c, board, scale, m.size.height); r.width -= 1; r.height -= 1;
                EditorGUI.DrawRect(r, new Color(.77f, .86f, .72f));
            }
            foreach (StarwayPrototype d in m.devices)
            {
                string label = d.role == 24 ? "C" : d.role == 20 ? "W" : d.role == 18 ? "R" : d.role == 19 ? "=" : d.role == 9 ? "D" : d.role == 7 ? Arrow(d.direction) : d.variant == "socket" ? "O" : "T";
                Mark(d.position, label, new Color(.96f, .79f, .39f), board, scale, m.size.height);
                if (d.role == 24) Mark(d.target, "N", new Color(.88f, .69f, .92f), board, scale, m.size.height);
            }
            foreach (StarwayRanchObject d in m.ranchV13Art)
                Mark(d.position, d.family == "sheep" ? "Y" : "G", d.family == "sheep" ? Color.white : new Color(.24f, .60f, .30f), board, scale, m.size.height);
            foreach (StarwaySluice d in m.ranchSluiceDesign)
            {
                Mark(d.position, "A", d.rightState ? new Color(.27f, .63f, .87f) : new Color(.69f, .58f, .46f), board, scale, m.size.height);
                Mark(d.pondB, "B", !d.rightState ? new Color(.27f, .63f, .87f) : new Color(.69f, .58f, .46f), board, scale, m.size.height);
            }
            foreach (StarwayCell b in m.bubbles)
            {
                Rect r = CellRect(b, board, scale, m.size.height);
                EditorGUI.DrawRect(new Rect(r.xMax - scale * .32f, r.y + 2, scale * .28f, scale * .28f), new Color(.18f, .42f, .95f));
            }
            Mark(m.start, "P", new Color(.30f, .89f, .75f), board, scale, m.size.height);
            Mark(m.exit, "E", new Color(.97f, .57f, .46f), board, scale, m.size.height);
            Rect frame = new Rect(board.x + (clamped.x - 3.5f + .5f) * scale,
                board.y + (m.size.height - .5f - clamped.y - 5.5f) * scale, 7 * scale, 11 * scale);
            DrawFrame(frame, new Color(.03f, .81f, .95f));
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && board.Contains(Event.current.mousePosition))
            {
                focus = new Vector2((Event.current.mousePosition.x - board.x) / scale - .5f,
                    m.size.height - .5f - (Event.current.mousePosition.y - board.y) / scale);
                Event.current.Use(); Repaint();
            }
        }

        private void Mark(StarwayCell c, string label, Color color, Rect board, float scale, int height)
        {
            Rect r = CellRect(c, board, scale, height); r.x += 2; r.y += 2; r.width -= 4; r.height -= 4;
            EditorGUI.DrawRect(r, color);
            if (drawLabels && scale >= 14)
            {
                GUIStyle s = new GUIStyle(EditorStyles.miniLabel); s.alignment = TextAnchor.MiddleCenter; s.normal.textColor = Color.black;
                GUI.Label(r, label, s);
            }
        }
        private static string Arrow(StarwayCell d) { return d.x > 0 ? ">" : d.x < 0 ? "<" : d.y > 0 ? "^" : "v"; }
        private static Vector2 CellVector(StarwayCell c) { return new Vector2(c.x, c.y); }
        private static Rect CellRect(StarwayCell c, Rect board, float scale, int height)
        { return new Rect(board.x + c.x * scale, board.y + (height - 1 - c.y) * scale, scale, scale); }
        private static void DrawFrame(Rect r, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 2), c); EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2, r.width, 2), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 2, r.height), c); EditorGUI.DrawRect(new Rect(r.xMax - 2, r.y, 2, r.height), c);
        }
    }
}
