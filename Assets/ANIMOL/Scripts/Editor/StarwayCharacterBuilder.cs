using System;
using System.IO;
using ANIMOL.Presentation;
using ANIMOL.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Animol.Art.Starway160
{
    public static class StarwayCharacterBuilder
    {
        const string Root = "Assets/ANIMOL/Art/StarwayCharacters160";
        const string CatalogPath = "Assets/ANIMOL/Data/Art/StarwayCharacters.asset";
        const string QaScene = "Assets/ANIMOL/Scenes/ANIMOL_Starway_Character_QA.unity";
        static readonly string[] Ids = { "choa", "uram", "yeoul", "tori" };
        static readonly string[] Names = { "초아", "우람", "여울", "토리" };

        [MenuItem("ANIMOL/Art/Connect Starway Characters")]
        public static void Build()
        {
            var manifest = JsonUtility.FromJson<AnimolStarway160Importer.Manifest>(
                File.ReadAllText(Root + "/starway_animations.json"));
            if (manifest.version != 6 || manifest.clips == null || manifest.clips.Length != 60)
                throw new InvalidDataException("Starway character manifest must be v6 with 60 clips.");
            var catalog = AssetDatabase.LoadAssetAtPath<StarwayCharacterCatalog>(CatalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<StarwayCharacterCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.defaultCharacterId = "choa";
            catalog.characters = new StarwayCharacterDefinition[Ids.Length];
            for (var i = 0; i < Ids.Length; i++)
            {
                var id = Ids[i];
                var entry = new StarwayCharacterDefinition { id = id, displayName = Names[i] };
                entry.illustration = AssetDatabase.LoadAssetAtPath<Sprite>(
                    Root + "/Illustrations/animol_starway_" + id + "_illustration.png");
                if (!entry.illustration) throw new InvalidDataException("Missing illustration: " + id);
                var index = 0;
                foreach (var data in manifest.clips)
                {
                    if (data.character != id) continue;
                    var path = Root + "/Animations/" + id + "/" + data.name + ".anim";
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (!clip) throw new InvalidDataException("Missing playable clip: " + path);
                    var motion = new StarwayCharacterMotion { clip = clip, fps = data.fps, loop = data.loop };
                    for (var frame = 0; frame < 6; frame++)
                    {
                        motion.frames[frame] = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + data.frames[frame]);
                        if (!motion.frames[frame]) throw new InvalidDataException("Missing frame: " + data.name + "/" + frame);
                    }
                    entry.motions[index] = motion;
                    index++;
                }
                if (index != 15) throw new InvalidDataException("Expected 15 clips for " + id);
                catalog.characters[i] = entry;
            }
            EditorUtility.SetDirty(catalog);
            foreach (var path in new[] {
                "Assets/ANIMOL/Data/Puzzle/Themes/StarGarden.asset",
                "Assets/ANIMOL/Data/Puzzle/Prototypes/starway_Theme.asset" })
            {
                var theme = AssetDatabase.LoadAssetAtPath<PuzzleTheme>(path);
                if (!theme) throw new FileNotFoundException("Missing starway theme", path);
                theme.starwayCharacters = catalog;
                theme.starwayCharacterId = catalog.defaultCharacterId;
                // Other theme previews copy this historical static fallback. Runtime uses the catalog Visual.
                theme.player = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/ANIMOL/Sprites/Player/rabbit/Frames/spr_player_rabbit_idle_s.png");
                EditorUtility.SetDirty(theme);
            }
            AssetDatabase.SaveAssets();
            BuildQaScene(catalog);
            Debug.Log("ANIMOL Starway v6: connected four characters, 60 clips, four illustrations, and both playable Starway theme assets.");
        }

        public static void BuildBatch() => Build();

        static void BuildQaScene(StarwayCharacterCatalog catalog)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.GetComponent<Camera>().orthographic = true;
            camera.GetComponent<Camera>().orthographicSize = 4;
            camera.GetComponent<Camera>().backgroundColor = new Color(.1f, .14f, .2f);
            var root = new GameObject("Starway Character QA");
            var player = StarwayCharacterVisual.Create(root.transform, Vector2.zero, 40, catalog, catalog.defaultCharacterId);
            player.catalog = catalog;
            player.characterId = catalog.defaultCharacterId;
            player.visual = player.GetComponent<SpriteRenderer>();
            EditorUtility.SetDirty(player);
            var qa = root.AddComponent<StarwayCharacterQaController>();
            qa.catalog = catalog;
            qa.character = player;
            EditorSceneManager.SaveScene(scene, QaScene);
        }
    }
}
