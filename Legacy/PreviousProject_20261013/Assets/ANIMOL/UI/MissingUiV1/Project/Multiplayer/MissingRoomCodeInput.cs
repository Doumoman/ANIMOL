using ANIMOL.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ANIMOL.MissingUiV1.Project.MissingUiLayout;

namespace ANIMOL.MissingUiV1.Project.Multiplayer
{
    // Native TMP single-line field owns caret, horizontal text scrolling and the platform keyboard.
    // No code format/length, room protocol or lookup success is inferred here.
    public sealed class MissingRoomCodeInput:MonoBehaviour
    {
        public TMP_InputField Field;
        public TextMeshProUGUI Placeholder;
        public static MissingRoomCodeInput Build(Transform parent,MissingMultiplayerArt art)
        {
            var root=Node("RoomCodeInput",parent);Height(root,144);
            var panel=Picture(root,art.Utility.Common.Panel,true);panel.raycastTarget=true;
            var c=root.gameObject.AddComponent<MissingRoomCodeInput>();
            var viewport=Node("TextViewport",root);Fill(viewport,24);viewport.gameObject.AddComponent<RectMask2D>();
            var textRoot=Node("EditableCode",viewport);Fill(textRoot);
            var text=textRoot.gameObject.AddComponent<TextMeshProUGUI>();text.font=art.Utility.Common.Typography.Font;
            text.fontSize=32;text.color=Ink;text.raycastTarget=false;text.richText=false;text.textWrappingMode=TextWrappingModes.NoWrap;
            text.alignment=TextAlignmentOptions.MidlineLeft;
            var placeholder=Node("CodePlaceholder",viewport);Fill(placeholder);
            c.Placeholder=placeholder.gameObject.AddComponent<TextMeshProUGUI>();c.Placeholder.font=text.font;
            c.Placeholder.fontSize=32;c.Placeholder.color=new Color32(86,108,134,255);c.Placeholder.raycastTarget=false;
            c.Placeholder.alignment=TextAlignmentOptions.MidlineLeft;c.Placeholder.textWrappingMode=TextWrappingModes.NoWrap;
            c.Field=root.gameObject.AddComponent<TMP_InputField>();c.Field.targetGraphic=panel;c.Field.textViewport=viewport;
            c.Field.textComponent=text;c.Field.placeholder=c.Placeholder;c.Field.contentType=TMP_InputField.ContentType.Standard;
            c.Field.lineType=TMP_InputField.LineType.SingleLine;c.Field.characterLimit=0;c.Field.characterValidation=TMP_InputField.CharacterValidation.None;
            c.Field.keyboardType=TouchScreenKeyboardType.Default;
            // Keep TMP's serialized false defaults for both mobile visibility flags. Their public
            // setters force true on desktop, which would bake a hidden mobile keyboard into prefabs.
            c.Field.customCaretColor=true;c.Field.caretColor=Ink;c.Field.caretWidth=3;
            c.Field.selectionColor=new Color(.45f,.93f,1,.5f);
            return c;
        }
        private void LateUpdate()
        {
            Field.textComponent.fontSize=MobileControlPreferences.LargeText?48:32;
            Placeholder.fontSize=Field.textComponent.fontSize;
            Placeholder.text=MobileControlPreferences.Localize("초대받은 방 코드 입력","Enter a room code");
        }
        private void OnDisable(){if(Field!=null)Field.DeactivateInputField();}
    }
}
