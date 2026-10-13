using UnityEngine;
using UnityEngine.UI;
namespace ANIMOL.ProductionV1
{
    public sealed class ProductionLoadingAnimation : MonoBehaviour
    {
        public Sprite[] Frames;
        private Image image;
        private void Awake()=>image=GetComponent<Image>();
        private void Update(){if(image!=null&&Frames!=null&&Frames.Length==8)image.sprite=Frames[Mathf.FloorToInt(Time.unscaledTime*8)%8];}
    }
}
