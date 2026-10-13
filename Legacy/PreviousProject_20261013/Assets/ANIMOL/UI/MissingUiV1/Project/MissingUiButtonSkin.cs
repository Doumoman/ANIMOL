using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.MissingUiV1.Project
{
    // Existing UiScenePolish can be reapplied. Preserve sprite colours without changing its global policy.
    [DefaultExecutionOrder(1900)]
    [RequireComponent(typeof(Button))]
    public sealed class MissingUiButtonSkin : MonoBehaviour
    {
        private Button button;
        private Text[] labels;
        public bool DarkText;
        private void LateUpdate()
        {
            if (button == null) button = GetComponent<Button>();
            if(labels==null)labels=GetComponentsInChildren<Text>(true);
            foreach(var label in labels)if(label!=null)label.color=DarkText?MissingUiLayout.Ink:MissingUiLayout.White;
            if (button.targetGraphic != null) button.targetGraphic.color = Color.white;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white; colors.pressedColor = new Color(.85f,.85f,.85f,1);
            colors.disabledColor = new Color(.48f,.48f,.55f,1); button.colors = colors;
        }
    }
}
