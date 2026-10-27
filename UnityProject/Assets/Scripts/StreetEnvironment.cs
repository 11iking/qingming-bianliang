using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Qingming
{
    // Decorative, Song-inspired set dressing. Kept separate from story props and navigation.
    public sealed class StreetEnvironment : MonoBehaviour
    {
        sealed class Batch {
            public readonly List<Vector3> v=new List<Vector3>(),n=new List<Vector3>();
            public readonly List<int> t=new List<int>();
        }
        readonly Dictionary<int,Batch> batches=new Dictionary<int,Batch>();
        readonly Dictionary<PrimitiveType,Mesh> shapes=new Dictionary<PrimitiveType,Mesh>();
        readonly List<Mesh> ownedMeshes=new List<Mesh>();
        readonly List<Material> ownedMaterials=new List<Material>();
        readonly List<Renderer> replacedFoliage=new List<Renderer>();
        readonly System.Random random=new System.Random(1085);
        // Preserve the existing palette: stone, timber, linen, muted cinnabar and celadon.
        readonly string[] palette={"b6ad94","c2b89f","a89f89","bcb098","756448","a38a62","5c503e","b9a481","92715a","768371","aa785f","c6b89a","727c59","536346","d0ba8e","8f9686","423e32"};
        const int Wood=4,Pale=5,Dark=6,Cloth=7,Pottery=8,Jade=9,Red=10,Linen=11,Leaf=12,Moss=13,Rope=14,Stone=15,Ink=16;
        public int DetailCount {get;private set;}
        public int BatchCount=>ownedMeshes.Count;
        bool built;
        static Vector3 P(float x,float y,float z)=>new Vector3(x,y,-z);
        float Rand(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        public void Build(IEnumerable<Transform> trees)
        {
            if(built)return;built=true;
            foreach(PrimitiveType type in new[]{PrimitiveType.Cube,PrimitiveType.Cylinder,PrimitiveType.Sphere}){
                var temp=GameObject.CreatePrimitive(type);temp.SetActive(false);
                shapes[type]=temp.GetComponent<MeshFilter>().sharedMesh;Destroy(temp);
            }
            Paving();Quays();Shops();Courtyards();Willows(trees);
            var shader=Shader.Find("Qingming/InkToon");
            foreach(var entry in batches){
                var data=entry.Value;var mesh=new Mesh{name="Street detail "+entry.Key,indexFormat=IndexFormat.UInt32};
                mesh.SetVertices(data.v);mesh.SetNormals(data.n);mesh.SetTriangles(data.t,0);mesh.RecalculateBounds();ownedMeshes.Add(mesh);
                var material=new Material(shader){name="Street pigment "+palette[entry.Key]};
                ColorUtility.TryParseHtmlString("#"+palette[entry.Key],out var color);
                material.SetColor("_Color",color);material.SetFloat("_Outline",entry.Key<4?0:.38f);ownedMaterials.Add(material);
                material.SetFloat("_SurfaceDetail",entry.Key<4?1:entry.Key==Wood||entry.Key==Pale?2:0);
                var go=new GameObject(material.name);go.transform.SetParent(transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=entry.Key<4?ShadowCastingMode.Off:ShadowCastingMode.On;
            }
            batches.Clear();shapes.Clear();
            Debug.Log("QINGMING_STREET_READY: "+DetailCount+" details, "+BatchCount+" combined meshes.");
        }
        void Add(PrimitiveType type,Vector3 p,Vector3 size,int color,Quaternion rotation)
        {
            if(!batches.TryGetValue(color,out var b)){b=new Batch();batches[color]=b;}
            var mesh=shapes[type];var vertices=mesh.vertices;var normals=mesh.normals;var triangles=mesh.triangles;
            var matrix=Matrix4x4.TRS(p,rotation,size);var normalMatrix=matrix.inverse.transpose;int start=b.v.Count;
            for(int i=0;i<vertices.Length;i++){b.v.Add(matrix.MultiplyPoint3x4(vertices[i]));b.n.Add(normalMatrix.MultiplyVector(normals[i]).normalized);}
            foreach(int i in triangles)b.t.Add(start+i);DetailCount++;
        }
        void Box(float x,float y,float z,float w,float h,float d,int color,float yaw=0,float tilt=0)=>Add(PrimitiveType.Cube,P(x,y,z),new Vector3(w,h,d),color,Quaternion.Euler(tilt,-yaw,0));
        void Ball(float x,float y,float z,float w,float h,float d,int color)=>Add(PrimitiveType.Sphere,P(x,y,z),new Vector3(w,h,d),color,Quaternion.identity);
        void Cylinder(float x,float y,float z,float radius,float height,int color)=>Add(PrimitiveType.Cylinder,P(x,y,z),new Vector3(radius*2,height*.5f,radius*2),color,Quaternion.identity);
        void Beam(Vector3 a,Vector3 b,float width,int color)=>Add(PrimitiveType.Cube,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),color,Quaternion.FromToRotation(Vector3.up,b-a));
        void Ring(float x,float y,float z,float radius,int color,int segments=20,float thickness=.025f)
        {
            for(int k=0;k<segments;k++){float a=k*Mathf.PI*2/segments,b=(k+1)*Mathf.PI*2/segments;
                Beam(P(x+Mathf.Cos(a)*radius,y,z+Mathf.Sin(a)*radius),P(x+Mathf.Cos(b)*radius,y,z+Mathf.Sin(b)*radius),thickness,color);}
        }
        void Paving()
        {
            // Thin, staggered slabs leave the player's existing foot height and route unchanged.
            for(int side=-1;side<=1;side+=2)for(int row=0;row<23;row++)for(int col=0;col<78;col++){
                float x=-35+col*.91f+(row%2)*.455f,z=side*(6.05f+row*.60f);
                if(Mathf.Abs(x)>35)continue;
                int shade=random.Next(4);
                Box(x,.221f,z,.867f+Rand(-.025f,.015f),.025f,.557f+Rand(-.018f,.015f),shade,Rand(-.65f,.65f));
            }
            // Drainage runs and occasional inset repair stones break the uniform grid.
            foreach(int side in new[]{-1,1})for(int i=0;i<140;i++){
                float x=-35+i*.5f;Box(x,.239f,side*6.02f,.46f,.012f,.12f,2);
                if(i%5==0)for(int j=0;j<3;j++)Box(x-.13f+j*.12f,.247f,side*6.02f,.035f,.008f,.13f,Dark);
            }
            for(int i=0;i<110;i++){
                float x=Rand(-33,33),z=Rand(6.5f,19);if(i%2==0)z=-z;
                Box(x,.238f,z,Rand(.08f,.27f),.008f,.018f,2,Rand(-18,18));
            }
        }
        void Quays()
        {
            for(int side=-1;side<=1;side+=2){
                for(int row=0;row<3;row++)for(int j=0;j<90;j++){
                    float x=-35+j*.79f+(row%2)*.39f;
                    Box(x,-.18f+row*.16f,side*5.01f,.76f,.145f,.10f,row==0?Moss:Stone);
                }
                foreach(float x in new[]{-29f,-22f,-8f,17f,28f}){
                    Cylinder(x,.73f,side*5.2f,.13f,.63f,Wood);
                    for(int n=0;n<4;n++)Ring(x,.66f+n*.035f,side*5.2f,.15f,Rope,12,.025f);
                    for(int n=0;n<4;n++)Ring(x+.42f,.46f+n*.014f,side*5.42f,.16f+n*.04f,Rope,18,.024f);
                }
                for(int i=0;i<25;i++){
                    float x=-33+i*2.7f;if(x>3&&x<13)continue;
                    for(int j=0;j<4;j++){
                        float z=side*4.95f,tip=Rand(.4f,.8f);
                        Beam(P(x+j*.12f,-.05f,z),P(x+j*.12f+.12f,tip,z+side*.13f),.018f,Leaf);
                        Beam(P(x+j*.12f+.07f,tip*.6f,z),P(x+j*.12f-.17f,tip*.9f,z+.15f),.027f,Leaf);
                    }
                }
            }
            foreach(float x in new[]{-24f,-9f,17f,27f}){
                // Timber repairs and tied cargo at the existing landing stages.
                for(int j=0;j<7;j++)Box(x+j*.24f,.325f,4.12f,.20f,.025f,2.75f,j%2==0?Wood:Pale);
                Crate(x+.45f,5.95f,.22f,.55f);Jar(x+1.25f,6.15f,.23f,.55f,Pottery);
            }
        }
        void Shops()
        {
            for(int i=0;i<8;i++)Facade(-28+i*8,-9.35f,5.7f,1,i);
            Facade(-25,14.9f,5.7f,-1,2);
            Facade(0,17.9f,5.5f,-1,1);Facade(10,17.9f,5.7f,-1,4);
            Facade(-14,18.9f,5.7f,-1,0);Facade(-6.8f,18.9f,4.1f,-1,5);
            // Storage beside each working stall, outside its approach and exchange surface.
            foreach(float x in new[]{-20f,-7f,6f,18f}){
                Crate(x,14.7f,.23f,.8f);Crate(x+.1f,14.7f,1.03f,.62f);
                Jar(x+.9f,14.9f,.23f,.72f,Pottery);Basket(x-1,14.8f,.23f,.38f);
            }
            // A textile display and a produce rack give adjacent shops distinct trades.
            for(int i=0;i<5;i++){
                Box(11.8f,1.15f+i*.13f,16.95f,1.3f,.10f,.68f,i%2==0?Jade:Red);
                Cylinder(-21.7f+i*.24f,1.04f,14.4f,.10f,.16f,Pottery);
            }
            Cart(-29,12.4f);Cart(20,18.6f);
        }
        void Facade(float x,float z,float width,int face,int variant)
        {
            // A raised threshold and door recess sit against the original facade.
            Box(x,.27f,z+face*.35f,1.55f,.13f,.58f,Stone);
            Box(x,1.18f,z+face*.05f,.96f,1.9f,.09f,Dark);
            for(int n=0;n<6;n++)Box(x-.40f+n*.16f,1.18f,z+face*.12f,.135f,1.85f,.055f,Wood);
            Box(x,2.23f,z+face*.10f,1.25f,.13f,.16f,Pale);
            for(int s=-1;s<=1;s+=2){
                float xx=x+s*width*.28f;
                Box(xx+s*.62f,1.53f,z+face*.28f,.42f,1.10f,.07f,Wood,s*face*23);
                for(int k=0;k<4;k++)Box(xx+s*.62f,1.18f+k*.23f,z+face*.32f,.40f,.045f,.07f,Pale,s*face*23);
            }
            // Slender supports, stitched awning strips and projecting brackets.
            int fabric=variant%3==0?Jade:variant%3==1?Linen:Red;
            float span=width*.84f;
            for(int j=0;j<9;j++){
                float xx=x-span*.5f+(j+.5f)*span/9;
                Box(xx,2.86f,z+face*.65f,span/9-.015f,.045f,1.5f,j%3==0?Cloth:fabric,0,face*9);
                Box(xx,2.67f,z+face*1.40f,span/9-.02f,.17f,.035f,fabric);
            }
            foreach(int s in new[]{-1,1}){
                float xx=x+s*span*.5f;
                Beam(P(xx,.23f,z+face*1.36f),P(xx,2.79f,z+face*1.36f),.075f,Wood);
                Beam(P(xx,2.35f,z+face*.10f),P(xx,2.85f,z+face*.66f),.07f,Pale);
            }
            Box(x,2.78f,z+face*1.36f,span+.12f,.07f,.07f,Wood);
            Jar(x-width*.43f,z+face*.38f,.23f,.65f,variant%2==0?Pottery:Jade);
            Basket(x+width*.42f,z+face*.46f,.23f,.32f);
            // Bench and a shallow display shelf stay close to the wall.
            float bx=x+width*.27f;
            Box(bx,.75f,z+face*.45f,1.1f,.10f,.45f,Pale);
            foreach(float dx in new[]{-.4f,.4f})Box(bx+dx,.48f,z+face*.45f,.08f,.48f,.32f,Wood);
        }
        void Crate(float x,float z,float bottom,float size)
        {
            Box(x,bottom+size*.5f,z,size*.94f,size*.94f,size*.94f,Dark);
            for(int j=0;j<5;j++){
                float offset=(j-2)*size*.19f;
                Box(x+offset,bottom+size,z,size*.17f,.035f,size,Pale);
                foreach(int side in new[]{-1,1}){
                    Box(x+offset,bottom+size*.5f,z+side*size*.5f,size*.17f,size,.035f,Wood);
                    Box(x+side*size*.5f,bottom+size*.5f,z+offset,.035f,size,size*.17f,Wood);
                }
            }
            foreach(int side in new[]{-1,1})Beam(P(x-size*.42f,bottom+.08f,z+side*(size*.5f+.025f)),P(x+size*.42f,bottom+size-.08f,z+side*(size*.5f+.025f)),.055f,Pale);
        }
        void Jar(float x,float z,float bottom,float size,int color)
        {
            Ball(x,bottom+size*.40f,z,size*.7f,size*.8f,size*.7f,color);
            Cylinder(x,bottom+size*.78f,z,size*.20f,size*.16f,color);
            Cylinder(x,bottom+size*.87f,z,size*.175f,.015f,Ink);
            Ring(x,bottom+size*.88f,z,size*.21f,color,20,.04f);
        }
        void Basket(float x,float z,float bottom,float radius)
        {
            Cylinder(x,bottom+.12f,z,radius,.24f,Wood);
            Cylinder(x,bottom+.245f,z,radius*.9f,.015f,Dark);
            for(int j=0;j<6;j++)Ring(x,bottom+.025f+j*.045f,z,radius+.005f,Rope,16,.025f);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Beam(P(x+Mathf.Cos(a)*radius,bottom,z+Mathf.Sin(a)*radius),P(x+Mathf.Cos(a)*radius,bottom+.27f,z+Mathf.Sin(a)*radius),.018f,Pale);}
            for(int i=0;i<4;i++)Ball(x+Rand(-.15f,.15f),bottom+.27f,z+Rand(-.15f,.15f),.13f,.14f,.13f,Leaf);
        }
        void Cart(float x,float z)
        {
            Box(x,.72f,z,1.45f,.12f,2.0f,Wood);
            for(int k=0;k<6;k++)Box(x-.64f+k*.255f,.8f,z,.23f,.045f,1.95f,Pale);
            foreach(int s in new[]{-1,1}){
                for(int k=0;k<3;k++)Box(x+s*.70f,.92f+k*.15f,z,.06f,.12f,1.9f,Wood);
                Beam(P(x+s*.52f,.76f,z+.2f),P(x+s*.52f,1.04f,z+2.2f),.07f,Wood);
                // Spoked wheels in the vertical YZ plane.
                for(int k=0;k<20;k++){float a=k*Mathf.PI/10,b=(k+1)*Mathf.PI/10;
                    Beam(P(x+s*.83f,.65f+Mathf.Cos(a)*.43f,z+Mathf.Sin(a)*.43f),P(x+s*.83f,.65f+Mathf.Cos(b)*.43f,z+Mathf.Sin(b)*.43f),.06f,Dark);
                    if(k%2==0)Beam(P(x+s*.83f,.65f,z),P(x+s*.83f,.65f+Mathf.Cos(a)*.42f,z+Mathf.Sin(a)*.42f),.035f,Pale);
                }
            }
            Crate(x,z-.4f,.84f,.64f);Basket(x+.18f,z+.45f,.84f,.33f);
        }
        void Courtyards()
        {
            foreach(float x in new[]{-31f,-18f,18f,30f}){
                // Bamboo fences frame the side alleys without closing the main street.
                for(int i=0;i<15;i++)Cylinder(x,.9f,16+i*.19f,.035f,1.35f,Pale);
                foreach(float y in new[]{.5f,1.2f})Beam(P(x,y,15.9f),P(x,y,18.8f),.045f,Wood);
                for(int i=0;i<8;i++){
                    float xx=x+Rand(-.4f,.4f),zz=Rand(16,18.8f);
                    Beam(P(xx,.23f,zz),P(xx+.16f,Rand(.5f,1),zz+.1f),.025f,Leaf);
                }
            }
            // Sagging ropes with narrow lengths of drying cloth, in the rear alleys.
            foreach(float x in new[]{-20f,18f}){
                Beam(P(x,3.0f,17),P(x+4,3.0f,17),.025f,Rope);
                for(int i=0;i<5;i++)Box(x+.5f+i*.65f,2.5f,17,.49f,.8f,.035f,i%2==0?Linen:Jade,0,4);
            }
        }
        void Willows(IEnumerable<Transform> trees)
        {
            foreach(var tree in trees){
                // Retain the original animated trunk, replace the oversized oval foliage.
                foreach(var renderer in tree.GetComponentsInChildren<MeshRenderer>()){
                    var material=renderer.sharedMaterial;if(!material||!material.HasProperty("_Color"))continue;
                    Color c=material.GetColor("_Color");
                    if(c.g>c.r*1.04f&&c.g>c.b*1.15f){renderer.enabled=false;replacedFoliage.Add(renderer);}
                }
                Vector3 basePoint=transform.InverseTransformPoint(tree.position);
                for(int branch=0;branch<7;branch++){
                    float angle=branch*Mathf.PI*2/7;
                    Vector3 tip=basePoint+new Vector3(Mathf.Cos(angle)*1.65f,Rand(4.25f,4.95f),Mathf.Sin(angle)*1.45f);
                    for(int n=0;n<22;n++){
                        Vector3 start=tip+new Vector3(Rand(-.65f,.65f),Rand(-.12f,.2f),Rand(-.55f,.55f));
                        LeafBlade(start,start+new Vector3(Rand(-.5f,.5f),Rand(-.12f,.04f),Rand(-.4f,.4f)),Rand(.10f,.22f),n%3==0?Moss:Leaf);
                    }
                    for(int strand=0;strand<4;strand++){
                        Vector3 start=tip+new Vector3(Rand(-.65f,.65f),0,Rand(-.45f,.45f));
                        Vector3 previous=start;float length=Rand(1.25f,2.25f);
                        for(int step=1;step<=10;step++){
                            float t=step/10f;Vector3 next=start+new Vector3(.3f*t*t,-length*t,.12f*Mathf.Sin(t*3));
                            Beam(previous,next,.012f,Moss);
                            for(int side=-1;side<=1;side+=2){
                                Vector3 end=next+new Vector3(side*Rand(.08f,.17f),-Rand(.18f,.32f),side*.06f);
                                LeafBlade(next,end,.055f,step%3==0?Moss:Leaf);
                            }
                            previous=next;
                        }
                    }
                }
            }
        }
        void LeafBlade(Vector3 a,Vector3 b,float width,int color)
        {
            if((b-a).sqrMagnitude<.001f)return;
            if(!batches.TryGetValue(color,out var batch)){batch=new Batch();batches[color]=batch;}
            Vector3 across=Vector3.Cross((b-a).normalized,new Vector3(.4f,.3f,1)).normalized*width;
            Vector3 mid=Vector3.Lerp(a,b,.42f);Vector3 normal=Vector3.Cross(mid+across-a,b-a).normalized;
            Vector3[] points={a,mid+across,b,mid-across};int start=batch.v.Count;
            for(int side=0;side<2;side++)for(int i=0;i<4;i++){batch.v.Add(points[i]);batch.n.Add(side==0?normal:-normal);}
            foreach(int i in new[]{0,1,2,0,2,3,4,6,5,4,7,6})batch.t.Add(start+i);DetailCount++;
        }
        void OnDestroy(){foreach(var mesh in ownedMeshes)Destroy(mesh);foreach(var mat in ownedMaterials)Destroy(mat);foreach(var r in replacedFoliage)if(r)r.enabled=true;}
    }
}
