using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.UI
{
    public sealed class UiModalStack : MonoBehaviour
    {
        [SerializeField] private Transform modalHost;
        private readonly Stack<GameObject> stack = new Stack<GameObject>();
        public int Count => stack.Count;

        private void Awake()
        {
            if (modalHost == null) modalHost = transform.Find("SafeArea/ModalHost");
            if (modalHost == null) return;
            foreach (Transform child in modalHost) child.gameObject.SetActive(false);
        }

        public bool Push(string modalName)
        {
            if (modalHost == null) return false;
            var modal = modalHost.Find(modalName)?.gameObject;
            if (modal == null) return false;
            modal.transform.SetAsLastSibling();
            modal.SetActive(true);
            stack.Push(modal);
            return true;
        }

        public bool Pop()
        {
            if (stack.Count == 0) return false;
            stack.Pop().SetActive(false);
            return true;
        }
    }
}
