using NUnit.Framework;
using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01.Tests
{
    public sealed class DiceAbsorbMotionV04Tests
    {
        [Test]
        public void R5CopyIsThreeQuartersOfTheOriginalDieInBothAxes()
        {
            Assert.That(DiceVisualSizeV05.CanvasWidthInCells, Is.EqualTo(.9375f));
            var original = new Vector3(1.2f, .8f, 1f);
            Assert.That(Vector3.Distance(DiceVisualSizeV05.FromOriginalScale(original),
                new Vector3(.9f, .6f, 1f)), Is.LessThan(.0001f));
        }
        [Test]
        public void RebindingAndReenablingUseTheUnscaledGraphicsValue()
        {
            var root = new GameObject("Die entity");
            var graphics = new GameObject("Die graphics");
            graphics.transform.SetParent(root.transform, false);
            graphics.transform.localScale = new Vector3(1.2f, .8f, 1f);
            var renderer = graphics.AddComponent<SpriteRenderer>();
            root.AddComponent<BoxCollider2D>();
            try
            {
                var binding = root.AddComponent<DiceVisualScaleBindingV05>();
                string error;
                Assert.That(binding.Bind(renderer, out error), Is.True, error);
                Assert.That(binding.BaseDiceGraphicsScale, Is.EqualTo(new Vector3(1.2f, .8f, 1f)));
                Assert.That(Vector3.Distance(graphics.transform.localScale,
                    new Vector3(.9f, .6f, 1f)), Is.LessThan(.0001f));
                graphics.transform.localScale = Vector3.one; // A visual writer runs before the size binding.
                binding.Apply();
                Assert.That(binding.Bind(renderer, out error), Is.True, error);
                root.SetActive(false);
                root.SetActive(true); // Simulate a pooled object becoming visible again.
                Assert.That(Vector3.Distance(graphics.transform.localScale,
                    new Vector3(.9f, .6f, 1f)), Is.LessThan(.0001f));
                Assert.That(root.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void LidWaitsUntilTheEntireDieHasPassedBehindTheFrontLip()
        {
            for (int i = 0; i <= 100; i++)
            {
                float y, closed; DiceAbsorbMotionV04.Sample(i / 100f, out y, out closed);
                if (closed > 0) Assert.That(y + .5f, Is.LessThan(DiceAbsorbMotionV04.FrontPlaneY));
            }
        }
        [Test]
        public void DescentIsMonotonicAfterContactAndTheFloorEndsFullyClosed()
        {
            float previous = 1;
            for (int i = 12; i <= 100; i++)
            {
                float y, closed; DiceAbsorbMotionV04.Sample(i / 100f, out y, out closed);
                Assert.That(y, Is.LessThanOrEqualTo(previous)); previous = y;
                Assert.That(closed, Is.InRange(0, 1));
            }
            float endY, endClosed; DiceAbsorbMotionV04.Sample(1, out endY, out endClosed);
            Assert.That(endClosed, Is.EqualTo(1));
            Assert.That(endY + .5f, Is.LessThan(DiceAbsorbMotionV04.FrontPlaneY));
        }
    }
}
