foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/ANIMOL/Sprites/Obstacles"}))
    UnityEditor.AssetDatabase.ImportAsset(UnityEditor.AssetDatabase.GUIDToAssetPath(guid),UnityEditor.ImportAssetOptions.ForceUpdate);
return "Reimported device textures under registered policy";
