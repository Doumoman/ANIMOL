using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WilliamParadox.Story
{
    /// <summary>Recreates default UI actions at runtime only when the Builder chose the installed new input module.</summary>
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent, RequireComponent(typeof(EventSystem))]
    public sealed class StoryInputBootstrap : MonoBehaviour
    {
        public bool preferNewInputSystem;
        private void Awake()
        {
            if (!preferNewInputSystem) return;
            foreach (var module in GetComponents<BaseInputModule>())
            {
                if (module.GetType().FullName != "UnityEngine.InputSystem.UI.InputSystemUIInputModule") continue;
                var assign = module.GetType().GetMethod("AssignDefaultActions", BindingFlags.Instance | BindingFlags.Public, null, System.Type.EmptyTypes, null);
                if (assign == null) { Debug.LogWarning("Installed InputSystemUIInputModule has no public AssignDefaultActions(). Check its action bindings in the Editor.", this); return; }
                assign.Invoke(module, null);
                return;
            }
        }
    }
}
