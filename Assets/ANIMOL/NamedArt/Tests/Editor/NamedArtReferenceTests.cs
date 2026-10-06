using System;
using System.Collections;
using System.IO;
using System.Linq;
using ANIMOL.NamedArt.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ANIMOL.NamedArt.Tests
{
    public class NamedArtReferenceTests
    {
        private const string Root = "Assets/ANIMOLNamedArtFixture/";
        private const string ImagePath = Root + "replace_me.png";
        private string previousIndex;
        [SetUp] public void Setup()
        {
            previousIndex = NamedArtReferences.IndexPath;
            NamedArtReferences.IndexPath = "Library/ANIMOLNamedArtFixture.json";
            NamedArtAutomation.Suspended = true; NamedArtAutomation.ResetQueue();
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath.StartsWith(Root, StringComparison.Ordinal)) StageUtility.GoToMainStage();
            if (File.Exists(NamedArtReferences.IndexPath)) File.Delete(NamedArtReferences.IndexPath);
            AssetDatabase.CreateFolder("Assets", "ANIMOLNamedArtFixture");
        }
        [TearDown] public void Cleanup()
        {
            NamedArtAutomation.Suspended = true; NamedArtAutomation.ResetQueue();
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath.StartsWith(Root, StringComparison.Ordinal)) StageUtility.GoToMainStage();
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Root + "fixture.unity");
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.DeleteAsset(Root.TrimEnd('/'));
            if (File.Exists(NamedArtReferences.IndexPath)) File.Delete(NamedArtReferences.IndexPath);
            NamedArtReferences.IndexPath = previousIndex; NamedArtAutomation.Suspended = false;
        }
        private static byte[] Png(Color color)
        {
            var t = new Texture2D(16, 8); t.SetPixels(Enumerable.Repeat(color, 128).ToArray()); t.Apply();
            var png = t.EncodeToPNG(); Object.DestroyImmediate(t); return png;
        }
        private static Sprite Import(bool multiple = false, string path = ImagePath)
        {
            File.WriteAllBytes(path, Png(Color.red)); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var i = (TextureImporter)AssetImporter.GetAtPath(path);
            i.textureType = TextureImporterType.Sprite; i.spritePixelsPerUnit = 16;
            i.filterMode = FilterMode.Point; i.mipmapEnabled = false; i.textureCompression = TextureImporterCompression.Uncompressed;
            i.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            if (multiple)
            {
#pragma warning disable CS0618
                i.spritesheet = new[] { new SpriteMetaData { name = "ear", rect = new Rect(0, 0, 8, 8), pivot = new Vector2(.25f, .75f), alignment = 9 },
                    new SpriteMetaData { name = "tail", rect = new Rect(8, 0, 8, 8), pivot = new Vector2(.5f, .5f), alignment = 9 } };
#pragma warning restore CS0618
            }
            i.SaveAndReimport(); return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
        }
        private static void Prefab(Sprite s)
        {
            var root = new GameObject("Saved", typeof(RectTransform), typeof(Image)); root.GetComponent<Image>().sprite = s;
            var child = new GameObject("Inactive", typeof(SpriteRenderer)); child.transform.SetParent(root.transform); child.GetComponent<SpriteRenderer>().sprite = s; child.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, Root + "saved.prefab"); Object.DestroyImmediate(root);
        }
        private static void Replace()
        {
            Assert.IsTrue(AssetDatabase.DeleteAsset(ImagePath));
            // Import a genuinely new GUID before moving it into the old slot. Unity can otherwise reuse a recently deleted GUID.
            AssetDatabase.CreateFolder(Root.TrimEnd('/'), "Incoming");
            string incoming = Root + "Incoming/replace_me.png";
            File.WriteAllBytes(incoming, Png(Color.green)); AssetDatabase.ImportAsset(incoming, ImportAssetOptions.ForceSynchronousImport);
            Assert.IsEmpty(AssetDatabase.MoveAsset(incoming, ImagePath));
            AssetDatabase.ImportAsset(ImagePath, ImportAssetOptions.ForceSynchronousImport);
        }
        [Test] public void GameplayMapImportsAndSavesDoNotParseOrQueueTheArtReferenceIndex()
        {
            // Reflect the existing owner so the art package retains no dependency on
            // the gameplay assembly. Use a real map asset, not a path-name heuristic.
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ANIMOL.Core.StageMapDefinition")).FirstOrDefault(t=>t!=null);
            Assert.That(type,Is.Not.Null);
            var map=ScriptableObject.CreateInstance(type);
            var path=Root+"any-map-name.asset";
            AssetDatabase.CreateAsset(map,path);AssetDatabase.SaveAssetIfDirty(map);
            File.WriteAllText(NamedArtReferences.IndexPath,"Must not be parsed for gameplay map changes");
            NamedArtAutomation.Suspended=false;
            Assert.DoesNotThrow(()=>NamedArtAutomation.QueueSaved(path));
            Assert.DoesNotThrow(()=>NamedArtAutomation.QueueImports(new[]{path},Array.Empty<string>(),new[]{path}));
            Assert.That(NamedArtAutomation.Pending,Is.False);
            Assert.That(File.ReadAllText(NamedArtReferences.IndexPath),Is.EqualTo("Must not be parsed for gameplay map changes"));
        }
        [Test] public void ChangedGuidRebindsStaticPrefabImageAndInactiveRenderer()
        {
            var old = Import(); Prefab(old); string guid = AssetDatabase.AssetPathToGUID(ImagePath);
            NamedArtReferences.CaptureAll(Root); Replace();
            Assert.AreNotEqual(guid, AssetDatabase.AssetPathToGUID(ImagePath));
            var r = NamedArtReferences.Repair(); Assert.AreEqual(2, r.ChangedSlots); Assert.IsEmpty(r.Issues);
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "saved.prefab"); var current = AssetDatabase.LoadAssetAtPath<Sprite>(ImagePath);
            Assert.AreSame(current, p.GetComponent<Image>().sprite); Assert.AreSame(current, p.GetComponentInChildren<SpriteRenderer>(true).sprite);
            Assert.AreEqual(2, p.GetComponentsInChildren<Transform>(true).Length);
        }
        [Test] public void NamedSheetFramesRestoreSlicingAndAnimationAndTileReferences()
        {
            var s = Import(true); Prefab(s);
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = s; AssetDatabase.CreateAsset(tile, Root + "tile.asset");
            var clip = new AnimationClip(); AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                new[] { new ObjectReferenceKeyframe { time = 0, value = s } }); AssetDatabase.CreateAsset(clip, Root + "clip.anim");
            NamedArtReferences.CaptureAll(Root); Replace();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(ImagePath).OfType<Sprite>().OrderBy(x => x.name).ToArray();
            Assert.AreEqual(new[] { "ear", "tail" }, sprites.Select(x => x.name)); Assert.AreEqual(new Vector2(2, 6), sprites[0].pivot);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ImagePath); Assert.AreEqual(FilterMode.Point, importer.filterMode); Assert.IsFalse(importer.mipmapEnabled);
            var r = NamedArtReferences.Repair(); Assert.IsEmpty(r.Issues); Assert.GreaterOrEqual(r.ChangedSlots, 4);
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<Tile>(Root + "tile.asset").sprite);
            var loaded = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "clip.anim");
            Assert.NotNull(AnimationUtility.GetObjectReferenceCurve(loaded, AnimationUtility.GetObjectReferenceCurveBindings(loaded)[0])[0].value);
        }
        [Test] public void SceneRebindsWithoutNewObjectsAndDefersUnsavedEdits()
        {
            var s = Import(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("StaticSprite", typeof(SpriteRenderer)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            root.GetComponent<SpriteRenderer>().sprite = s; EditorSceneManager.SaveScene(scene, Root + "fixture.unity");
            NamedArtReferences.CaptureAll(Root); Replace(); EditorSceneManager.MarkSceneDirty(scene);
            var deferred = NamedArtReferences.Repair(); Assert.AreEqual(0, deferred.ChangedSlots); Assert.IsTrue(deferred.Issues.Any(x => x.Contains("Unsaved scene")));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var r = NamedArtReferences.Repair(); Assert.IsEmpty(r.Issues); Assert.AreEqual(1, r.ChangedSlots);
            scene = EditorSceneManager.OpenScene(Root + "fixture.unity", OpenSceneMode.Additive);
            Assert.AreEqual(1, scene.rootCount); Assert.NotNull(scene.GetRootGameObjects()[0].GetComponent<SpriteRenderer>().sprite);
        }
        [Test] public void DifferentUserAssignmentAndExplicitNullAreNotOverwritten()
        {
            var s = Import(); var other = Import(false, Root + "other.png"); Prefab(s); NamedArtReferences.CaptureAll(Root);
            var p = PrefabUtility.LoadPrefabContents(Root + "saved.prefab"); p.GetComponent<Image>().sprite = other;
            p.GetComponentInChildren<SpriteRenderer>(true).sprite = null;
            PrefabUtility.SaveAsPrefabAsset(p, Root + "saved.prefab"); PrefabUtility.UnloadPrefabContents(p);
            var index = NamedArtIndex.Read(NamedArtReferences.IndexPath); NamedArtReferences.CaptureContainer(index, Root + "saved.prefab", true); index.Write(NamedArtReferences.IndexPath);
            Replace(); NamedArtReferences.Repair(); p = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "saved.prefab");
            Assert.AreSame(other, p.GetComponent<Image>().sprite); Assert.IsNull(p.GetComponentInChildren<SpriteRenderer>(true).sprite);
        }
        [Test] public void DuplicateFileNamesNeverPickAnArbitraryReplacement()
        {
            var s = Import(); var index = NamedArtReferences.CaptureAll(Root); var source = index.Sources.First(x => x.Kind == "Sprite");
            AssetDatabase.DeleteAsset(ImagePath); AssetDatabase.CreateFolder(Root.TrimEnd('/'), "A"); AssetDatabase.CreateFolder(Root.TrimEnd('/'), "B");
            Import(false, Root + "A/replace_me.png"); Import(false, Root + "B/replace_me.png");
            var value = index.Resolve(source, AssetDatabase.GetAllAssetPaths().Where(index.InScope).ToArray(), out string reason);
            Assert.IsNull(value); Assert.AreEqual("Ambiguous filename", reason);
        }
        [UnityTest] public IEnumerator ImportAutomaticallyRepairsWithoutARebuildButton()
        {
            var s = Import(); Prefab(s); NamedArtReferences.CaptureAll(Root); NamedArtAutomation.Suspended = false;
            Replace();
            double deadline = EditorApplication.timeSinceStartup + 10;
            while (NamedArtAutomation.Pending && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.IsFalse(NamedArtAutomation.Pending);
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "saved.prefab").GetComponent<Image>().sprite);
        }
        [Test] public void SerializedSpriteSubassetsKeepTheirNewSourceTexture()
        {
            var s = Import(); var tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, Root + "subasset.asset");
            var frame = Sprite.Create(s.texture, new Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 16); frame.name = "Loading_0";
            AssetDatabase.AddObjectToAsset(frame, tile); tile.sprite = frame; EditorUtility.SetDirty(tile); AssetDatabase.SaveAssetIfDirty(tile);
            NamedArtReferences.CaptureAll(Root); Replace(); var r = NamedArtReferences.Repair(); Assert.IsEmpty(r.Issues);
            var current = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
            Assert.AreSame(current, AssetDatabase.LoadAssetAtPath<Tile>(Root + "subasset.asset").sprite.texture);
        }
        [Test] public void SamePathReimportRestoresSheetEvenWhenUnityReusesADeletedGuid()
        {
            Import(true); NamedArtReferences.CaptureAll(Root);
            AssetDatabase.DeleteAsset(ImagePath); File.WriteAllBytes(ImagePath, Png(Color.green));
            AssetDatabase.ImportAsset(ImagePath, ImportAssetOptions.ForceSynchronousImport);
            Assert.AreEqual(new[] { "ear", "tail" }, AssetDatabase.LoadAllAssetsAtPath(ImagePath).OfType<Sprite>().Select(s => s.name).OrderBy(s => s).ToArray());
        }
        [UnityTest] public IEnumerator NewImportedSpritesAreAutomaticallyIndexedForFutureReplacement()
        {
            NamedArtReferences.CaptureAll(Root); NamedArtAutomation.Suspended = false; Import(true);
            double deadline = EditorApplication.timeSinceStartup + 10;
            while (NamedArtAutomation.Pending && EditorApplication.timeSinceStartup < deadline) yield return null;
            var index = NamedArtIndex.Read(NamedArtReferences.IndexPath);
            Assert.AreEqual(2, index.Sources.Count(s => s.Kind == "Sprite")); Assert.AreEqual(1, index.Importers.Count);
        }
        [UnityTest] public IEnumerator DeferredSceneReplacementResumesAfterUserSavesTheirEdits()
        {
            var sprite = Import(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Authored", typeof(SpriteRenderer)); go.GetComponent<SpriteRenderer>().sprite = sprite;
            EditorSceneManager.SaveScene(scene, Root + "fixture.unity"); NamedArtReferences.CaptureAll(Root);
            go.transform.position = new Vector3(7, 9, 0); EditorSceneManager.MarkSceneDirty(scene);
            NamedArtAutomation.Suspended = false; Replace();
            double deadline = EditorApplication.timeSinceStartup + 10;
            while (NamedArtAutomation.Pending && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.IsTrue(scene.isDirty); Assert.AreEqual(new Vector3(7, 9, 0), go.transform.position);
            EditorSceneManager.SaveScene(scene);
            deadline = EditorApplication.timeSinceStartup + 10;
            while (NamedArtAutomation.Pending && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.NotNull(go.GetComponent<SpriteRenderer>().sprite); Assert.AreEqual(new Vector3(7, 9, 0), go.transform.position);
        }
        [UnityTest] public IEnumerator OpenPrefabIsDeferredUntilTheUserClosesItsStage()
        {
            var sprite = Import(); Prefab(sprite); NamedArtReferences.CaptureAll(Root);
            var stage = PrefabStageUtility.OpenPrefab(Root + "saved.prefab");
            double frameReady = EditorApplication.timeSinceStartup + .5;
            while (EditorApplication.timeSinceStartup < frameReady) yield return null;
            NamedArtAutomation.Suspended = false; Replace();
            double deadline = EditorApplication.timeSinceStartup + 10;
            while (NamedArtAutomation.Pending && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.AreSame(stage, PrefabStageUtility.GetCurrentPrefabStage());
            Assert.IsTrue(NamedArtReferences.LastReport.Issues.Any(i => i.StartsWith("Open prefab deferred")));
            frameReady = EditorApplication.timeSinceStartup + .5;
            while (EditorApplication.timeSinceStartup < frameReady) yield return null;
            StageUtility.GoToMainStage();
            deadline = EditorApplication.timeSinceStartup + 10;
            while (NamedArtAutomation.Pending && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "saved.prefab").GetComponent<Image>().sprite);
        }
    }
}
