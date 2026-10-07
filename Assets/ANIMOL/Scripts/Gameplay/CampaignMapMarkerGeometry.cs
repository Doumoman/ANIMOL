using ANIMOL.Core;
using ANIMOL.Gameplay.ObstacleGraphics;
using UnityEngine;

namespace ANIMOL.Gameplay
{
    // Authored coordinates are cell origins. START/CP safe positions describe feet,
    // while triggers and artwork use cell centres. No runtime position is saved back.
    public static class CampaignMapMarkerGeometry
    {
        public static Vector2 ContactSize(StageMapObjectPlacement marker) => marker.Kind == StageMapObjectKind.Exit
            ?new Vector2(1.6f,1.8f):new Vector2(.9f*marker.Settings.FootprintCells.x,marker.Settings.FootprintCells.y);
        public static Vector3 Centre(StageMapDefinition map,StageMapObjectPlacement marker) =>
            new Vector3(marker.X+marker.Settings.FootprintCells.x*.5f,marker.Y+marker.Settings.FootprintCells.y*.5f,0)*map.WorldUnitsPerCell;
        public static Vector3 Feet(StageMapDefinition map, StageMapObjectPlacement marker) =>
            (new Vector3(marker.X + .5f, marker.Y, 0) +
             (Vector3)marker.Settings.CheckpointRespawnOffsetCells) * map.WorldUnitsPerCell;

        public static Vector3 BodyAtFeet(DevPlayerController player, Vector3 feet)
        {
            Physics2D.SyncTransforms();
            var collider = player.GetComponent<Collider2D>();
            if (collider is BoxCollider2D box)
            {
                var localBottom = box.offset - Vector2.up * box.size.y * .5f;
                return feet + Vector3.up * (player.transform.position.y - player.transform.TransformPoint(localBottom).y);
            }
            return feet + Vector3.up * (player.transform.position.y - collider.bounds.min.y);
        }

        public static void BuildVisual(GameObject root, StageMapObjectKind kind, float units)
        {
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ObstacleArtRegistry.Load().Marker(kind switch
            {
                StageMapObjectKind.BubbleCandidate => "bell_idle_1",
                StageMapObjectKind.Exit => "goal_locked_1",
                _ => "start_idle_1"
            });
            renderer.sortingOrder = 40;
            root.transform.localScale = Vector3.one * units;
            if (kind == StageMapObjectKind.Checkpoint) renderer.color = new Color(.45f, .9f, 1f);
        }
    }
}
