using ANIMOL.Art.EchoRailCartV01;
using ANIMOL.Puzzle;
using NUnit.Framework;
using ANIMOL.Presentation.Editor;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Tests
{
    public sealed class EchoRailCartV01ArtTests
    {
        const string Package = "Assets/ANIMOL/Sprites/Environment/memory_theater/ANIMOL_ECHO_THEATER_RAIL_CART_V01";
        [System.Serializable] sealed class ArtSpec { public string key; public int width, height; public float pivotX, pivotY; }
        [System.Serializable] sealed class ArtManifest { public int installationRevision, spriteCount, clipCount; public ArtSpec[] sprites; }
        [System.Serializable] sealed class LoopExample { public bool closed; public Vector2Int[] cells; }

        [Test] public void GeneratedArtAndM06PrefabUseCurrentCart()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EchoRailCartArtCatalogV01>(
                Package + "/_GeneratedRailCartV01/EchoRailCartCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Validate(out var error), Is.True, error);
            var manifest = JsonUtility.FromJson<ArtManifest>(File.ReadAllText(Package + "/animation_catalog.json"));
            Assert.That(manifest.installationRevision, Is.EqualTo(2));
            Assert.That(manifest.spriteCount, Is.EqualTo(16));
            Assert.That(manifest.clipCount, Is.EqualTo(12));
            Assert.That(catalog.sprites.Length, Is.EqualTo(16));
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
                var spec = manifest.sprites.Single(s => s.key == entry.key);
                Assert.That(entry.sprite.rect.size, Is.EqualTo(new Vector2(spec.width, spec.height)), entry.key);
                Assert.That(entry.sprite.pixelsPerUnit, Is.EqualTo(256), entry.key);
                Assert.That(Vector2.Distance(entry.sprite.pivot,
                    new Vector2(spec.pivotX * spec.width, spec.pivotY * spec.height)), Is.LessThan(.01f), entry.key);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), entry.key);
                Assert.That(importer.wrapMode, Is.EqualTo(entry.key == "rail/material" ?
                    TextureWrapMode.Repeat : TextureWrapMode.Clamp), entry.key);
                Assert.That(importer.mipmapEnabled, Is.False, entry.key);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), entry.key);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), entry.key);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), entry.key);
                Assert.That(Vector2.Distance(settings.spritePivot,
                    new Vector2(spec.pivotX, spec.pivotY)), Is.LessThan(.0001f), entry.key);
                if (entry.key == "rail/material")
                {
                    Assert.That(entry.sprite.packed, Is.False);
                    Assert.That(importer.spritePackingTag, Is.Empty);
                }
            }
            foreach (var heading in new[] { "north", "east", "south", "west" })
            {
                Assert.That(catalog.TrySprite("cart/" + heading + "/idle", out var idle), Is.True);
                Assert.That(catalog.TrySprite("cart/" + heading + "/contact", out var contact), Is.True);
                Assert.That(idle, Is.Not.SameAs(contact));
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ANIMOL/Prefabs/Maps/EchoTheater/d06_moving_stage.prefab");
            var visual = prefab.GetComponentInChildren<EchoRailCartVisualV01>(true);
            Assert.That(visual, Is.Not.Null);
            Assert.That(prefab.GetComponent<ANIMOL.Presentation.DeviceVisualView>().enabled, Is.False);
            Assert.That(prefab.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(1));
            Assert.That(prefab.GetComponentInChildren<Animator>(true), Is.Null);
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
                "Assets/ANIMOL/Data/EchoTheater/Maps/ET-06_MapData.asset");
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

        [Test] public void ClosedLoopUsesEqualSleeperSpacingAcrossSeam()
        {
            var example = JsonUtility.FromJson<LoopExample>(File.ReadAllText(
                Package + "/Examples/R2_CLOSED_LOOP.json"));
            Assert.That(example.closed, Is.True);
            Assert.That(EchoRailPathV02.TryCreate(example.cells, true, out var path, out var error),
                Is.True, error);
            Assert.That(path.CellCount, Is.EqualTo(12));
            Assert.That(path.SleeperCount, Is.EqualTo(45));
            Assert.That(path.ActualSleeperSpacing * 256f, Is.EqualTo(63.3833f).Within(.01f));
            Assert.That(path.Length - path.SleeperDistance(path.SleeperCount - 1) +
                path.SleeperDistance(0), Is.EqualTo(path.ActualSleeperSpacing).Within(.0001f));
            path.Sample(0, out var first, out _);
            path.Sample(path.Length, out var last, out _);
            Assert.That(Vector2.Distance(first, last), Is.LessThan(.0001f));
        }

        static void Check(string expected, Vector2Int first, Vector2Int middle, Vector2Int last)
        {
            Assert.That(EchoRailCartRailTopologyV01.TryKey(
                new[] { first, middle, last }, 1, Vector2Int.right, out var key), Is.True);
            Assert.That(key, Is.EqualTo(expected));
        }
    }
}
