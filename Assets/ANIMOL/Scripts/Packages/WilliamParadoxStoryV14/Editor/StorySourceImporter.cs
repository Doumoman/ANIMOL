#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace WilliamParadox.Story.Editor
{
    [Serializable]
    public sealed class StoryImportManifest
    {
        public int schemaVersion;
        public string storySourcePath;
        public StoryImportAsset[] assets;
    }

    [Serializable]
    public sealed class StoryImportAsset
    {
        public string id;
        public string sourcePath;
        public string resourcePath;
    }

    public sealed class StoryImportFile
    {
        public string sourcePath;
        public string assetPath;
    }

    public sealed class StorySourcePlan
    {
        public string sourceRoot;
        public StoryPackage package;
        public StoryImportManifest manifest;
        public StoryPresentationPackage presentation;
        public TMP_FontAsset font;
        public string fontAssetPath;
        public string fingerprint;
        public readonly List<StoryImportFile> files = new List<StoryImportFile>();
        public readonly List<string> warnings = new List<string>();
    }

    /// <summary>Read-only preflight comes before all AssetDatabase mutations.</summary>
    public static class StorySourceImporter
    {
        public const string BuilderVersion = "14.1.1-layered-v16";
        public const string ManifestSourcePath = "data/import_manifest.json";
        public const string PresentationSourcePath = "data/story_presentation.json";
        public const string UiGlyphText = "윌리엄의 역설 별길 목장 다음 일시정지 재개 계속 처음부터 등불을 막는다 기록을 덮는다 연출 줄이기 " +
            "이솔의 말을 들었습니다. 등불을 가릴 준비가 되면 버튼을 누르세요. 스토리 대화 미리보기 · 퍼즐 플레이는 연결하지 않습니다. " +
            "다음 버튼을 눌러 계속 일시정지 · 화면 밖에서도 시간이 흐르지 않습니다. 스토리 미리보기 오류 " +
            "기록을 다 읽었습니다. 계속 버튼을 눌러주세요. 마지막 기록을 읽는 중… 다음 기록 기록 일시정지 기록 재개 0123456789 /";

        public static StorySourcePlan Validate(string sourceRoot, string requestedFontAssetPath)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot)) throw new ArgumentException("data와 art가 들어 있는 v16 소스 폴더를 선택해 주세요.");
            var plan = new StorySourcePlan { sourceRoot = Path.GetFullPath(sourceRoot) };
            string manifestFile = StoryImportPaths.ResolveSourceFile(plan.sourceRoot, ManifestSourcePath);
            string manifestJson = File.ReadAllText(manifestFile, Encoding.UTF8);
            plan.manifest = JsonUtility.FromJson<StoryImportManifest>(manifestJson);
            if (plan.manifest == null || plan.manifest.schemaVersion != 14 || plan.manifest.assets == null)
                throw new InvalidDataException("import_manifest.json은 schemaVersion 14와 assets 배열을 가져야 합니다.");
            string storyFile = StoryImportPaths.ResolveSourceFile(plan.sourceRoot, plan.manifest.storySourcePath);
            string storyJson = File.ReadAllText(storyFile, Encoding.UTF8);
            plan.package = JsonUtility.FromJson<StoryPackage>(storyJson);
            ValidateStory(plan.package);
            string presentationFile = StoryImportPaths.ResolveSourceFile(plan.sourceRoot, PresentationSourcePath);
            plan.presentation = JsonUtility.FromJson<StoryPresentationPackage>(File.ReadAllText(presentationFile, Encoding.UTF8));
            if (!StoryGraph.TryCreate(plan.package, out var presentationGraph, out var graphErrors))
                throw new InvalidDataException(string.Join("\n", graphErrors));
            var presentationErrors = StoryPresentationIndex.Validate(plan.presentation, presentationGraph);
            foreach (string requiredScene in new[] { "R01", "R05", "R06", "R08", "R10", "R13", "R17" })
                if (plan.presentation?.scenes == null || !plan.presentation.scenes.Any(s => s != null && s.sceneId == requiredScene))
                    presentationErrors.Add(requiredScene + ": 요청한 화자 연출용 분리 장면이 없습니다.");
            if (presentationErrors.Count != 0) throw new InvalidDataException("분리 연출 데이터 오류:\n" + string.Join("\n", presentationErrors));

            var byId = new Dictionary<string, StoryImportAsset>(StringComparer.Ordinal);
            var byTarget = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (StoryImportAsset item in plan.manifest.assets)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id) || byId.ContainsKey(item.id))
                    throw new InvalidDataException("import_manifest.json에 비어 있거나 중복된 에셋 ID가 있습니다.");
                string source = StoryImportPaths.ResolveSourceFile(plan.sourceRoot, item.sourcePath);
                string extension = Path.GetExtension(source).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                    throw new InvalidDataException("아트 소스는 PNG 또는 JPEG여야 합니다: " + item.sourcePath);
                string target = StoryImportPaths.AssetPathForResource(item.resourcePath, extension);
                string previous;
                if (byTarget.TryGetValue(item.resourcePath, out previous))
                {
                    if (!string.Equals(source, previous, StringComparison.Ordinal))
                        throw new InvalidDataException("서로 다른 소스가 같은 Resources 경로를 사용합니다: " + item.resourcePath);
                }
                else
                {
                    byTarget.Add(item.resourcePath, source);
                    plan.files.Add(new StoryImportFile { sourcePath = source, assetPath = target });
                }
                byId.Add(item.id, item);
            }
            foreach (StoryAsset asset in plan.package.assets)
            {
                StoryImportAsset mapping;
                if (!byId.TryGetValue(asset.id, out mapping) || mapping.resourcePath != asset.resourcePath)
                    throw new InvalidDataException("스토리 에셋의 import_manifest 매핑이 없거나 다릅니다: " + asset.id);
                string source = StoryImportPaths.ResolveSourceFile(plan.sourceRoot, mapping.sourcePath);
                ValidateSpriteRect(asset, source);
            }
            plan.files.Add(new StoryImportFile
            {
                sourcePath = storyFile,
                assetPath = StoryImportPaths.AssetPathForResource(StoryImportPaths.StoryResourceId, ".json")
            });
            plan.files.Add(new StoryImportFile
            {
                sourcePath = presentationFile,
                assetPath = StoryImportPaths.AssetPathForResource(StoryImportPaths.PresentationResourceId, ".json")
            });

            string glyphText = CollectVisibleText(plan.package);
            plan.font = ResolveFont(requestedFontAssetPath, glyphText, plan.warnings);
            plan.fontAssetPath = AssetDatabase.GetAssetPath(plan.font);
            plan.fingerprint = Fingerprint(plan, manifestJson);
            return plan;
        }

        public static void ValidateStory(StoryPackage package)
        {
            if (package == null || package.schemaVersion != 14 || string.IsNullOrWhiteSpace(package.packageId) ||
                string.IsNullOrWhiteSpace(package.contentVersion) || package.assets == null || package.nodes == null || package.nodes.Length == 0)
                throw new InvalidDataException("스토리는 schemaVersion 14, packageId, contentVersion, assets, nodes가 필요합니다.");
            var assets = new HashSet<string>(StringComparer.Ordinal);
            foreach (StoryAsset asset in package.assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.id) || !assets.Add(asset.id))
                    throw new InvalidDataException("비어 있거나 중복된 스토리 에셋 ID가 있습니다.");
                StoryImportPaths.AssetPathForResource(asset.resourcePath, ".png");
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var kinds = new HashSet<string>(new[] { "line", "cue", "gate", "credits", "end" }, StringComparer.Ordinal);
            foreach (StoryNode node in package.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id) || !ids.Add(node.id))
                    throw new InvalidDataException("비어 있거나 중복된 노드 ID가 있습니다.");
                if (!kinds.Contains(node.kind)) throw new InvalidDataException("알 수 없는 노드 kind: " + node.id + " / " + node.kind);
            }
            if (!ids.Contains(package.startNodeId)) throw new InvalidDataException("startNodeId가 실제 노드를 가리키지 않습니다.");
            foreach (StoryNode node in package.nodes)
            {
                if (!string.IsNullOrEmpty(node.nextId) && !ids.Contains(node.nextId))
                    throw new InvalidDataException("nextId 대상이 없습니다: " + node.id + " → " + node.nextId);
                if (!string.IsNullOrEmpty(node.artId) && !assets.Contains(node.artId))
                    throw new InvalidDataException("배경 artId가 없습니다: " + node.id + " / " + node.artId);
                if (node.kind == "gate" && (string.IsNullOrWhiteSpace(node.gateId) || string.IsNullOrWhiteSpace(node.gateLabel)))
                    throw new InvalidDataException("gate 노드는 gateId와 gateLabel이 필요합니다: " + node.id);
                if (node.cues == null) continue;
                foreach (StoryCue cue in node.cues)
                {
                    if (cue == null) throw new InvalidDataException("빈 cue가 있습니다: " + node.id);
                    if (!FiniteNonnegative(cue.duration) || !FiniteNonnegative(cue.delay) || !Finite(cue.amount) ||
                        !Finite(cue.x) || !Finite(cue.y) || !Finite(cue.scale) || cue.scale <= 0)
                        throw new InvalidDataException("cue 시간/수치가 유효하지 않습니다: " + node.id + " / " + cue.type);
                    if (!string.IsNullOrEmpty(cue.assetId) && !assets.Contains(cue.assetId))
                        throw new InvalidDataException("cue assetId가 없습니다: " + node.id + " / " + cue.assetId);
                }
            }
            List<string> runtimeErrors = StoryGraph.Validate(package);
            if (runtimeErrors.Count != 0) throw new InvalidDataException("런타임 스토리 검증 오류:\n" + string.Join("\n", runtimeErrors.ToArray()));
        }

        public static string CollectVisibleText(StoryPackage package)
        {
            var text = new StringBuilder(UiGlyphText);
            foreach (StoryNode node in package.nodes)
            {
                text.Append('\n').Append(node.speaker).Append('\n').Append(node.text).Append('\n').Append(node.gateLabel);
                if (node.credits != null)
                    foreach (string paragraph in node.credits) text.Append('\n').Append(paragraph);
            }
            // Newlines and rich-text tags are harmless here; checking a superset is intentional.
            return text.ToString();
        }

        public static TMP_FontAsset ResolveFont(string requestedPath, string text, List<string> warnings)
        {
            if (!string.IsNullOrWhiteSpace(requestedPath))
            {
                TMP_FontAsset requested = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(requestedPath.Replace('\\', '/'));
                if (requested == null) throw new InvalidDataException("지정한 기존 TMP_FontAsset을 찾을 수 없습니다: " + requestedPath);
                string missing = MissingGlyphReport(requested, text);
                if (!string.IsNullOrEmpty(missing)) throw new InvalidDataException(FontActionMessage(requestedPath, missing));
                return requested;
            }
            string[] paths = AssetDatabase.FindAssets("t:TMP_FontAsset").Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p, StringComparer.Ordinal).ToArray();
            var failures = new List<string>();
            foreach (string path in paths)
            {
                TMP_FontAsset candidate = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (candidate == null) continue;
                string missing = MissingGlyphReport(candidate, text);
                if (string.IsNullOrEmpty(missing))
                {
                    warnings.Add("글꼴 자동 선택: " + path + ". 다른 기존 글꼴을 쓰려면 창에서 선택하거나 -storyFont를 지정하세요.");
                    return candidate;
                }
                if (failures.Count < 3) failures.Add(path + "\n" + missing);
            }
            throw new InvalidDataException(FontActionMessage("(자동 검색)", paths.Length == 0
                ? "프로젝트에 TMP_FontAsset이 없습니다."
                : string.Join("\n\n", failures.ToArray())));
        }

        public static string MissingGlyphReport(TMP_FontAsset font, string text)
        {
            uint[] missing;
            if (font.HasCharacters(text, out missing, true, false)) return string.Empty;
            if (missing == null || missing.Length == 0) return "글꼴 문자 검사를 통과하지 못했습니다.";
            uint[] meaningful = missing.Where(c => c > 32 && c != 0x7f).Distinct().OrderBy(c => c).ToArray();
            if (meaningful.Length == 0) return string.Empty;
            string shown = string.Join(" ", meaningful.Take(80).Select(c => "U+" + c.ToString("X4") + "(" + char.ConvertFromUtf32((int)c) + ")").ToArray());
            return "누락 문자 " + meaningful.Length + "개: " + shown + (meaningful.Length > 80 ? " …" : "");
        }

        private static string FontActionMessage(string path, string missing)
        {
            return "한국어 TMP 글꼴 검증에 실패하여 임포트와 씬 생성을 중단했습니다.\n선택 글꼴: " + path + "\n" + missing +
                "\n조치: 1) 사용 권한이 있는 기존 한국어 TMP_FontAsset을 프로젝트에 준비합니다." +
                " 2) Window > TextMeshPro > Font Asset Creator에서 이 스토리의 문자를 포함한 atlas를 만들거나 지원하는 fallback을 연결합니다." +
                " 3) 빌드 창에 해당 에셋을 지정하거나 -storyFont Assets/.../Korean.asset으로 다시 실행합니다." +
                "\n빌더는 글꼴을 다운로드하거나 기존 글꼴 atlas에 문자를 자동 추가하지 않습니다.";
        }

        private static void ValidateSpriteRect(StoryAsset asset, string source)
        {
            StorySpriteRect rect = asset.rect;
            if (!StorySpriteRect.IsSpecified(rect)) return;
            if (!Finite(rect.x) || !Finite(rect.y) || !Finite(rect.width) || !Finite(rect.height) ||
                !Finite(rect.pivotX) || !Finite(rect.pivotY) || rect.x < 0 || rect.y < 0 || rect.width < 0 || rect.height < 0 ||
                rect.pivotX < 0 || rect.pivotX > 1 || rect.pivotY < 0 || rect.pivotY > 1)
                throw new InvalidDataException("sprite rect 또는 pivot이 유효하지 않습니다: " + asset.id);
            if (rect.width <= 0 || rect.height <= 0) throw new InvalidDataException("sprite rect의 폭과 높이를 함께 지정하세요: " + asset.id);
            // PNG header read only: no resize/re-encode or destructive art changes.
            if (Path.GetExtension(source).Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                using (FileStream input = File.OpenRead(source))
                {
                    byte[] header = new byte[24];
                    if (input.Read(header, 0, header.Length) != header.Length || header[0] != 137 || header[1] != 80 || header[2] != 78 || header[3] != 71)
                        throw new InvalidDataException("유효한 PNG 헤더가 아닙니다: " + source);
                    int width = BigEndianInt(header, 16), height = BigEndianInt(header, 20);
                    if (rect.x + rect.width > width || rect.y + rect.height > height)
                        throw new InvalidDataException("sprite rect가 원본 이미지 경계를 벗어납니다: " + asset.id);
                }
            }
        }

        private static int BigEndianInt(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        private static string Fingerprint(StorySourcePlan plan, string manifestJson)
        {
            using (SHA256 hash = SHA256.Create())
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(BuilderVersion);
                writer.Write(Application.unityVersion);
                writer.Write(manifestJson);
                writer.Write(AssetDatabase.AssetPathToGUID(plan.fontAssetPath));
                writer.Write(AssetDatabase.GetAssetDependencyHash(plan.fontAssetPath).ToString());
                foreach (StoryImportFile file in plan.files.OrderBy(f => f.assetPath, StringComparer.Ordinal))
                {
                    writer.Write(file.assetPath);
                    writer.Write(hash.ComputeHash(File.ReadAllBytes(file.sourcePath)));
                }
                writer.Flush();
                return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool FiniteNonnegative(float value) { return Finite(value) && value >= 0; }
    }
}
#endif
