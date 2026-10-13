using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ANIMOL.Presentation.Editor;
namespace ANIMOL.Presentation.Tests
{
    public class ArtAssetTests
    {
        [Test] public void ManifestImportsAndExactClipDurations() {ArtAssetBuilder.Validate();}
        [Test] public void TouchRestartsWithoutRootMovementAndReturnsToIdle()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(ArtAssetBuilder.Root+"/Prefabs/vine_tile.prefab");var go=Object.Instantiate(source);var visual=go.GetComponent<SpriteFramePlayer>();var position=go.transform.position;
            visual.PlayTouch();visual.Advance(.2f);visual.PlayTouch();Assert.That(visual.visual.sprite,Is.EqualTo(visual.touch.frames[0]));
            visual.Advance(.45f);Assert.That(visual.IsTouching,Is.False);Assert.That(visual.visual.sprite,Is.EqualTo(visual.idle.frames[0]));Assert.That(go.transform.position,Is.EqualTo(position));Object.DestroyImmediate(go);
        }
        [Test] public void CollectFxHasNoCollisionCleansAfter280msAndResetsOnReuse()
        {
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ArtAssetBuilder.Root+"/Prefabs/bubble_collect_fx.prefab"));var visual=go.GetComponent<SpriteFramePlayer>();
            Assert.That(go.GetComponentsInChildren<Collider2D>(),Is.Empty);visual.PlayTouch();visual.Advance(.26f);Assert.That(go.activeSelf,Is.True);visual.Advance(.021f);Assert.That(go.activeSelf,Is.False);
            // EditMode does not invoke non-ExecuteAlways OnEnable. Actual pool activation is checked in PlayMode.
            go.SetActive(true);visual.ResetVisual();Assert.That(visual.visual.enabled,Is.True);Assert.That(visual.IsTouching,Is.False);Object.DestroyImmediate(go);
        }
        [Test] public void PauseFreezesOnlyPresentation()
        {
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ArtAssetBuilder.Root+"/Prefabs/vine_tile.prefab"));var visual=go.GetComponent<SpriteFramePlayer>();visual.PlayTouch();var frame=visual.visual.sprite;float scale=Time.timeScale;
            visual.paused=true;visual.Advance(5);Assert.That(visual.visual.sprite,Is.EqualTo(frame));Assert.That(Time.timeScale,Is.EqualTo(scale));visual.paused=false;visual.Advance(.45f);Assert.That(visual.IsTouching,Is.False);Object.DestroyImmediate(go);
        }
    }
}
