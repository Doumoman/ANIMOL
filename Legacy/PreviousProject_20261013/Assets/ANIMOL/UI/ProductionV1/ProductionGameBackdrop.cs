using UnityEngine;

namespace ANIMOL.ProductionV1
{
    /// <summary>Three editor-authored finite source planes. No tiling and no world-size assumptions.</summary>
    [DefaultExecutionOrder(900)]
    public sealed class ProductionGameBackdrop : MonoBehaviour
    {
        public Camera WorldCamera;
        public SpriteRenderer[] Layers;
        private Vector3 origin;
        private void Awake(){if(WorldCamera!=null)origin=WorldCamera.transform.position;}
        private void LateUpdate()
        {
            if(WorldCamera==null||Layers==null)return;
            var cameraPosition=WorldCamera.transform.position;
            float height=WorldCamera.orthographicSize*2f;
            // Uniform cover plus 12% overscan. Source is finite, never declared seamless.
            float scale=Mathf.Max(height/22f,height*WorldCamera.aspect/44f)*1.12f;
            for(int i=0;i<Layers.Length;i++)
            {
                if(Layers[i]==null)continue;
                float weight=i*.045f;
                var drift=Vector3.ClampMagnitude((cameraPosition-origin)*weight,height*.045f);
                Layers[i].transform.position=new Vector3(cameraPosition.x-drift.x,cameraPosition.y-drift.y,10+i);
                Layers[i].transform.localScale=Vector3.one*scale;
            }
        }
    }
}
