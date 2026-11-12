using System;
using ANIMOL.Art.EditorValidation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ANIMOL.Presentation.Tests
{
    public sealed class ArtCatalogBuildValidationTests
    {
        [Test] public void AllNineRelocatedCatalogsRetainRuntimeIntegrityAndSpriteIdentity()
        {
            foreach (string type in new[] { "RanchConnectedDecorV01Catalog", "RanchConnectedDecorV02Catalog",
                "TheaterConnectedDecorV01Catalog", "SanctumConnectedDecorV01Catalog", "CoastConnectedDecorV01Catalog",
                "StarwayRanchV12VisualCatalog", "EchoTheaterV03VisualCatalog", "SunsetSanctumV02VisualCatalog", "StarSwellCoastV01VisualCatalog" })
            {
                var guids = AssetDatabase.FindAssets("t:" + type);
                Assert.That(guids.Length, Is.EqualTo(1), type);
                var catalog = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]));
                var runtime = catalog.GetType();
                runtime.GetMethod("Invalidate", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.Invoke(catalog, null);
                object[] args = { runtime.GetProperty("SourceManifestSha256").GetValue(catalog), null };
                bool valid = (bool)runtime.GetMethod("ValidateIntegrity").Invoke(catalog, args);
                Assert.DoesNotThrow(() => ArtCatalogBuildValidation.Validate(catalog, valid, args[1] as string), type);
            }
        }
        [Test] public void ChangedSpriteIdentityAndRuntimeFailureStillRejectBuilds()
        {
            var id = AssetDatabase.FindAssets("t:RanchConnectedDecorV01Catalog")[0];
            var copy = Object.Instantiate(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(id)));
            try
            {
                Assert.Throws<BuildFailedException>(() => ArtCatalogBuildValidation.Validate(copy, false, "fixture runtime failure"));
                using (var data = new SerializedObject(copy))
                {
                    data.FindProperty("sprites").GetArrayElementAtIndex(0).FindPropertyRelative("assetGuid").stringValue = new string('0', 32);
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                Assert.Throws<BuildFailedException>(() => ArtCatalogBuildValidation.Validate(copy, true, null));
            }
            finally { Object.DestroyImmediate(copy); }
        }
    }
}
