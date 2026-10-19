using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ANIMOL.Puzzle;
using Animol.StarwayRanch.V12;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ANIMOL.Presentation.Editor
{
    public static class StarwayRanchValidation
    {
        const string Scene = "Assets/ANIMOL/Scenes/QA/ANIMOL_Starway_Art_QA.unity";
        const string Output = "Docs/Validation/StarwayRanchV12";

        public static void CaptureDeviceQa()
        {
            EditorSceneManager.OpenScene("Assets/ANIMOL/Scenes/QA/ANIMOL_Device8_Art_QA.unity");
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (!camera) throw new InvalidOperationException("Device QA camera missing");
            Directory.CreateDirectory(Output);
            Save(camera, Output + "/device_qa_1080x1920.png", 1080, 1920);
            Debug.Log("STARWAY_V12_DEVICE_CAPTURE_OK");
        }

        public static void Capture()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.OpenScene(Scene);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (!camera) throw new InvalidOperationException("QA camera missing");
            var rig = UnityEngine.Object.FindFirstObjectByType<StarwayRanchV12BackgroundRig>();
            var sky = UnityEngine.Object.FindFirstObjectByType<StarwayRanchV12CameraCover>();
            if (!rig || !sky) throw new InvalidOperationException("V12 background components missing");
            var renderers = rig.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length != 129) throw new InvalidOperationException("Expected 128 layers + sky, got " + renderers.Length);
            if (renderers.Any(r => !r.sprite || r.sortingLayerName != "Background"))
                throw new InvalidOperationException("Missing sprite or Background sorting layer");
            if (renderers.Where(r => r.name.Contains("star") || r.name.Contains("dot"))
                .Any(r => r.sortingOrder >= -60)) throw new InvalidOperationException("Star order crosses clouds");
            rig.Rebase();
            sky.FitNow();
            Directory.CreateDirectory(Output);
            Save(camera, Output + "/qa_1080x1920.png", 1080, 1920);
            var original = camera.transform.position;
            foreach (var p in new[] { new Vector2(-30, 0), new Vector2(30, 0),
                new Vector2(0, -60), new Vector2(0, 60) })
            {
                camera.transform.position = new Vector3(original.x + p.x, original.y + p.y, original.z);
                typeof(StarwayRanchV12BackgroundRig).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(rig, null);
                sky.FitNow();
                Save(camera, Output + "/qa_camera_" + p.x + "_" + p.y + ".png", 360, 640);
            }
            camera.transform.position = original;
            var originalSize = camera.orthographicSize;
            foreach (var size in new[] { 5f, 25f })
            {
                camera.orthographicSize = size;
                typeof(StarwayRanchV12BackgroundRig).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(rig, null);
                sky.FitNow();
                Save(camera, Output + "/qa_ortho_" + size + ".png", 360, 640);
            }
            camera.orthographicSize = originalSize;
            sky.FitNow();
            if (setup.Any(entry => entry.isLoaded && entry.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
            Debug.Log("STARWAY_V12_CAPTURE_OK renderers=129 1080x1920 and four camera extremes");
        }

        static void Save(Camera camera, string file, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(file, pixels.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(pixels);
            }
            finally { camera.targetTexture = previous; RenderTexture.active = active; UnityEngine.Object.DestroyImmediate(target); }
        }
    }
}
