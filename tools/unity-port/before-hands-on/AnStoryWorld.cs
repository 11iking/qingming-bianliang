using System.Collections.Generic;
using UnityEngine;
namespace Qingming
{
    public partial class BianliangWorld
    {
        readonly Dictionary<string,Transform> storyPeople=new Dictionary<string,Transform>();
        readonly List<Transform> storySigns=new List<Transform>();
        Transform homeSet,teaSet,marketSet,storyPerson;
        public Transform StoryPerson=>storyPerson;
        public static Vector3 StoryPosition(int chapter){if(chapter==0||chapter==13)return G(-25,.22f,9);if(chapter==1)return G(-18,.22f,9);if(chapter==7)return G(10,.22f,9);if(chapter==12)return G(-7,.22f,9);return G(-3,.22f,9.25f);}
        public void InitializeStory()
        {
            if(storyPeople.Count>0)return;
            string[] names={"安母","果行卖者","顾嫂","沈客","许娘子","何四","老周","阿绣","铺兵"};
            Color[] colors={new Color(.43f,.38f,.29f),new Color(.50f,.42f,.30f),new Color(.48f,.36f,.30f),new Color(.32f,.40f,.40f),new Color(.37f,.44f,.34f),new Color(.40f,.38f,.31f),new Color(.36f,.32f,.27f),new Color(.54f,.35f,.29f),new Color(.34f,.35f,.31f)};
            for(int i=0;i<names.Length;i++){
                var person=Instantiate(customer.gameObject,transform).transform;person.name="Story_"+names[i];
                foreach(var c in person.GetComponentsInChildren<Collider>())c.enabled=false;
                var collider=person.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(1,2,1);collider.center=new Vector3(0,.8f,0);
                (person.GetComponent<InteractionTarget>()??person.gameObject.AddComponent<InteractionTarget>()).id="story_person";
                foreach(var r in person.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial&&r.sharedMaterial.HasProperty("_Color")){var mat=r.material;mat.SetColor("_Color",Color.Lerp(mat.GetColor("_Color"),colors[i],.45f));}
                Sign(person,names[i],new Vector3(0,2.25f,0),.035f);storyPeople[names[i]]=person;person.gameObject.SetActive(false);
            }
            homeSet=CreateSet("安家 · 今日送梨",StoryPosition(0),false);
            marketSet=CreateSet("晨市 · 果行",StoryPosition(1),false);
            teaSet=CreateSet("许家茶坊",StoryPosition(7),true);
            foreach(var label in worldLabels)if(label.text.Contains("孙羊"))label.text="临河小酒肆";
        }
        Transform CreateSet(string title,Vector3 position,bool tea)
        {
            var root=new GameObject(title).transform;root.SetParent(transform,false);root.localPosition=position;
            Part(root,"台面",new Vector3(0,.78f,-2.5f),new Vector3(3.6f,.16f,1.25f),new Color(.42f,.34f,.23f));
            for(int i=0;i<2;i++)Part(root,"木脚",new Vector3(i==0?-1.4f:1.4f,.36f,-2.5f),new Vector3(.17f,.75f,.85f),new Color(.36f,.30f,.21f));
            Part(root,"布幌",new Vector3(-2.1f,2.7f,-2.8f),new Vector3(1.6f,.65f,.045f),new Color(.66f,.57f,.40f));
            Part(root,"幌杆",new Vector3(-2.85f,1.6f,-2.8f),new Vector3(.08f,3.2f,.08f),new Color(.4f,.32f,.22f));
            Sign(root,title,new Vector3(-2.1f,2.7f,-2.7f),.035f);
            var item=Instantiate(templates.Find(tea?"cup":"basket").gameObject,root).transform;item.gameObject.SetActive(true);item.localPosition=new Vector3(0,.89f,-2.5f);item.localScale=Vector3.one*(tea?1.8f:.8f);
            if(title.StartsWith("安家"))foreach(Transform child in item)if(child.childCount==3)child.gameObject.SetActive(false);
            var b=item.gameObject.AddComponent<BoxCollider>();b.size=Vector3.one;
            (item.GetComponent<InteractionTarget>()??item.gameObject.AddComponent<InteractionTarget>()).id="story_prop";
            if(tea){var jug=Instantiate(templates.Find("jug").gameObject,root).transform;jug.gameObject.SetActive(true);jug.localPosition=new Vector3(.8f,.89f,-2.5f);}
            root.gameObject.SetActive(false);return root;
        }
        void Part(Transform parent,string name,Vector3 pos,Vector3 size,Color color){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=size;var mat=new Material(Shader.Find("Qingming/InkToon"));mat.SetColor("_Color",color);go.GetComponent<Renderer>().material=mat;go.GetComponent<Collider>().enabled=false;}
        void Sign(Transform parent,string text,Vector3 pos,float scale){var go=new GameObject("姓名与店幌");go.transform.SetParent(parent,false);go.transform.localPosition=pos;storySigns.Add(go.transform);var label=go.AddComponent<TextMesh>();label.font=Resources.Load<Font>("Fonts/NotoSerifSC");label.text=text;label.fontSize=48;label.characterSize=scale;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=new Color(.25f,.22f,.17f);var m=new Material(Shader.Find("Qingming/WorldText"));m.mainTexture=label.font.material.mainTexture;go.GetComponent<MeshRenderer>().material=m;}
        void LateUpdate(){var camera=Camera.main;if(!camera)return;foreach(var sign in storySigns)if(sign.gameObject.activeInHierarchy){if(!sign.parent.name.StartsWith("Story_")){sign.rotation=Quaternion.Euler(0,180,0);continue;}var away=sign.position-camera.transform.position;away.y=0;if(away.sqrMagnitude>.001f)sign.rotation=Quaternion.LookRotation(away);}}
        public void SetStoryScene(DayState s,int mode)
        {
            InitializeStory();homeSet.gameObject.SetActive(s.chapter==0||s.chapter==13);marketSet.gameObject.SetActive(s.chapter==1);teaSet.gameObject.SetActive(s.chapter==7);
            foreach(var p in storyPeople.Values)p.gameObject.SetActive(false);
            customer.gameObject.SetActive(false);friend.gameObject.SetActive(false);
            string who=s.chapter==0||s.chapter==13?"安母":s.chapter==1?"果行卖者":s.chapter==4?"沈客":s.chapter==5||s.chapter==6?"何四":s.chapter==7?"许娘子":s.chapter==10?"老周":s.chapter==11?"阿绣":s.chapter==12?"铺兵":"顾嫂";
            if(s.chapter==6&&s.porterAgreed)who="老周";
            storyPerson=storyPeople[who];storyPerson.gameObject.SetActive(mode!=2);
            var ppos=player.localPosition;storyPerson.localPosition=ppos+new Vector3(0,0,-2.7f);storyPerson.localRotation=Quaternion.Euler(0,180,0);
            if(s.chapter==10)storyPerson.localPosition=G(-14,.1f,9.5f);
            if(s.chapter==11)storyPerson.localPosition=G(23,1.1f,11);
            if(s.chapter==5)storyPerson.localPosition=G(8,3.65f,.1f);
            worldLabels=GetComponentsInChildren<TextMesh>(true);foreach(var l in worldLabels)UpdateFontAtlas(l.font);
            foreach(var t in targets)t.Value.enabled=false;
            targets["basket"].enabled=targets["counter"].enabled=s.chapter>=2&&s.chapter<=4||s.chapter==8||s.chapter==9;
            targets["jug"].enabled=targets["cup"].enabled=s.chapter==10;
        }
    }
}
