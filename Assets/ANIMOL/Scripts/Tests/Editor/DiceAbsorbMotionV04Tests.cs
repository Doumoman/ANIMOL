using NUnit.Framework;
using UnityEngine;

namespace ANIMOL.Presentation.DiceHintV01.Tests
{
    public sealed class DiceAbsorbMotionV04Tests
    {
        [Test]
        public void NativeDieBodyFitsTheReceiverWithoutChangingItsAspectRatio()
        {
            var aperture = new Vector4(-.328125f, -.24609375f, .328125f, .3203125f);
            var canvasWidth = DiceGoalPlatformViewV03.FittedDieCanvasWidth(aperture);
            var visibleBodyWidth = canvasWidth * 256f / 320f;
            Assert.That(visibleBodyWidth, Is.GreaterThan(0));
            Assert.That(visibleBodyWidth, Is.LessThan(aperture.z - aperture.x));
            Assert.That(canvasWidth, Is.EqualTo(.7875f).Within(.0001f));
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
