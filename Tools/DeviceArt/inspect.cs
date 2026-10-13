var path="Assets/ANIMOL/Sprites/Obstacles/M07_Conveyor/Frames/spr_obstacle_common_m07_conveyor_idle_e.png";
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
var sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
return new {importer.spritePixelsPerUnit,importer.filterMode,importer.textureCompression,importer.spritePivot,sprite.rect,sprite.pivot};
