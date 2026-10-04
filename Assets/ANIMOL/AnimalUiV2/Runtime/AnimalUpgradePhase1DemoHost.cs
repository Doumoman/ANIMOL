using System.Collections;
using UnityEngine;

namespace ANIMOL.AnimalUiV2
{
    /// <summary>Readonly demonstration only. Do not attach this host to the real growth hub.</summary>
    public sealed class AnimalUpgradePhase1DemoHost : MonoBehaviour
    {
        public AnimalUiPresenter Screen;
        private void Start()
        {
            if (Screen != null) Screen.Cancelled += OnCancelled;
        }
        private void OnDestroy()
        {
            if (Screen != null) Screen.Cancelled -= OnCancelled;
        }
        private void OnCancelled(AnimalUiMode mode, AnimalLoadout loadout)
        {
            if (Screen != null && Screen.Backend == null) StartCoroutine(ReopenReadonly(Screen.InspectedAnimalId));
        }
        private IEnumerator ReopenReadonly(string animalId)
        {
            // Back deactivates the screen after emitting Cancelled; re-open after that completes.
            yield return null;
            if (Screen == null || Screen.Backend != null) yield break;
            if (Screen.OpenUpgrade(animalId)) Screen.View.Status.text = "1단계 읽기 전용 데모 · 실제 뒤로 가기는 성장 허브에 연결";
        }
    }
}
