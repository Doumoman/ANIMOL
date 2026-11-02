using System.Collections;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ANIMOL.Presentation.Tests
{
    public class CoastRetouchPlayTests
    {
        const string CoastCatalog = "Assets/ANIMOL/Data/Art/TwilightSeaCharacters.asset";

        [UnityTest] public IEnumerator ActualCoastPrototypeKeepsFourCharactersAndMoveCost()
        {
#if UNITY_EDITOR
            var map = UnityEditor.AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/twilight_sea.asset");
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CoastCatalog);
            var host = new GameObject("Coast prototype QA");
            var controller = host.AddComponent<PuzzlePrototypeController>();
            try
            {
                controller.Load(map);
                var visual = controller.CharacterVisual;
                Assert.That(visual, Is.Not.Null);
                Assert.That(visual.characterId, Is.EqualTo("haerin"));
                Assert.That(visual.GetComponents<SpriteRenderer>().Length, Is.EqualTo(1));
                Assert.That(visual.GetComponents<Animator>(), Is.Empty);
                foreach (var entry in catalog.characters)
                {
                    Assert.That(controller.SelectTwilightSeaCharacter(entry.id), Is.True, entry.id);
                    Assert.That(visual.CoastPushPresenter.isActiveAndEnabled, Is.True, entry.id);
                    Assert.That(visual.PlayPose("left", "push"), Is.True, entry.id);
                    Assert.That(visual.visual.flipX, Is.True, entry.id);
                }
                Assert.That(controller.SelectTwilightSeaCharacter("eden"), Is.True);
                Assert.That(controller.SelectTwilightSeaCostume("puppy"), Is.True);
                Assert.That(visual.characterId, Is.EqualTo("eden"));
                Assert.That(visual.costumeId, Is.EqualTo("puppy"));
                Assert.That(visual.CoastPushPresenter.isActiveAndEnabled, Is.True);
                Vector2Int chosen = Vector2Int.zero;
                PuzzleDeviceSession expected = null;
                foreach (var direction in new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
                {
                    var candidate = new PuzzleDeviceSession(map);
                    if (!candidate.Apply(direction).accepted) continue;
                    chosen = direction;
                    expected = candidate;
                    break;
                }
                Assert.That(chosen, Is.Not.EqualTo(Vector2Int.zero));
                Assert.That(controller.RequestMove(chosen), Is.True);
                var deadline = Time.realtimeSinceStartup + 5f;
                while (controller.IsMoving && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(controller.IsMoving, Is.False);
                Assert.That(controller.Session.Cell, Is.EqualTo(expected.Cell));
                Assert.That(controller.Session.Cost, Is.EqualTo(expected.Cost));
            }
            finally { Object.Destroy(host); }
#else
            Assert.Ignore("Requires Unity Editor asset loader."); yield break;
#endif
        }

        [UnityTest] public IEnumerator CoastCostumesAndFourThemesSelectOnePresenterAndRestoreRest()
        {
#if UNITY_EDITOR
            var coast = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CoastCatalog);
            var starway = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(
                "Assets/ANIMOL/Data/Art/StarwayCharacters.asset");
            var theater = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(
                "Assets/ANIMOL/Data/Art/MemoryTheaterCharacters.asset");
            var sanctum = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(
                "Assets/ANIMOL/Data/Art/LetterObservatoryCharacters.asset");
            var host = new GameObject("Coast PUSH theme QA");
            var contact = new GameObject("Coast visual contact").transform;
            try
            {
                var visual = StarwayCharacterVisual.Create(host.transform, Vector2.zero, 40, coast, "haerin");
                foreach (var set in new[] { "haerin", "stranger", "danu", "eden", "edenpuppy" })
                {
                    var id = set == "edenpuppy" ? "eden" : set;
                    var costume = set == "edenpuppy" ? "puppy" : set == "eden" ? "rabbit" : "default";
                    Assert.That(visual.Initialize(coast, id, costume), Is.True, set);
                    var entry = coast.Find(id);
                    var appearance = set == "edenpuppy" ? entry.costumes[0] : null;
                    var contacts = entry.CoastContactsFor(costume);
                    var rest = appearance != null ? appearance.restingLocalPosition : entry.restingLocalPosition;
                    var scaleX = appearance != null ? appearance.coastGraphicsScaleXFactor : entry.coastGraphicsScaleXFactor;
                    Assert.That(visual.CoastPushPresenter.isActiveAndEnabled, Is.True, set);
                    Assert.That(visual.PushPresenter.isActiveAndEnabled, Is.False, set);
                    Assert.That(visual.TheaterPushPresenter == null || !visual.TheaterPushPresenter.isActiveAndEnabled, Is.True);
                    Assert.That(visual.SanctumPushPresenter == null || !visual.SanctumPushPresenter.isActiveAndEnabled, Is.True);
                    Assert.That(visual.transform.localPosition, Is.EqualTo(rest), set);
                    Assert.That(visual.transform.localScale.x, Is.EqualTo(.85f * scaleX).Within(.0001f), set);
                    foreach (var direction in new[] { "front", "back", "side", "left" })
                    foreach (var state in new[] { "idle", "move", "sliding", "jump", "victory", "push" })
                    {
                        Assert.That(visual.PlayPose(direction, state), Is.True, set + "/" + direction + "/" + state);
                        Assert.That(visual.visual.flipX, Is.EqualTo(direction == "left"));
                        if (state == "push") Assert.That(contacts.TryGet(visual.visual.sprite, out _), Is.True);
                    }
                }
                Assert.That(visual.Initialize(coast, "eden", "rabbit"), Is.True);
                visual.SetMotion(Vector2Int.right, "push");
                var rabbit = coast.Find("eden").CoastContactsFor("rabbit");
                Assert.That(rabbit.TryGet(visual.visual.sprite, out var anchor), Is.True);
                contact.position = visual.visual.transform.TransformPoint(anchor.handLocal) + Vector3.right * .2f;
                visual.BeginPushPresentation(null, Vector2.right, contact);
                Assert.That(visual.PushPoseActive, Is.True);
                Assert.That(visual.SetCostume("puppy"), Is.True);
                Assert.That(visual.costumeId, Is.EqualTo("puppy"));
                Assert.That(visual.transform.localPosition, Is.EqualTo(coast.Find("eden").costumes[0].restingLocalPosition));
                Assert.That(coast.Find("eden").CoastContactsFor("puppy").TryGet(
                    visual.PlayPose("side", "push") ? visual.visual.sprite : null, out _), Is.True);
                Assert.That(visual.Initialize(starway, "choa"), Is.True);
                Assert.That(visual.PushPresenter.isActiveAndEnabled, Is.True);
                Assert.That(visual.CoastPushPresenter.isActiveAndEnabled, Is.False);
                Assert.That(visual.Initialize(theater, "seri"), Is.True);
                Assert.That(visual.TheaterPushPresenter.isActiveAndEnabled, Is.True);
                Assert.That(visual.PushPresenter.isActiveAndEnabled, Is.False);
                Assert.That(visual.Initialize(sanctum, "sein"), Is.True);
                Assert.That(visual.SanctumPushPresenter.isActiveAndEnabled, Is.True);
                Assert.That(visual.TheaterPushPresenter.isActiveAndEnabled, Is.False);
                Assert.That(visual.Initialize(coast, "haerin"), Is.True);
                Assert.That(visual.CoastPushPresenter.isActiveAndEnabled, Is.True);
                Assert.That(visual.SanctumPushPresenter.isActiveAndEnabled, Is.False);
                Assert.That(visual.transform.localPosition, Is.EqualTo(coast.Find("haerin").restingLocalPosition));
                yield return null;
            }
            finally { Object.Destroy(host); Object.Destroy(contact.gameObject); }
#else
            Assert.Ignore("Requires Unity Editor asset loader."); yield break;
#endif
        }

        [UnityTest] public IEnumerator CommittedDominoAndDiceUseCoastPushWithoutChangingCost()
        {
#if UNITY_EDITOR
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CoastCatalog);
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device13_Domino.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device22_Dice.asset" })
            {
                var source = UnityEditor.AssetDatabase.LoadAssetAtPath<PuzzleMap>(path);
                var map = Object.Instantiate(source);
                var theme = Object.Instantiate(source.theme);
                theme.letterObservatoryCharacters = null;
                theme.twilightSeaCharacters = catalog;
                theme.twilightSeaCharacterId = "haerin";
                theme.twilightSeaCostumeId = "default";
                map.theme = theme;
                var expected = new PuzzleDeviceSession(map);
                Assert.That(expected.Apply(Vector2Int.right).accepted, Is.True, path);
                var host = new GameObject("Coast committed PUSH QA");
                var controller = host.AddComponent<PuzzlePrototypeController>();
                try
                {
                    controller.Load(map);
                    Assert.That(controller.RequestMove(Vector2Int.right), Is.True, path);
                    Assert.That(controller.CharacterVisual.State, Is.EqualTo("push"), path);
                    Assert.That(controller.CharacterVisual.CoastPushPresenter.PoseActive, Is.True, path);
                    var deadline = Time.realtimeSinceStartup + 5f;
                    while (controller.IsMoving && Time.realtimeSinceStartup < deadline) yield return null;
                    yield return new WaitForSeconds(.13f);
                    Assert.That(controller.Session.Cell, Is.EqualTo(expected.Cell), path);
                    Assert.That(controller.Session.Cost, Is.EqualTo(expected.Cost), path);
                    Assert.That(controller.CharacterVisual.transform.localPosition,
                        Is.EqualTo(catalog.Find("haerin").restingLocalPosition), path);
                }
                finally { Object.Destroy(host); Object.Destroy(map); Object.Destroy(theme); }
                yield return null;
            }
#else
            Assert.Ignore("Requires Unity Editor asset loader."); yield break;
#endif
        }
    }
}
