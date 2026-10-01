using System.IO;
using ANIMOL.Gameplay;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public static class MoonGraphicsQaCapture
    {
        public const string Evidence = "Docs/GraphicsQA/Captures";
        [MenuItem("ANIMOL/Graphics QA/Capture Game View and Input Replay")]
        public static void Capture()
        {
            var qa = Object.FindFirstObjectByType<MoonGraphicsQaScene>();
            if (!EditorApplication.isPlaying || qa == null) throw new System.InvalidOperationException("Play MoonGraphicsQA first.");
            qa.CaptureEvidence(Evidence, Screen.height == 1920);
        }
        [MenuItem("ANIMOL/Graphics QA/Capture Current Game View")]
        public static void Still()
        {
            Directory.CreateDirectory(Evidence);
            ScreenCapture.CaptureScreenshot(Evidence + "/inspection.png");
        }
    }
}
