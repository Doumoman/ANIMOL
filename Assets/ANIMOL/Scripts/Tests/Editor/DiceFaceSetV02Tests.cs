using NUnit.Framework;
using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01.Tests
{
    public sealed class DiceFaceSetV02Tests
    {
        [Test]
        public void GoalAndHintUseTheSameFaceValueAndRejectInvalidValues()
        {
            var set = ScriptableObject.CreateInstance<DiceFaceSetV02>();
            var texture = new Texture2D(256, 256); var sprites = new Sprite[6];
            try
            {
                for (int i = 0; i < 6; i++) sprites[i] = Sprite.Create(texture, new Rect(0, 0, 256, 256), new Vector2(.5f, .5f), 256);
                set.faces = sprites;
                for (int value = 1; value <= 6; value++)
                {
                    Sprite goal, hint;
                    Assert.That(set.TryGet(value, out goal), Is.True);
                    Assert.That(set.TryGet(value, out hint), Is.True);
                    Assert.That(goal, Is.SameAs(sprites[value - 1])); Assert.That(hint, Is.SameAs(goal));
                }
                Sprite invalid; Assert.That(set.TryGet(0, out invalid), Is.False); Assert.That(set.TryGet(7, out invalid), Is.False);
            }
            finally { foreach (var sprite in sprites) if (sprite != null) Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); Object.DestroyImmediate(set); }
        }
        [Test]
        public void PreviewFillsTheCellAfterTransparentCanvasPaddingIsAccountedFor()
        {
            float visibleCoverage = DiceFaceLayoutV02.HintWidth * 224f / 256f;
            Assert.That(visibleCoverage, Is.InRange(.95f, 1f));
        }
        [Test]
        public void PreviewSharingAndRemovalNeverChangeTheGoalsRequiredFace()
        {
            var set = ScriptableObject.CreateInstance<DiceFaceSetV02>(); var texture = new Texture2D(256, 256);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 256, 256), new Vector2(.5f, .5f), 256);
            var go = new GameObject("GoalFaceTest");
            try
            {
                for (int i = 0; i < 6; i++) set.faces[i] = sprite;
                var floor = go.AddComponent<SpriteRenderer>(); var goal = go.AddComponent<DiceGoalFaceViewV02>(); string error;
                Assert.That(goal.Configure(set, floor, go.transform, 1, new Vector2Int(3, 4), 5, out error), Is.True, error);
                Assert.That(goal.RequiredFaceVisible, Is.True);
                goal.SetPreviewOwner(1, true); Assert.That(goal.RequiredFaceVisible, Is.False);
                goal.SetPreviewOwner(2, true); goal.SetPreviewOwner(1, false); Assert.That(goal.RequiredFaceVisible, Is.False);
                goal.SetPreviewOwner(2, false); Assert.That(goal.RequiredFaceVisible, Is.True);
                goal.SetConvertedPresentation(true); Assert.That(goal.RequiredFaceVisible, Is.False);
                goal.SetConvertedPresentation(false); Assert.That(goal.RequiredFaceVisible, Is.True);
                Assert.That(goal.RequiredFace, Is.EqualTo(5)); Assert.That(goal.LogicalCell, Is.EqualTo(new Vector2Int(3, 4)));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); Object.DestroyImmediate(set); }
        }
    }
}
