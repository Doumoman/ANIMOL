using System;
using System.Linq;
using ANIMOL.Core;
using ANIMOL.NamedArt.Editor;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    internal static class ObstacleNamedArtBindings
    {
        public static void Refresh()
        {
            var index=NamedArtIndex.Read(NamedArtReferences.IndexPath);
            string artRoot=ObstacleArtV1Builder.Root+"/Art/";
            var sources=index.Sources.Where(s=>s.Path.StartsWith(artRoot,StringComparison.Ordinal)).Select(s=>s.Key).ToHashSet();
            var retired=CommonObstacleV6Builder.Root+"/C09.asset";
            // Replace bindings owned by this generator; retain every other container.
            if(index.Containers.Any(c=>c.Path!=ObstacleArtV1Builder.RegistryPath&&!c.Path.StartsWith(CommonObstacleV6Builder.Root+"/",StringComparison.Ordinal)&&c.Slots.Any(s=>sources.Contains(s.Key))))
                throw new InvalidOperationException("An external container still references retired obstacle graphics; explicit migration is required.");
            index.Sources.RemoveAll(s=>s.Path.StartsWith(artRoot,StringComparison.Ordinal));
            index.Containers.RemoveAll(c=>c.Path==retired);
            foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{ObstacleArtV1Builder.Root+"/Art"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);index.CaptureImporter(path);
                foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))index.Register(asset);
            }
            NamedArtReferences.CaptureContainer(index,ObstacleArtV1Builder.RegistryPath,true);
            foreach(var kind in CommonObstacleCatalog.ActiveKinds)NamedArtReferences.CaptureContainer(index,CommonObstacleV6Builder.Root+"/"+CommonObstacleCatalog.Id(kind)+".asset",true);
            index.Write(NamedArtReferences.IndexPath);
        }
    }
}
