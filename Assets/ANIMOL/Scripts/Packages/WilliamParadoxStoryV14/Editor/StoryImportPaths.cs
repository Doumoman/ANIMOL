#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

namespace WilliamParadox.Story.Editor
{
    /// <summary>All importer writes are confined to this package. Source paths are untrusted data.</summary>
    public static class StoryImportPaths
    {
        public const string OutputRoot = "Assets/WilliamParadoxStoryV14";
        public const string ResourcePrefix = "WilliamParadox/V14/";
        public const string StoryResourceId = "WilliamParadox/V14/Story/ranch_story";
        public const string PresentationResourceId = "WilliamParadox/V14/Story/story_presentation";
        public const string ScenePath = OutputRoot + "/Scenes/RanchStoryPreview.unity";
        public const string PrefabPath = OutputRoot + "/Prefabs/StoryPreviewCanvas.prefab";
        public const string StampPath = OutputRoot + "/Editor/StoryPreviewBuildStamp.asset";
        public const string ReportPath = OutputRoot + "/Editor/LastBuildReport.txt";

        public static string ResolveSourceFile(string sourceRoot, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot))
                throw new ArgumentException("소스 폴더를 선택해 주세요.", nameof(sourceRoot));
            string normalized = ValidateRelativePath(relativePath, "소스 파일");
            string root = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string full = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, PathComparison))
                throw new InvalidDataException("소스 폴더 밖의 파일은 읽을 수 없습니다: " + relativePath);
            if (!File.Exists(full)) throw new FileNotFoundException("필수 소스 파일이 없습니다: " + relativePath, full);
            RejectSymbolicLinks(root, full);
            return full;
        }

        public static string ValidateRelativePath(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(label + " 경로가 비어 있습니다.");
            string path = value.Replace('\\', '/');
            if (Path.IsPathRooted(path) || path.StartsWith("/", StringComparison.Ordinal) || path.IndexOf(':') >= 0)
                throw new InvalidDataException(label + "에는 상대 경로만 사용할 수 있습니다: " + value);
            foreach (string segment in path.Split('/'))
            {
                if (segment.Length == 0 || segment == "." || segment == "..")
                    throw new InvalidDataException(label + "에 빈 구간이나 . / .. 구간을 사용할 수 없습니다: " + value);
                foreach (char c in segment)
                    if (char.IsControl(c) || c == '<' || c == '>' || c == '|' || c == '"' || c == '?' || c == '*')
                        throw new InvalidDataException(label + "에 사용할 수 없는 문자가 있습니다: " + value);
            }
            return path;
        }

        public static string AssetPathForResource(string resourcePath, string extension)
        {
            string resource = ValidateRelativePath(resourcePath, "Resources ID");
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                throw new InvalidDataException("Resources ID는 " + ResourcePrefix + "로 시작해야 합니다: " + resource);
            if (!string.IsNullOrEmpty(Path.GetExtension(resource)))
                throw new InvalidDataException("Resources ID에는 확장자를 붙이지 마세요: " + resource);
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".json")
                throw new InvalidDataException("지원하지 않는 소스 확장자입니다: " + extension);
            return RequireScopedAssetPath(OutputRoot + "/Resources/" + resource + extension);
        }

        public static string RequireScopedAssetPath(string assetPath)
        {
            string path = ValidateRelativePath(assetPath, "출력 에셋");
            if (path != OutputRoot && !path.StartsWith(OutputRoot + "/", StringComparison.Ordinal))
                throw new InvalidDataException("출력은 " + OutputRoot + " 안으로 제한됩니다: " + path);
            return path;
        }

        public static string ToAbsoluteAssetPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string full = Path.Combine(projectRoot, RequireScopedAssetPath(assetPath).Replace('/', Path.DirectorySeparatorChar));
            string cursor = full;
            while (!string.IsNullOrEmpty(cursor) && !string.Equals(cursor, projectRoot, PathComparison))
            {
                if ((File.Exists(cursor) || Directory.Exists(cursor)) && (File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("출력 경로에 심볼릭 링크를 사용할 수 없습니다: " + cursor);
                cursor = Path.GetDirectoryName(cursor);
            }
            return full;
        }

        public static void EnsureAssetFolder(string assetFolder)
        {
            string path = RequireScopedAssetPath(assetFolder);
            ToAbsoluteAssetPath(path);
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    string result = AssetDatabase.CreateFolder(current, parts[i]);
                    if (string.IsNullOrEmpty(result)) throw new IOException("에셋 폴더를 만들 수 없습니다: " + next);
                }
                current = next;
            }
        }

        private static StringComparison PathComparison
        {
            get { return Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal; }
        }

        private static void RejectSymbolicLinks(string root, string file)
        {
            // Reject links rather than accidentally allowing a source manifest to escape its extracted folder.
            string cursor = file;
            while (!string.IsNullOrEmpty(cursor))
            {
                if ((File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("소스에 심볼릭 링크를 사용할 수 없습니다. 실제 파일을 복사해 주세요: " + cursor);
                if (string.Equals(cursor, root, PathComparison)) break;
                cursor = Path.GetDirectoryName(cursor);
            }
        }
    }
}
#endif
