using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.SpritePathEditor.Tests
{
    public sealed class CanonicalSpritePathTests
    {
        [Serializable] private sealed class Entry { public string source; public string destination; public string guid; }
        [Serializable] private sealed class Manifest { public Entry[] sprites; }

        [Test]
        public void EveryMigratedSpriteResolvesWithItsOriginalGuid()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/ANIMOL/Sprites/sprite_path_manifest.json"));
            Assert.That(manifest.sprites, Has.Length.EqualTo(75));
            foreach (var entry in manifest.sprites)
            {
                Assert.That(CanonicalSpritePaths.Resolve(entry.source), Is.EqualTo(entry.destination), entry.source);
                Assert.That(File.Exists(entry.source), Is.False, entry.source);
                Assert.That(File.Exists(entry.destination), Is.True, entry.destination);
                Assert.That(AssetDatabase.AssetPathToGUID(entry.destination), Is.EqualTo(entry.guid), entry.destination);
                Assert.That(Path.GetFileName(entry.destination), Does.Match("^spr_ui_[a-z0-9]+(?:_[a-z0-9]+)*\\.png$"));
            }
        }

        [Test]
        public void DefaultBuildContainsNoPlatformerSceneEntry()
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                Assert.That(scene.path, Does.Not.Contain("MapAuthoringWorkspace"));
                Assert.That(scene.path, Does.Not.Contain("ThemeRuntime_"));
                Assert.That(scene.path, Does.Not.EndWith("/Gameplay.unity"));
                Assert.That(File.Exists(scene.path), Is.True);
            }
        }
    }
}
