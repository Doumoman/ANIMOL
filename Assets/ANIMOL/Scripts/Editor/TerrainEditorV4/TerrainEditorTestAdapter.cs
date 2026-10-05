using ANIMOL.Gameplay;
using UnityEditor;
using UnityEngine;

namespace ANIMOL.Editor
{
    public sealed partial class TerrainEditorAdapter
    {
        public GameObject PlayerPrefab => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ANIMOL/Prefabs/Gameplay/DevPlayer.prefab");
        public void ConfigureTestServices(GameObject root,Vector3 spawn)
        {
            var checkpoint=new GameObject("Test respawn").transform;checkpoint.SetParent(root.transform,false);checkpoint.position=spawn;
            var session=root.AddComponent<ObjectLabSession>();var serialized=new SerializedObject(session);
            serialized.FindProperty("checkpoint").objectReferenceValue=checkpoint;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        public void ConfigureTouchControl(DevMobileTouchControl control,MobileTouchAction action)
        {
            var serialized=new SerializedObject(control);serialized.FindProperty("action").enumValueIndex=(int)action;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
