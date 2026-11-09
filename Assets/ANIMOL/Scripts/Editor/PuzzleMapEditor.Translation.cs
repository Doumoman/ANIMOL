using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    public sealed partial class PuzzleMapEditor
    {
        bool moveWholeMap, mapMoveStroke;
        int mapMoveUndoGroup, mapMoveControl;
        Vector2 mapMoveLastPointer;
        double nextMapMoveRepeat;
        Rect mapMoveViewport;
        Vector2 mapMoveGridOrigin;
        int mapMoveShownHeight;

        void SetMapMoveMode(bool enabled)
        {
            EndStroke(); EndMapMove(); moveWholeMap = enabled;
            linkTargetMode = false;
            if (Event.current != null) GUI.FocusControl(null);
            Repaint();
        }
        void EndMapMove()
        {
            if (mapMoveStroke) Undo.CollapseUndoOperations(mapMoveUndoGroup);
            ResetMapMoveGesture();
        }
        void ResetMapMoveGesture()
        {
            mapMoveStroke = false;
            if (mapMoveControl != 0 && GUIUtility.hotControl == mapMoveControl) GUIUtility.hotControl = 0;
            mapMoveControl = 0;
        }
        void MoveSelectedMap(Vector2Int requested)
        {
            if (PuzzleMapTranslation.ClampDelta(map, requested) == Vector2Int.zero) return;
            if (!mapMoveStroke)
            {
                EndStroke(); Undo.IncrementCurrentGroup();
                mapMoveUndoGroup = Undo.GetCurrentGroup(); mapMoveStroke = true;
            }
            var delta = PuzzleMapTranslation.Move(map, requested);
            cameraPreviewCell += delta; selectedDecorationCell += delta;
            previewCacheValid = false; cachedValidationErrors = null;
            legacyDeviceSprites.Clear(); Repaint();
        }
        void DrawMapMoveToolbar()
        {
            var available = PuzzleMapTranslation.Available(map, out var reason);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var enabled = GUILayout.Toggle(moveWholeMap, new GUIContent("전체 선택/이동", "Ctrl+A로 전체 선택, Esc로 해제"), EditorStyles.toolbarButton);
                if (enabled != moveWholeMap) SetMapMoveMode(enabled);
                using (new EditorGUI.DisabledScope(!moveWholeMap || !available))
                {
                    MapMoveRepeat("←", Vector2Int.left); MapMoveRepeat("↓", Vector2Int.down);
                    MapMoveRepeat("↑", Vector2Int.up); MapMoveRepeat("→", Vector2Int.right);
                }
            }
            if (moveWholeMap && !available) EditorGUILayout.HelpBox(reason, MessageType.Warning);
        }
        void MapMoveRepeat(string label, Vector2Int direction)
        {
            using (new EditorGUI.DisabledScope(PuzzleMapTranslation.ClampDelta(map, direction) == Vector2Int.zero))
                if (GUILayout.RepeatButton(label, EditorStyles.toolbarButton, GUILayout.Width(32)))
                {
                    Repaint();
                    if (Event.current.rawType == EventType.MouseDown ||
                        Event.current.type == EventType.Repaint && EditorApplication.timeSinceStartup >= nextMapMoveRepeat)
                    {
                        MoveSelectedMap(direction); nextMapMoveRepeat = EditorApplication.timeSinceStartup + .12;
                    }
                }
            // RepeatButton needs repaints while held, including when it has reached the edge.
            if (Event.current.rawType == EventType.MouseDown || GUIUtility.hotControl != 0) Repaint();
        }
        bool HandleMapMoveKeyboard(Event e)
        {
            if (!map || EditorGUIUtility.editingTextField) return false;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.A && (e.control || e.command) ||
                (e.type == EventType.ExecuteCommand || e.type == EventType.ValidateCommand) && e.commandName == "SelectAll")
            {
                if (e.type != EventType.ValidateCommand) SetMapMoveMode(true);
                e.Use(); return true;
            }
            if (!moveWholeMap || e.type != EventType.KeyDown) return false;
            if (e.keyCode == KeyCode.Escape) { SetMapMoveMode(false); e.Use(); return true; }
            var delta = e.keyCode == KeyCode.LeftArrow ? Vector2Int.left : e.keyCode == KeyCode.RightArrow ? Vector2Int.right :
                e.keyCode == KeyCode.UpArrow ? Vector2Int.up : e.keyCode == KeyCode.DownArrow ? Vector2Int.down : Vector2Int.zero;
            if (delta == Vector2Int.zero) return false;
            MoveSelectedMap(delta); e.Use(); return true;
        }
        void DrawMapMoveSelection(Vector2 origin, int shownHeight)
        {
            if (!moveWholeMap || Event.current.type != EventType.Repaint) return;
            var b = PuzzleMapTranslation.Bounds(map);
            Handles.DrawSolidRectangleWithOutline(new Rect(origin.x + b.xMin * cellSize,
                origin.y + (shownHeight - b.yMax) * cellSize, b.width * cellSize, b.height * cellSize),
                new Color(.1f, .7f, 1f, .06f), new Color(.15f, .8f, 1f));
        }
        void HandleMapMovePointer(Event e, bool inside, Vector2 origin, int shownHeight, Vector2 windowPointer)
        {
            var control = GUIUtility.GetControlID("WholeMapMove".GetHashCode(), FocusType.Passive);
            if (!moveWholeMap) return;
            var cell = new Vector2Int(Mathf.FloorToInt((e.mousePosition.x - origin.x) / cellSize),
                shownHeight - 1 - Mathf.FloorToInt((e.mousePosition.y - origin.y) / cellSize));
            if (e.type == EventType.MouseDown && inside && e.button == 0 && map.Contains(cell))
            {
                EndMapMove(); GUI.FocusControl(null); mapMoveLastPointer = windowPointer;
                mapMoveControl = control; GUIUtility.hotControl = control; e.Use();
            }
            else if (e.type == EventType.MouseDrag && mapMoveControl != 0 && GUIUtility.hotControl == mapMoveControl)
            {
                // Window coordinates stay stable even when camera preview padding changes after a move.
                var travel = windowPointer - mapMoveLastPointer;
                var delta = new Vector2Int(Mathf.RoundToInt(travel.x / cellSize), Mathf.RoundToInt(-travel.y / cellSize));
                MoveSelectedMap(delta);
                // Consume blocked cursor travel as well, so reversing at the edge responds immediately.
                mapMoveLastPointer += new Vector2(delta.x, -delta.y) * cellSize; e.Use();
            }
            else if (e.rawType == EventType.MouseUp && mapMoveControl != 0)
            { EndMapMove(); e.Use(); }
            else if (inside && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
                e.Use(); // Selection mode never paints or erases individual cells.
        }
    }
}
