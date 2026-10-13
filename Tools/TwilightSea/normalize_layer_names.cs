using System;
using System.IO;
using UnityEngine;
using UnityEditor;
public static class SeaLayerNames
{
    public static string Main()
    {
        var lines=File.ReadAllLines("Docs/Validation/TwilightSea/layer_name_moves.tsv");
        foreach(var line in lines)
        {
            var move=line.Split('\t');string from=move[0],to=move[1],guid=move[2];
            if(AssetDatabase.AssetPathToGUID(from)!=guid)throw new Exception("Unexpected source GUID "+from);
            string error=AssetDatabase.MoveAsset(from,to);if(error!="")throw new Exception(error);
            if(AssetDatabase.AssetPathToGUID(to)!=guid)throw new Exception("GUID changed "+to);
        }
        AssetDatabase.SaveAssets();return $"Moved {lines.Length} layer assets; every GUID preserved; direction suffix last";
    }
}
