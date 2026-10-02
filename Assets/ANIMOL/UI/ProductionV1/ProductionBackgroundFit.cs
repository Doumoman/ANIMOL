using UnityEngine;
namespace ANIMOL.ProductionV1
{
    [ExecuteAlways]
    public sealed class ProductionBackgroundFit : MonoBehaviour
    {
        public RectTransform Surface;
        private void LateUpdate()
        {
            if(Surface==null)return;
            var r=((RectTransform)transform).rect;
            float k=Mathf.Max(r.width/352f,r.height/704f);
            Surface.sizeDelta=new Vector2(352*k,704*k);
        }
    }
}
