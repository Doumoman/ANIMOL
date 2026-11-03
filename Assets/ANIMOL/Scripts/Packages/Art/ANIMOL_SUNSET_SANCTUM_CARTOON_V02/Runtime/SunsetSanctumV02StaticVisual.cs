using UnityEngine;

namespace ANIMOL.Art.SunsetSanctumV02
{
    [DisallowMultipleComponent]
    public sealed class SunsetSanctumV02StaticVisual : MonoBehaviour
    {
        [SerializeField] private SunsetSanctumV02VisualCatalog catalog;
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private string expectedManifestSha256;
        private string lastError;
        private bool hasExplicitState;
        public string LastDiagnostic { get { return lastError; } }
        private void Awake()
        {
            if(target==null) { target=GetComponent<SpriteRenderer>();hasExplicitState=false; }
            // An inactive pooled object can be configured and bound before its first Awake.
            // Preserve that explicit latest binding; hide only an unbound serialized sprite.
            if(!hasExplicitState && target!=null) { target.sprite=null;target.enabled=false; }
        }
        public bool Configure(SunsetSanctumV02VisualCatalog latest,SpriteRenderer renderer,string pinnedCatalogSha256)
        { catalog=latest;target=renderer;expectedManifestSha256=pinnedCatalogSha256;
            hasExplicitState=false;
            if(target!=null) { target.sprite=null;target.enabled=false; }
            return Validate(); }
        public bool BindExactFile(string relativeFile)
        {
            if(!Validate())return false;
            SunsetSanctumV02VisualCatalog.Part part;string error;
            if(!catalog.TryGetPart(relativeFile,expectedManifestSha256,out part,out error))return Fail(error);
            if(part==null || part.Frame==null || part.Frame.Sprite==null)return Fail("Latest static sprite unloaded or missing: "+relativeFile);
            target.sprite=part.Frame.Sprite;target.enabled=true;hasExplicitState=true;lastError=null;return true;
        }
        private bool Validate()
        {
            if(target==null || catalog==null)return Fail("Missing latest catalog or renderer.");
            if(!SunsetSanctumV02VisualCatalog.IsHash(expectedManifestSha256,64))return Fail("Missing pinned manifest SHA256.");
            // TryGetPart below performs the cached full integrity check before exposing a sprite.
            return true;
        }
        private bool Fail(string error)
        {
            hasExplicitState=false;
            if(target!=null) { target.sprite=null;target.enabled=false; }
            if(lastError!=error)Debug.LogError("[Sunset Sanctum V02] "+error,this);
            lastError=error;return false;
        }
    }
}
