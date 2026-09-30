using ANIMOL.Core;
using ANIMOL.Editor;
using NUnit.Framework;
using UnityEditor;

namespace ANIMOL.Tests.EditMode
{
    public sealed class AnimolMapEditorArtPreviewTests
    {
        [TestCase("CLOUD_SHEEP_STEP")]
        [TestCase("PAGE_BRIDGE")]
        [TestCase("LIB_POPUP_STAIR")]
        [TestCase("DEW_SEED_STEP")]
        public void OperationalTypePreviewUsesSpriteFromBoundAnimolArt(string id)
        {
            var type = StageMapScenePalette.FindType(id);
            var sprite = StageMapScenePalette.ResolvePreviewSprite(type);
            Assert.That(type, Is.Not.Null);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(sprite), Does.StartWith("Assets/ANIMOL/Atlases/ANIMOL_Master_Animations_32.png"));
            Assert.That(sprite, Is.Not.EqualTo(type.GhostSprite));
        }

        [Test]
        public void EveryDevPlayableTypeHasAnEditorPreviewSprite()
        {
            var registry = AssetDatabase.LoadAssetAtPath<StageMapObjectTypeRegistry>("Assets/ANIMOL/Data/Development/M9Objects/MapObjectTypeRegistry.asset");
            foreach (var type in registry.Types)
                if (type.ImplementationLevel == MapObjectImplementationLevel.DevPlayable)
                    Assert.That(StageMapScenePalette.ResolvePreviewSprite(type), Is.Not.Null, type.StableTypeId);
        }

    }
}
