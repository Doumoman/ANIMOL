using System;
using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Art.SunsetSanctumV02
{
    [DisallowMultipleComponent,RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class SunsetSanctumV02BeamRenderer : MonoBehaviour
    {
        [SerializeField] private Material beamMaterial;
        [SerializeField,Range(.025f,.16f)] private float widthCells=.11f;
        [SerializeField] private string sortingLayerName="Default";
        [SerializeField] private int sortingOrder=6;
        private Mesh mesh;private MeshRenderer rendererComponent;private MaterialPropertyBlock properties;
        private float pathLength,duration,elapsed;private bool active,externalProgress,hasSequence;
        private long lastSequence;
        private static readonly int LengthId=Shader.PropertyToID("_PathLength");
        private static readonly int ProgressId=Shader.PropertyToID("_PulseDistance");
        private static readonly int ActiveId=Shader.PropertyToID("_Active");
        public bool HasVisibleBeam { get { return mesh!=null && mesh.vertexCount>0; } }
        private void EnsureObjects()
        {
            if(rendererComponent==null)rendererComponent=GetComponent<MeshRenderer>();
            if(properties==null)properties=new MaterialPropertyBlock();
            if(mesh==null) { mesh=new Mesh {name="Sanctum owned light mesh"};mesh.MarkDynamic();GetComponent<MeshFilter>().sharedMesh=mesh; }
            rendererComponent.sharedMaterial=beamMaterial;
            rendererComponent.sortingLayerName=sortingLayerName;rendererComponent.sortingOrder=sortingOrder;
        }
        public void Configure(Material material,string layer,int order)
        {
            if(material==null)throw new ArgumentNullException("material");
            beamMaterial=material;sortingLayerName=layer;sortingOrder=order;EnsureObjects();ApplyProperties();
        }
        private static Vector2 Center(SanctumCell c) { return new Vector2(c.x+.5f,c.y+.5f); }
        private static Vector2 Direction(SanctumHeading h) { SanctumCell d=SanctumDirections.Delta(h);return new Vector2(d.x,d.y); }

        // Feed the exact route used for receivers/linked device state, not another visual trace.
        // Native emitter and reflector centers are logical-cell centers. Source hardware and
        // actors sort ABOVE this narrow beam; intact glass may reveal it through its alpha center.
        public void SetRoute(SanctumLightRoute route,Transform gridSpace,Vector2 gridOrigin,float cellSize)
        {
            if(route==null)throw new ArgumentNullException("route");
            if(gridSpace==null)throw new ArgumentNullException("gridSpace");
            if(!(cellSize>0f) || float.IsNaN(cellSize) || float.IsInfinity(cellSize))throw new ArgumentOutOfRangeException("cellSize");
            EnsureObjects();if(beamMaterial==null)throw new InvalidOperationException("Assign the latest Sanctum beam material.");
            mesh.Clear();active=false;externalProgress=false;elapsed=0;duration=0;pathLength=0;
            if(route.cells.Count==0) { rendererComponent.enabled=false;ApplyProperties();return; }
            var points=new List<Vector2>();points.Add(Center(route.emitter));
            foreach(SanctumLightCell item in route.cells)points.Add(Center(item.cell));
            SanctumLightCell last=route.cells[route.cells.Count-1];
            if(route.stop!=SanctumLightStop.Receiver)points.Add(Center(last.cell)+Direction(last.outgoing)*.5f);
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            float half=Mathf.Clamp(widthCells,.025f,.16f)*.5f;float distance=0;
            for(int i=0;i<points.Count;i++)
            {
                Vector2 before=i>0?(points[i]-points[i-1]).normalized:(points[1]-points[0]).normalized;
                Vector2 after=i+1<points.Count?(points[i+1]-points[i]).normalized:before;
                Vector2 normalA=new Vector2(-before.y,before.x),normalB=new Vector2(-after.y,after.x);
                Vector2 miter=(normalA+normalB).normalized;
                float denominator=Vector2.Dot(miter,normalB);
                if(denominator<.25f) { miter=normalB;denominator=1f; }
                Vector2 offset=miter*(half/denominator);
                if(i>0)distance+=Vector2.Distance(points[i-1],points[i]);
                Vector2 a=gridOrigin+(points[i]-offset)*cellSize,b=gridOrigin+(points[i]+offset)*cellSize;
                vertices.Add(transform.InverseTransformPoint(gridSpace.TransformPoint(new Vector3(a.x,a.y,0))));
                vertices.Add(transform.InverseTransformPoint(gridSpace.TransformPoint(new Vector3(b.x,b.y,0))));
                uv.Add(new Vector2(distance,0));uv.Add(new Vector2(distance,1));
                if(i>0) { int p=(i-1)*2;triangles.Add(p);triangles.Add(p+2);triangles.Add(p+1);
                    triangles.Add(p+1);triangles.Add(p+2);triangles.Add(p+3); }
            }
            pathLength=distance;
            mesh.indexFormat=vertices.Count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            rendererComponent.enabled=true;ApplyProperties();
        }
        public bool PlayCommittedAction(bool committed,long sequence,float visualDuration)
        {
            if(!committed || !HasVisibleBeam || (hasSequence && sequence<=lastSequence))return false;
            if(!(visualDuration>0) || float.IsNaN(visualDuration) || float.IsInfinity(visualDuration))throw new ArgumentOutOfRangeException("visualDuration");
            hasSequence=true;lastSequence=sequence;elapsed=0;duration=visualDuration;active=true;externalProgress=false;ApplyProperties();return true;
        }
        public void SetNormalizedProgress(float progress)
        {
            if(float.IsNaN(progress) || float.IsInfinity(progress))throw new ArgumentOutOfRangeException("progress");
            if(!active)return;externalProgress=true;elapsed=Mathf.Clamp01(progress)*duration;
            if(progress>=1)active=false;ApplyProperties();
        }
        public void CompleteAction() { active=false;externalProgress=false;ApplyProperties(); }
        public void HideBeam()
        { EnsureObjects();mesh.Clear();rendererComponent.enabled=false;active=false;elapsed=0;duration=0;pathLength=0;ApplyProperties(); }
        public void ResetActionSequence() { hasSequence=false;lastSequence=0; }
        private void Update()
        {
            if(!active || externalProgress)return;elapsed=Mathf.Min(duration,elapsed+Time.deltaTime);
            if(elapsed>=duration)active=false;ApplyProperties();
        }
        private void ApplyProperties()
        {
            if(rendererComponent==null || properties==null)return;
            properties.SetFloat(LengthId,pathLength);properties.SetFloat(ProgressId,duration>0?(elapsed/duration)*(pathLength+.6f)-.3f:0);
            properties.SetFloat(ActiveId,active?1:0);rendererComponent.SetPropertyBlock(properties);
        }
        private void OnDestroy() { if(mesh!=null) { if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh); } }
    }
}
