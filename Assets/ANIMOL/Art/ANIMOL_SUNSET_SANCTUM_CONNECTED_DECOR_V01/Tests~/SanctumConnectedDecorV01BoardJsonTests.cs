#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.IO;
using NUnit.Framework;

namespace ANIMOL.Art.SanctumConnectedDecorV01
{
    public sealed class SanctumConnectedDecorV01BoardJsonTests
    {
        private static string EmptyBoard(string revision = "0")
        {
            return "{\"schemaVersion\":1,\"packageId\":\"" + SanctumConnectedDecorV01Catalog.PackageId + "\",\"sourceManifestSha256\":\"" + SanctumConnectedDecorV01BoardSnapshot.ArtManifestSha256
                + "\",\"boardId\":\"json-test\",\"revision\":" + revision + ",\"isWholeBoard\":true,\"isExampleOnly\":true,\"originX\":0,\"originY\":0,\"originZ\":0,\"cellSize\":1,\"seed\":0,\"postSpacing\":4,\"networkCells\":[],\"groupPlacements\":[],\"allowedDecorCells\":[]}";
        }
        [Test] public void ExplicitEmptyArraysRemainValid() { Assert.AreEqual(0, SanctumConnectedDecorV01BoardJson.Parse(EmptyBoard()).networkCells.Length); }
        [TestCase("9007199254740993")] [TestCase("9223372036854775807")]
        public void JsonUtilityPreservesExactInt64Revision(string value)
        { Assert.AreEqual(long.Parse(value, System.Globalization.CultureInfo.InvariantCulture), SanctumConnectedDecorV01BoardJson.Parse(EmptyBoard(value)).revision); }
        [TestCase("9223372036854775808")] [TestCase("1.0")] [TestCase("1e2")]
        public void RevisionMustBeExactInt64(string value) { Assert.Throws<InvalidDataException>(() => SanctumConnectedDecorV01BoardJson.Parse(EmptyBoard(value))); }
        [Test] public void EscapedDuplicateKeysRejected()
        { Assert.Throws<InvalidDataException>(() => SanctumConnectedDecorV01BoardJson.Parse(EmptyBoard().Replace("\"originX\":0", "\"originX\":0,\"origin\\u0058\":0"))); }
        [Test] public void WhitespaceInsideUnicodeEscapeRejected()
        { Assert.Throws<InvalidDataException>(() => SanctumConnectedDecorV01BoardJson.Parse(EmptyBoard().Replace("originX", "origin\\u 058"))); }
        [Test] public void UnknownFieldRejected()
        { Assert.Throws<InvalidDataException>(() => SanctumConnectedDecorV01BoardJson.Parse(EmptyBoard().Replace("\"originX\":0", "\"unknown\":0"))); }
        [Test] public void NullRequiredArrayRejected()
        { Assert.Throws<InvalidDataException>(() => SanctumConnectedDecorV01BoardJson.Parse(EmptyBoard().Replace("\"networkCells\":[]", "\"networkCells\":null"))); }
        [Test] public void Int32OverflowRejected()
        { Assert.Throws<InvalidDataException>(() => SanctumConnectedDecorV01BoardJson.Parse(EmptyBoard().Replace("\"seed\":0", "\"seed\":2147483648"))); }
        [Test] public void FloatOverflowRejected()
        { Assert.Throws<InvalidDataException>(() => SanctumConnectedDecorV01BoardJson.Parse(EmptyBoard().Replace("\"originX\":0", "\"originX\":1e400"))); }

        [Test] public void ManifestSchemaAcceptsExactUnicodeEscape()
        { Assert.DoesNotThrow(() => SanctumConnectedDecorV01JsonSchema.Validate("{\"schemaVersion\":1,\"packageId\":\"\\u0061\",\"assets\":[],\"groups\":[]}")); }
        [TestCase(" 061")] [TestCase("+061")] [TestCase("00g1")]
        public void ManifestSchemaRejectsNonHexUnicodeDigits(string digits)
        { Assert.Throws<InvalidDataException>(() => SanctumConnectedDecorV01JsonSchema.Validate("{\"schemaVersion\":1,\"packageId\":\"\\u" + digits + "\",\"assets\":[],\"groups\":[]}")); }
    }
}
#endif
