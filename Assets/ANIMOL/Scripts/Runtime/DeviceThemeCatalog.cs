using System;
using UnityEngine;
namespace ANIMOL.Presentation
{
    [Serializable] public class DeviceThemeEntry {public string deviceId;public GameObject prefab;}
    public sealed class DeviceThemeCatalog : ScriptableObject
    {
        public string themeId;
        public DeviceThemeEntry[] devices;
        // Swap art data on the existing writer. No extra renderer, subscriptions or gameplay state.
        public bool Apply(DeviceVisualView view,string deviceId)
        {
            var entry=Array.Find(devices,e=>e.deviceId==deviceId);
            if(entry==null||!view||view.deviceId!=deviceId)return false;
            var source=entry.prefab.GetComponent<DeviceVisualView>();
            view.RebindArt(source.states,source.initialState);return true;
        }
    }
}
