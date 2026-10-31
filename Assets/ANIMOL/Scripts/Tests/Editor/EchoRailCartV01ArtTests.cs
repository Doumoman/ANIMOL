using ANIMOL.Art.EchoRailCartV01;
using ANIMOL.Puzzle;
using NUnit.Framework;
using ANIMOL.Presentation.Editor;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class EchoRailCartV01ArtTests
    {
        const string Package = "Assets/ANIMOL/Sprites/Environment/memory_theater/ANIMOL_ECHO_THEATER_RAIL_CART_V01";

        [Test] public void GeneratedArtAndM06PrefabUseCurrentCart()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EchoRailCartArtCatalogV01>(
                Package + "/_GeneratedRailCartV01/EchoRailCartCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Validate(out var error), Is.True, error);
            Assert.That(catalog.sprites.Length, Is.EqualTo(14));
            Assert.That(catalog.clips.Length, Is.EqualTo(12));
            foreach (var clipEntry in catalog.clips)
            {
                Assert.That(AssetDatabase.GetAssetPath(clipEntry.clip),
                    Does.StartWith(Package + "/_GeneratedRailCartV01/"), clipEntry.key);
                if (!clipEntry.key.EndsWith("/move")) continue;
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clipEntry.clip);
                Assert.That(bindings.Length, Is.EqualTo(1), clipEntry.key);
                var frames = AnimationUtility.GetObjectReferenceCurve(clipEntry.clip, bindings[0]);
                Assert.That(System.Linq.Enumerable.Distinct(
                    System.Linq.Enumerable.Select(frames, f => f.value)).Count(), Is.EqualTo(1), clipEntry.key);
            }
            var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/memory_theater_Theme.asset");
            Assert.That(theme.echoRailCartV01Catalog, Is.SameAs(catalog));

            foreach (var entry in catalog.sprites)
            {
                var path = AssetDatabase.GetAssetPath(entry.sprite);
                Assert.That(path, Does.StartWith(Package + "/Sprites/"));
                Assert.That(entry.sprite.rect.size, Is.EqualTo(new Vector2(320, 320)), entry.key);
                Assert.That(entry.sprite.pixelsPerUnit, Is.EqualTo(256), entry.key);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), entry.key);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), entry.key);
                Assert.That(importer.mipmapEnabled, Is.False, entry.key);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), entry.key);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), entry.key);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), entry.key);
                Assert.That(settings.spritePivot, Is.EqualTo(new Vector2(.5f, .5f)), entry.key);
            }
            foreach (var heading in new[] { "north", "east", "south", "west" })
            {
                Assert.That(catalog.TrySprite("cart/" + heading + "/idle", out var idle), Is.True);
                Assert.That(catalog.TrySprite("cart/" + heading + "/contact", out var contact), Is.True);
                Assert.That(idle, Is.Not.SameAs(contact));
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ANIMOL/Prefabs/Themes/memory_theater/d06_moving_stage.prefab");
            var visual = prefab.GetComponentInChildren<EchoRailCartVisualV01>(true);
            Assert.That(visual, Is.Not.Null);
            Assert.That(prefab.GetComponent<ANIMOL.Presentation.DeviceVisualView>().enabled, Is.False);
            Assert.That(visual.GetComponent<SpriteRenderer>().sprite,
                Is.SameAs(System.Array.Find(catalog.sprites, s => s.key == "cart/east/idle").sprite));
            Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [Test] public void OrderedRouteSelectsAllSixUnrotatedRailShapes()
        {
            Check("ew", new Vector2Int(-1, 0), Vector2Int.zero, Vector2Int.right);
            Check("ns", Vector2Int.down, Vector2Int.zero, Vector2Int.up);
            Check("ne", Vector2Int.up, Vector2Int.zero, Vector2Int.right);
            Check("es", Vector2Int.right, Vector2Int.zero, Vector2Int.down);
            Check("sw", Vector2Int.down, Vector2Int.zero, Vector2Int.left);
            Check("wn", Vector2Int.left, Vector2Int.zero, Vector2Int.up);
        }

        [Test] public void MapEditorShowsCatalogCartForExistingM06()
        {
            var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/MapData/memory_theater/ET-06_MapData.asset");
            var stage = System.Linq.Enumerable.First(map.scenario.devices,
                d => d.role == PrototypeRole.MovingStage);
            var window = ScriptableObject.CreateInstance<PuzzleMapEditor>();
            try
            {
                typeof(PuzzleMapEditor).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(window, map);
                var sprite = (Sprite)typeof(PuzzleMapEditor).GetMethod("DeviceSprite",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(window, new object[] { stage });
                Assert.That(sprite, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(sprite), Does.Contain("/ANIMOL_ECHO_THEATER_RAIL_CART_V01/"));
                Assert.That(PuzzleMapEditor.GridDeviceLabel(stage), Does.Contain("카트"));
            }
            finally { Object.DestroyImmediate(window); }
        }

        static void Check(string expected, Vector2Int first, Vector2Int middle, Vector2Int last)
        {
            Assert.That(EchoRailCartRailTopologyV01.TryKey(
                new[] { first, middle, last }, 1, Vector2Int.right, out var key), Is.True);
            Assert.That(key, Is.EqualTo(expected));
        }
    }
}
