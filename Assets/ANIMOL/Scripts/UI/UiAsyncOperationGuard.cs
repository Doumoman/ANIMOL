using System;

namespace ANIMOL.UI
{
    public sealed class UiAsyncOperationGuard
    {
        private int generation;
        private bool busy;
        public bool IsBusy => busy;
        public int Begin()
        {
            if (busy) return -1;
            busy = true;
            return ++generation;
        }
        public bool IsCurrent(int token) => busy && token == generation;
        public void Complete(int token) { if (token == generation) busy = false; }
        public void Cancel() { generation++; busy = false; }
    }
}
