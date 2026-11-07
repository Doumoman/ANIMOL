using System.Text;
using ANIMOL.Presentation.Editor;
using ANIMOL.Puzzle;
using NUnit.Framework;
using UnityEditor;

namespace ANIMOL.Tests
{
    public sealed class PuzzleSearchKeyCodecTests
    {
        [Test] public void CodingIsLosslessForTokensEscapesUnicodeAndEveryUtf16CodeUnit()
        {
            var text = new StringBuilder("False:False:False:False:0:0:1,6,5,2,3,4,:0000:0:0");
            for (int value = 0; value <= char.MaxValue; value++) text.Append((char)value);
            string original = text.ToString();
            Assert.That(PuzzleSearchKeyCodec.Unpack(PuzzleSearchKeyCodec.Pack(original)), Is.EqualTo(original));
            Assert.That(PuzzleSearchKeyCodec.Unpack(PuzzleSearchKeyCodec.Pack("")), Is.Empty);
        }
        [Test] public void CurrentMapKeysRoundTripAndCrateHeavyKeysShrink()
        {
            for (int number = 0; number < 20; number++)
            {
                var map = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                    "Assets/ANIMOL/Data/Puzzle/MapData/starway/SW" + number.ToString("00") + "_MapData.asset");
                var state = new PuzzleDeviceSession(map); state.AdvanceWind();
                string key = state.SearchStateKey(), packed = PuzzleSearchKeyCodec.Pack(key);
                Assert.That(PuzzleSearchKeyCodec.Unpack(packed), Is.EqualTo(key));
                if (number == 17) Assert.That(packed.Length, Is.LessThan(key.Length / 2));
            }
        }
    }
}
