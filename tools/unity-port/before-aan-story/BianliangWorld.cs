using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Qingming
{
    public class InteractionTarget : MonoBehaviour { public string id; }
    public class BianliangWorld : MonoBehaviour
    {
        public Transform player,friend,customer,cargoBoat,rowBoat,oar,pearDisplay,templates;
        public Light sun;
        public bool crossing,night;
        public float boatProgress;
        public Dictionary<string,BoxCollider> targets=new Dictionary<string,BoxCollider>();
        class Moving {
            public Transform node,left,right; public float phase,speed; public Vector3 position; public Quaternion rotation;
        }
        readonly List<Moving> walkers=new List<Moving>(),willows=new List<Moving>(),banners=new List<Moving>(),lamps=new List<Moving>(),leaves=new List<Moving>();
        readonly List<Transform> boats=new List<Transform>(),actors=new List<Transform>();
        readonly List<Light> nightLights=new List<Light>();
        readonly List<Renderer> lanterns=new List<Renderer>();
        float elapsed,flourish;
        bool initialized;
        TextMesh[] worldLabels;
        Coroutine oarMotion;
        public static Vector3 G(float x,float y,float z)=>new Vector3(x,y,-z);
        Transform Node(string path)=>path=="."?transform:transform.Find(path);
        public void Initialize()
        {
            if(initialized)return;
            initialized=true;
            worldLabels=GetComponentsInChildren<TextMesh>(true);
            Font.textureRebuilt+=UpdateFontAtlas;
            foreach(var label in worldLabels)UpdateFontAtlas(label.font);
            var meta=JObject.Parse(Resources.Load<TextAsset>("Imported/WorldMetadata").text);
            player=Node((string)meta["player"]);friend=Node((string)meta["friend"]);customer=Node((string)meta["customer"]);
            cargoBoat=Node((string)meta["cargo_boat"]);rowBoat=Node((string)meta["row_boat"]);oar=Node((string)meta["oar"]);
            pearDisplay=Node((string)meta["pear_display"]);templates=transform.Find("HeldTemplates");
            sun=Node((string)meta["sun"]).GetComponent<Light>();
            foreach(var key in new[]{"walkers","willows","banners","wind_lanterns","drifting_leaves"}){
                var list=key=="walkers"?walkers:key=="willows"?willows:key=="banners"?banners:key=="wind_lanterns"?lamps:leaves;
                foreach(var entry in (JArray)meta[key]){
                    var node=Node((string)entry["node"]);if(!node)continue;
                    list.Add(new Moving{node=node,left=node.Find("LeftArm"),right=node.Find("RightArm"),
                        position=node.localPosition,rotation=node.localRotation,phase=(float?)entry["phase"]??0,speed=(float?)entry["speed"]??0});
                }
            }
            foreach(var p in (JArray)meta["boats"])boats.Add(Node((string)p));
            foreach(var p in (JArray)meta["actors"])actors.Add(Node((string)p));
            foreach(var p in (JArray)meta["lanterns"]){var n=Node((string)p);var r=n.GetComponent<Renderer>();if(r)lanterns.Add(r);}
            foreach(var p in (JArray)meta["night_lights"]){
                var n=Node((string)p);var l=n.GetComponent<Light>()??n.gameObject.AddComponent<Light>();
                l.type=LightType.Point;l.renderMode=LightRenderMode.ForceVertex;l.range=5;l.intensity=.65f;l.color=new Color(1,.65f,.28f);nightLights.Add(l);
            }
            var sizes=new Dictionary<string,Vector3>{
                {"basket",new Vector3(1.2f,1.2f,1.2f)},{"counter",new Vector3(3.1f,.45f,1.8f)},
                {"customer",new Vector3(.8f,1.9f,.7f)},{"jug",new Vector3(.7f,.8f,.7f)},{"cup",new Vector3(.5f,.4f,.5f)}};
            foreach(var pair in (JObject)meta["targets"]){
                var n=Node((string)pair.Value);var b=n.gameObject.GetComponent<BoxCollider>()??n.gameObject.AddComponent<BoxCollider>();
                b.size=sizes[pair.Key];targets.Add(pair.Key,b);
                (n.gameObject.GetComponent<InteractionTarget>()??n.gameObject.AddComponent<InteractionTarget>()).id=pair.Key;
            }
            SetTime(0);
        }
        public void Stock(int count){for(int i=0;i<pearDisplay.childCount;i++)pearDisplay.GetChild(i).gameObject.SetActive(i<count);}
        public void SetTargets(int chapter)
        {
            foreach(var t in targets)t.Value.enabled=(chapter==0||chapter==4)?(t.Key=="basket"||t.Key=="counter"):
                chapter==1?(t.Key=="counter"||t.Key=="customer"):chapter==5&&(t.Key=="jug"||t.Key=="cup");
        }
        public void SetTime(int chapter)
        {
            night=chapter>=5;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight=night?new Color(.34f,.31f,.25f):new Color(.50f,.46f,.37f);
            sun.color=night?new Color(.78f,.86f,.88f):chapter==4?new Color(1,.8f,.57f):new Color(1,.97f,.92f);
            sun.intensity=night?.28f:.62f;
            sun.shadowStrength=.38f;
            foreach(var light in nightLights)light.enabled=night;
            foreach(var r in lanterns){var m=r.material;m.SetColor("_Color",night?new Color(1,.75f,.46f):new Color(.62f,.42f,.33f));m.SetFloat("_Glow",night?.6f:0);}
        }
        void Update()
        {
            if(!initialized)return;
            float dt=Time.deltaTime;elapsed+=dt;
            foreach(var n in willows)n.node.localRotation=Quaternion.AngleAxis(Mathf.Sin(elapsed*.7f+n.phase)*.916f,Vector3.forward);
            foreach(var n in banners)n.node.localRotation=Quaternion.AngleAxis(-(.07f+Mathf.Sin(elapsed*1.5f+n.phase)*.025f)*Mathf.Rad2Deg,Vector3.right);
            foreach(var n in lamps){
                n.node.localPosition=n.position+new Vector3(Mathf.Sin(elapsed*1.1f+n.phase)*.055f,Mathf.Cos(elapsed*2.2f+n.phase)*.01f,0);
                n.node.localRotation=Quaternion.AngleAxis(Mathf.Sin(elapsed*1.1f+n.phase)*3.72f,Vector3.forward);
            }
            foreach(var n in leaves){
                n.node.localPosition+=new Vector3(dt*n.speed,-dt*.12f,-Mathf.Sin(elapsed+n.phase)*dt*.13f);
                n.node.Rotate(0,-dt*28.65f,0,Space.Self);
                var p=n.node.localPosition;if(p.x>32||p.y<.3f)n.node.localPosition=new Vector3(-31,3+Mathf.Repeat(n.phase,5),-Mathf.Repeat(n.phase*3,21)+4);
            }
            player.localScale=new Vector3(1,1+Mathf.Sin(elapsed*1.7f)*.007f,1);
            friend.localScale=new Vector3(1,1+Mathf.Sin(elapsed*1.6f+1)*.009f,1);
            customer.localScale=new Vector3(1,1+Mathf.Sin(elapsed*1.8f+2)*.007f,1);
            foreach(var n in walkers){
                var p=n.node.localPosition;p.x+=dt*n.speed;if(Mathf.Abs(p.x)>31)p.x=-Mathf.Sign(p.x)*30.8f;
                p.y=.22f+Mathf.Sin(elapsed*4+n.phase)*.018f;n.node.localPosition=p;
                if(n.left)n.left.localRotation=Quaternion.Euler(-Mathf.Sin(elapsed*4+n.phase)*12.6f,0,0);
                if(n.right)n.right.localRotation=Quaternion.Euler(Mathf.Sin(elapsed*4+n.phase)*12.6f,0,0);
            }
            for(int i=0;i<actors.Count;i++){
                var a=actors[i];a.localRotation=Quaternion.Euler(0,elapsed<flourish?elapsed*313:-Mathf.Sin(elapsed*.8f+i)*17.2f,0);
                a.Find("LeftArm").localRotation=Quaternion.Euler(0,0,(.8f+Mathf.Sin(elapsed*1.4f+i)*.5f)*Mathf.Rad2Deg);
                a.Find("RightArm").localRotation=Quaternion.Euler(0,0,(-.8f-Mathf.Sin(elapsed*1.2f+i)*.5f)*Mathf.Rad2Deg);
            }
            for(int i=0;i<boats.Count;i++){
                var p=boats[i].localPosition;p.x+=dt*.12f;if(p.x>34)p.x=-34;p.y=Mathf.Sin(elapsed*.7f+i)*.04f;boats[i].localPosition=p;
            }
            var cargo=cargoBoat.localPosition;if(crossing)cargo.x=Mathf.MoveTowards(cargo.x,14,dt*1.6f);cargo.y=Mathf.Sin(elapsed)*.035f;cargoBoat.localPosition=cargo;
            var row=rowBoat.localPosition;row.x=Mathf.Lerp(row.x,-17+boatProgress*19,1-Mathf.Exp(-dt*2));row.y=Mathf.Sin(elapsed*1.3f)*.04f;rowBoat.localPosition=row;
        }
        public void Gesture(Transform person){if(person)StartCoroutine(Wave(person.Find("RightArm")));}
        IEnumerator Wave(Transform arm){if(!arm)yield break;for(float t=0;t<.9f;t+=Time.deltaTime){arm.localRotation=Quaternion.Euler(Mathf.Sin(t/.9f*Mathf.PI)*60,0,0);yield return null;}arm.localRotation=Quaternion.identity;}
        public void Flourish(){flourish=elapsed+1.2f;}
        public void Stroke(string side){if(oarMotion!=null)StopCoroutine(oarMotion);oarMotion=StartCoroutine(Oar(side));}
        IEnumerator Oar(string side){for(float t=0;t<.5f;t+=Time.deltaTime){oar.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t/.5f*Mathf.PI)*(side=="left"?52:-52));yield return null;}oar.localRotation=Quaternion.identity;}
        public void Goods(){StartCoroutine(PopGoods());}
        void UpdateFontAtlas(Font font)
        {
            if(worldLabels==null||!font)return;
            foreach(var label in worldLabels)if(label&&label.font==font)
                label.GetComponent<Renderer>().sharedMaterial.mainTexture=font.material.mainTexture;
        }
        void OnDestroy(){Font.textureRebuilt-=UpdateFontAtlas;}
        IEnumerator PopGoods(){for(float t=0;t<.45f;t+=Time.deltaTime){float a=Mathf.Clamp01(t/.45f);float s=1+Mathf.Pow(1-a,3)*(2.7f*a-1);foreach(Transform p in pearDisplay)p.localScale=Vector3.one*Mathf.Max(.1f,s);yield return null;}foreach(Transform p in pearDisplay)p.localScale=Vector3.one;}
    }
}
