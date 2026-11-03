#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WilliamParadox.Story.Editor
{
    /// <summary>Small editor safety regression suite; no NUnit/Test Framework dependency and no asset writes.</summary>
    public static class StoryEditorSafetyChecks
    {
        [MenuItem("Tools/William Paradox/Run Editor Safety Checks")]
        public static void Run()
        {
            var failures = new List<string>();
            int checks = 0;
            foreach (string path in new[] { "../escape.png", "art/../escape.png", "art\\..\\escape.png", "/tmp/escape.png", "C:/escape.png", "art//file.png", "./art/file.png", "art/./file.png", "art/file?.png" })
            {
                checks++;
                ExpectThrows(() => StoryImportPaths.ValidateRelativePath(path, "검사"), "차단해야 하는 상대 경로: " + path, failures);
            }
            foreach (string resource in new[] { "Other/Art/R01", "WilliamParadox/V14/../R01", "WilliamParadox/V14/Art/R01.png", "WilliamParadox/V14//R01" })
            {
                checks++;
                ExpectThrows(() => StoryImportPaths.AssetPathForResource(resource, ".png"), "차단해야 하는 Resources ID: " + resource, failures);
            }
            checks++;
            Expect(StoryImportPaths.AssetPathForResource("WilliamParadox/V14/Art/Scenes/R01", ".png") ==
                "Assets/WilliamParadoxStoryV14/Resources/WilliamParadox/V14/Art/Scenes/R01.png", "정상 Resources 매핑", failures);
            checks++;
            Expect(StoryImportPaths.ValidateRelativePath("art/장면/R01.png", "검사") == "art/장면/R01.png", "한글 상대 경로 보존", failures);
            checks++;
            ExpectThrows(() => StoryImportPaths.RequireScopedAssetPath("Assets/ExistingGame/MainUI.prefab"), "기존 프로젝트 UI 출력 거부", failures);
            checks++;
            ExpectThrows(() => StorySourceImporter.ValidateStory(null), "빈 스토리 거부", failures);
            if (failures.Count > 0) throw new InvalidDataException("Editor 안전 검사 실패 " + failures.Count + "/" + checks + "\n" + string.Join("\n", failures.ToArray()));
            Debug.Log("Editor 안전 검사 통과: " + checks + "개 · 경로 검증과 출력 범위 검사 · 에셋 변경 없음");
        }

        public static void RunFromCommandLine()
        {
            try { Run(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); if (Application.isBatchMode) EditorApplication.Exit(1); else throw; }
        }

        private static void ExpectThrows(Action action, string label, List<string> failures)
        {
            try { action(); failures.Add(label); }
            catch (ArgumentException) { }
            catch (InvalidDataException) { }
        }
        private static void Expect(bool condition, string label, List<string> failures) { if (!condition) failures.Add(label); }
    }
}
#endif
