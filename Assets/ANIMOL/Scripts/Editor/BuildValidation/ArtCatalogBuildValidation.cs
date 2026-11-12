using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ANIMOL.Art.EditorValidation
{
    /// <summary>Builds validate current serialized assets after the 2026-11-03 art reorganization.
    /// Original-package manifest/path checks remain in the authoring builders.</summary>
    public static class ArtCatalogBuildValidation
    {
        public static void Validate(Object catalog, bool runtimeValid, string runtimeError)
        {
            if (!catalog || !runtimeValid)
                throw new BuildFailedException("Invalid runtime art catalog: " + runtimeError);
            using (var data = new SerializedObject(catalog))
            {
                var sprites = data.FindProperty("sprites");
                if (sprites == null || !sprites.isArray || sprites.arraySize == 0)
                    throw new BuildFailedException("Art catalog has no serialized sprites: " + catalog.name);
                for (int i = 0; i < sprites.arraySize; i++)
                {
                    var row = sprites.GetArrayElementAtIndex(i);
                    var sprite = row.FindPropertyRelative("sprite")?.objectReferenceValue as Sprite;
                    var expectedGuid = row.FindPropertyRelative("assetGuid")?.stringValue;
                    var expectedId = row.FindPropertyRelative("localFileId");
                    if (!sprite || expectedId == null ||
                        !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long fileId) ||
                        !string.Equals(guid, expectedGuid, StringComparison.Ordinal) || fileId != expectedId.longValue)
                        throw new BuildFailedException("Sprite GUID/fileID mismatch: " + catalog.name + " [" + i + "]");
                    string path = AssetDatabase.GetAssetPath(sprite);
                    if (!path.StartsWith("Assets/ANIMOL/Art/", StringComparison.Ordinal) ||
                        AssetDatabase.LoadAssetAtPath<Sprite>(path) != sprite)
                        throw new BuildFailedException("Sprite is outside the active art layout: " + path);
                }
            }
        }
    }
}
