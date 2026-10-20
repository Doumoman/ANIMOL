using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ANIMOL.Menu
{
    // Keeps existing serialized UGUI Text references while all visible lettering uses TMP.
    public sealed class MainMenuTextMirror : MonoBehaviour
    {
        public Text source;
        public TMP_Text visibleText;

        void LateUpdate()
        {
            if (!source || !visibleText) return;
            if (visibleText.text != source.text) visibleText.text = source.text;
            if (visibleText.color != source.color) visibleText.color = source.color;
        }
    }
}
