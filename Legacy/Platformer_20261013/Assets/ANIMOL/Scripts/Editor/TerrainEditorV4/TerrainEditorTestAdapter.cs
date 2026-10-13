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
            var session=root.AddComponent<CampaignMapDevSession>();
            session.ConfigureEditorPreview(root.GetComponent<StageMapRuntimeLoader>().Map,root.GetComponentInChildren<ExitRevealCameraController>(true));
        }
        public void ConfigureTouchControl(DevMobileTouchControl control,MobileTouchAction action)
        {
            var serialized=new SerializedObject(control);serialized.FindProperty("action").enumValueIndex=(int)action;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
