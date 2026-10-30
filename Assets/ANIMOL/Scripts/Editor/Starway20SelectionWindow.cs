using System.Linq;
using ANIMOL.Puzzle;
using ANIMOL.Presentation.Editor;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Puzzle.Editor
{
    public sealed class Starway20SelectionWindow : EditorWindow
    {
        int selected;
        StarwayV3MapSet set;

        static void Open() => GetWindow<Starway20SelectionWindow>("Starway 21 maps");

        void OnGUI()
        {
            if (!set) set = AssetDatabase.LoadAssetAtPath<StarwayV3MapSet>(Starway20ProjectAdapter.ActiveSetPath);
            if (!set || set.maps.Count != 21)
            { EditorGUILayout.HelpBox("The 21-stage map set is missing or incomplete.", MessageType.Warning); return; }
            selected = EditorGUILayout.Popup("Stage", selected, set.maps.Select(e => e.stageName).ToArray());
            EditorGUILayout.LabelField(selected < 10 ? "Easy" : selected < 15 ? "Medium" : "Hard");
            var entry = set.maps[selected];
            if (GUILayout.Button("Select map asset")) Selection.activeObject = entry.map;
            if (GUILayout.Button("Play map")) PuzzlePreview.Play(entry.map);
        }
    }
}
