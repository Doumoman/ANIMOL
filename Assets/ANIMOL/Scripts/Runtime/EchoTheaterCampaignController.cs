using UnityEngine;
using UnityEngine.SceneManagement;

namespace ANIMOL.Puzzle
{
    public sealed class EchoTheaterCampaignController : MonoBehaviour
    {
        public EchoTheaterMapSet mapSet;
        public PuzzlePrototypeController puzzle;
        int selected;
        bool stageMenu;
        bool savedWin;
        const string SelectionKey = "ANIMOL.EchoTheater.Selected";
        static string ClearKey(string id) => "ANIMOL.EchoTheater.Cleared." + id;
        public static bool IsCleared(string id) => PlayerPrefs.GetInt(ClearKey(id), 0) == 1;

        void Start()
        {
            if (!mapSet || mapSet.maps.Count != 35) { Debug.LogError("Echo Theater 35-map set is missing.", this); enabled = false; return; }
            if (!puzzle) puzzle = gameObject.AddComponent<PuzzlePrototypeController>();
            SelectStage(Mathf.Clamp(PlayerPrefs.GetInt(SelectionKey, 0), 0, mapSet.maps.Count - 1));
        }
        public void SelectStage(int index)
        {
            if (!mapSet || index < 0 || index >= mapSet.maps.Count || !mapSet.maps[index].map) return;
            selected = index; savedWin = false; stageMenu = false;
            PlayerPrefs.SetInt(SelectionKey, index);
            puzzle.Load(mapSet.maps[index].map);
        }
        void Update()
        {
            if (savedWin || !puzzle || puzzle.Session == null || !puzzle.Session.Won) return;
            PlayerPrefs.SetInt(ClearKey(mapSet.maps[selected].stageId), 1);
            PlayerPrefs.Save(); savedWin = true;
        }
        void OnGUI()
        {
            if (!mapSet || !puzzle || puzzle.Session == null) return;
            GUI.Box(new Rect(10, 10, 355, 74), "Echo Theater " + mapSet.maps[selected].stageId +
                "  " + puzzle.Session.Collected + "/3" + (puzzle.Session.Won ? "  CLEAR" : ""));
            if (GUI.Button(new Rect(18, 43, 74, 32), "Stages")) stageMenu = !stageMenu;
            if (GUI.Button(new Rect(96, 43, 60, 32), "Reset")) puzzle.ResetStage();
            if (GUI.Button(new Rect(160, 43, 60, 32), "Next")) SelectStage((selected + 1) % mapSet.maps.Count);
            if (GUI.Button(new Rect(224, 43, 65, 32), "Lobby")) SceneManager.LoadScene("ANIMOL_Main");
            if (!stageMenu) return;
            GUI.Box(new Rect(10, 90, 355, 258), "Select stage");
            for (var i = 0; i < mapSet.maps.Count; i++)
            {
                var x = 18 + i % 5 * 67;
                var y = 114 + i / 5 * 32;
                var label = (i + 1).ToString("00") + (IsCleared(mapSet.maps[i].stageId) ? " *" : "");
                if (GUI.Button(new Rect(x, y, 61, 29), label)) SelectStage(i);
            }
        }
    }
}
