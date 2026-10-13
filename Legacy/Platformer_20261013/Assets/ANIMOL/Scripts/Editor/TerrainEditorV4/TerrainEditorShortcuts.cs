using System;
using System.Reflection;
using ANIMOL.Development;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    // One Editor dispatch owns Ctrl/Cmd Z/Y/S in the focused Game View. The runtime
    // Input loop never polls these shortcuts, so Unity and uGUI cannot both commit them.
    [InitializeOnLoad]
    public static class TerrainEditorShortcuts
    {
        private static readonly FieldInfo GlobalHandler=typeof(EditorApplication).GetField("globalEventHandler",BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public);
        private static readonly EditorApplication.CallbackFunction Handler=Dispatch;
        static TerrainEditorShortcuts()
        {
            if(GlobalHandler==null)return;
            GlobalHandler.SetValue(null,Delegate.Combine((Delegate)GlobalHandler.GetValue(null),Handler));
            AssemblyReloadEvents.beforeAssemblyReload+=()=>GlobalHandler.SetValue(null,Delegate.Remove((Delegate)GlobalHandler.GetValue(null),Handler));
        }
        public static void Dispatch()
        {
            var screen=TerrainEditorScreen.Instance;var e=Event.current;
            if(screen==null || !TerrainEditorSession.Active || EditorWindow.focusedWindow?.GetType().Name!="GameView" || e==null || e.type!=EventType.KeyDown)return;
            if(!(e.control||e.command) || e.keyCode is not (KeyCode.Z or KeyCode.Y or KeyCode.S))return;
            var key=e.keyCode;var shift=e.shift;e.Use();
            if(screen.TextEditing())return;
            if(screen.IsTesting){screen.SetStatus("시험 플레이 중에는 편집 단축키를 사용할 수 없습니다.");return;}
            screen.Run(()=>
            {
                if(key==KeyCode.Z && !shift)screen.Adapter.Undo();
                else if(key==KeyCode.Y || key==KeyCode.Z)screen.Adapter.Redo();
                else{screen.Adapter.Save();screen.SetStatus("저장 완료 · revision "+screen.Adapter.Map.AuthoringRevision);}
            });
        }
    }
}
