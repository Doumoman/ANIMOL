using System.Collections;
using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ANIMOL.Tests
{
    public sealed class DevicePatchP01PlayTests
    {
        [UnityTest]
        public IEnumerator DominoPanelsStartInChainOrderWithoutExtraCost()
        {
            Time.timeScale = 1;
            EditorSceneManager.LoadSceneInPlayMode(
                "Assets/ANIMOL/Scenes/Prototype/ANIMOL_memory_theater_Prototype.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null; yield return null;
            var host = Object.FindFirstObjectByType<PuzzlePrototypeController>();
            var source = AssetDatabase.LoadAssetAtPath<PuzzleMap>(
                "Assets/ANIMOL/Data/Puzzle/Prototypes/Device13_Domino.asset");
            var map = Object.Instantiate(source);
            try
            {
                host.Load(map);
                Assert.That(host.RequestMove(Vector2Int.right), Is.True);
                Assert.That(host.Session.Cost, Is.EqualTo(1));
                var panels = host.Session.Devices.Where(d => d.role == PrototypeRole.Domino)
                    .OrderBy(d => d.position.x)
                    .Select(d => host.Devices.Actor(d.id).GetComponent<DeviceVisualView>()).ToArray();
                Assert.That(panels.Length, Is.EqualTo(3));
                Assert.That(panels[0].CurrentState, Is.EqualTo("fold_e"));
                Assert.That(panels[1].CurrentState, Is.EqualTo("standing_e"));
                Assert.That(panels[2].CurrentState, Is.EqualTo("standing_e"));
                yield return new WaitForSeconds(.8f);
                Assert.That(panels.All(p => p.CurrentState == "folded_e"), Is.True);
                Assert.That(host.Session.Cost, Is.EqualTo(1));
            }
            finally { Object.Destroy(map); Time.timeScale = 1; }
        }
    }
}
