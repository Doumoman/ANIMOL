using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using ANIMOL.StarwayMaps.Editor;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Puzzle.Editor
{
    public sealed class Starway20SelectionWindow : EditorWindow
    {
        int selected;
        StarwayV3MapSet set;

        [MenuItem("ANIMOL/Starway 20/Play Canonical Maps")]
        static void Open() => GetWindow<Starway20SelectionWindow>("Starway 20 maps");

        void OnGUI()
        {
            if (!set) set = AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>(Starway20ProjectAdapter.SetPath);
            if (!set || set.maps.Count != 20)
            { EditorGUILayout.HelpBox("Build and validate the 20 native maps first.", MessageType.Warning); return; }
            selected = EditorGUILayout.Popup("Stage", selected, set.maps.Select(e => e.stageName).ToArray());
            EditorGUILayout.LabelField(selected < 10 ? "Easy" : selected < 15 ? "Medium" : "Hard");
            var entry = set.maps[selected];
            if (GUILayout.Button("Select map asset")) Selection.activeObject = entry.map;
            if (GUILayout.Button("Play map")) PuzzlePreview.Play(entry.map);
            if (GUILayout.Button("Rebuild and validate all"))
            {
                StarwayPackCommands.BuildNativeMaps(); StarwayPackCommands.ValidateNativeMaps();
                set = AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>(Starway20ProjectAdapter.SetPath);
            }
        }
    }
}
