using NUnit.Framework;
using William.NotebookUI;

namespace ANIMOL.Presentation.Tests
{
    public sealed class NotebookCampaignPolicyTests
    {
        [TestCase("SW")]
        [TestCase("ET-")]
        [TestCase("observatory-")]
        [TestCase("sea-")]
        public void NextStageStaysInItsThemeAndStopsAtTwenty(string prefix)
        {
            Assert.That(NotebookCampaignStages.PerTheme, Is.EqualTo(20));
            Assert.That(NotebookCampaignStages.TryNext(prefix + "19", out string next), Is.True);
            Assert.That(next, Is.EqualTo(prefix + "20"));
            Assert.That(NotebookCampaignStages.IsLast(prefix + "20"), Is.True);
            Assert.That(NotebookCampaignStages.TryNext(prefix + "20", out _), Is.False);
            Assert.That(NotebookCampaignStages.TryNext(prefix + "21", out _), Is.False);
            Assert.That(NotebookCampaignStages.TryNext(prefix + "00", out _), Is.False);
        }
    }
}
