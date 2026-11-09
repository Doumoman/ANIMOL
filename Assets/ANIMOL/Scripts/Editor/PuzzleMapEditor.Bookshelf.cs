using System.Linq;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    public sealed partial class PuzzleMapEditor
    {
        [SerializeField] bool showBookshelfRotationArea = true;

        public static bool BookshelfPreviewBlocked(PuzzleMap data, PrototypeDevice shelf, Vector2Int cell)
        {
            if (!data.Contains(cell) || data.DecorationAt(cell) ||
                data.TryTerrain(cell, out var terrain) && terrain.blocked || data.start == cell ||
                data.DeviceAt(cell).kind != DeviceKind.None) return true;
            if (!data.scenario) return false;
            return data.scenario.devices.Any(d => d != shelf &&
                (d.role == PrototypeRole.VineBridge && d.variant != "plate" && d.cells.Count > 0
                    ? d.cells : InitialDeviceFootprint(d)).Contains(cell)) ||
                data.scenario.ranchV13Art.Any(a => a != null && a.Footprint().Contains(cell));
        }

        void DrawBookshelfRotationArea(PrototypeDevice device, int shownHeight, Vector2 origin)
        {
            foreach (var p in BookshelfRotationRules.SweptCells(device.position, device.orientation, device.bookshelfRotation))
            {
                var blocked = BookshelfPreviewBlocked(map, device, p);
                var rect = new Rect(origin.x + p.x * cellSize, origin.y + (shownHeight - p.y - 1) * cellSize, cellSize, cellSize);
                Handles.DrawSolidRectangleWithOutline(rect, blocked ? new Color(1, .15f, .1f, .25f) : new Color(1, .7f, .15f, .15f),
                    blocked ? new Color(1, .2f, .15f, .9f) : new Color(1, .75f, .2f, .75f));
            }
            var pivot = new Vector3(origin.x + (device.position.x + .5f) * cellSize,
                origin.y + (shownHeight - device.position.y - .5f) * cellSize);
            var clockwise = device.bookshelfRotation == BookshelfRotationDirection.Clockwise;
            var start = device.orientation % 2 == 0 ? Vector3.right : Vector3.up;
            var previousColor = Handles.color;
            Handles.color = new Color(1, .85f, .3f);
            Handles.DrawWireArc(pivot, Vector3.forward, start, clockwise ? 90 : -90, cellSize * .8f);
            Handles.color = previousColor;
            GUI.Label(new Rect(pivot.x - cellSize * .5f, pivot.y - cellSize * 1.35f, cellSize * 2, 18),
                clockwise ? "회전 ↻" : "회전 ↺", EditorStyles.whiteMiniLabel);
        }
    }
}
