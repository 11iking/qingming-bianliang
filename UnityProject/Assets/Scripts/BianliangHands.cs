using System;
using System.Collections.Generic;
using UnityEngine;
namespace Qingming
{
    public partial class BianliangDemo
    {
        HandsWork work;
        Transform workRoot;
        readonly Dictionary<int,Transform> workObjects=new Dictionary<int,Transform>();
        readonly Dictionary<string,Vector3> workZones=new Dictionary<string,Vector3>();
        readonly List<Renderer> nearDecorations=new List<Renderer>();
        Vector3 sceneAnchor,workAnchor;
        Vector2 workMouseDown;
        string workHover="",workReply="";
        bool resolvingWork,workDragging;
        float workTurn,stepSound,replyUntil;
        LineRenderer hoverRing;
        static readonly Vector3[] DeliveryStops={G(-22,.22f,10),G(-9,.22f,10),G(10,.22f,8)};
        bool BeginWork(string id)
        {
            if(work!=null)return true;
            if(!state.Options().Exists(a=>a.id==id&&!a.disabled))return false;
            work=HandsWork.Create(id,state);if(work==null)return false;
            held="";UpdateHeld();viewMode=1;world.player.gameObject.SetActive(false);hand.gameObject.SetActive(true);
            interactionOpen=false;workAnchor=sceneAnchor;look=Vector2.zero;zoom=65;
            world.player.localPosition=sceneAnchor;workReply="";workTurn=0;
            world.SetHandsStage(true,work.mode==HandsWork.Mode.Carry);
            BuildWorkScene();SetCameraGoals();cam.transform.SetPositionAndRotation(goalPosition,Quaternion.LookRotation(goalTarget-goalPosition));
            // Clear only small foreground decorations that intersect the working view.
            foreach(var r in world.GetComponentsInChildren<Renderer>())if(r.enabled&&!r.transform.IsChildOf(workRoot)&&!r.transform.IsChildOf(world.StoryPerson)&&!r.transform.IsChildOf(world.player)&&r.bounds.size.magnitude<4f&&r.bounds.SqrDistance(cam.transform.position)<6.25f){nearDecorations.Add(r);r.enabled=false;}
            return true;
        }
        void BuildWorkScene()
        {
            ClearWorkObjects();
            workRoot=new GameObject("Hands-on · "+work.action).transform;workRoot.SetParent(world.transform,false);
            workObjects.Clear();workZones.Clear();
            bool carry=work.mode==HandsWork.Mode.Carry;
            if(!carry){
                WorkBox("木案",workAnchor+new Vector3(0,.68f,-1.85f),new Vector3(5.8f,.16f,2.55f),Hex("705b3c"),true);
                foreach(float x in new[]{-2.5f,2.5f})WorkBox("案脚",workAnchor+new Vector3(x,.29f,-1.85f),new Vector3(.15f,.6f,1.8f),Hex("5c4b32"));
                AddWorkZone("source",workAnchor+new Vector3(1.4f,.79f,-1.85f),new Vector3(2.25f,.10f,2.15f),Hex("aa9468"));
                if(work.mode==HandsWork.Mode.Inspect){
                    AddWorkZone("target",workAnchor+new Vector3(-1.45f,.79f,-1.28f),new Vector3(2.25f,.1f,.94f),Hex("789482"));
                    AddWorkZone("bad",workAnchor+new Vector3(-1.45f,.79f,-2.45f),new Vector3(2.25f,.1f,.94f),Hex("a37156"));
                }else AddWorkZone("target",workAnchor+new Vector3(-1.4f,.79f,-1.85f),new Vector3(2.25f,.1f,2.15f),Hex("789482"));
                world.PlaceHandsPartner(workAnchor+new Vector3(0,0,-3.65f));
            }else{
                AddWorkZone("source",workAnchor+new Vector3(.6f,.05f,-.8f),new Vector3(1.1f,.1f,1.1f),Hex("aa9468"));
                AddWorkZone("target",G(10,.27f,9),new Vector3(1.8f,.1f,1.8f),Hex("789482"));
                for(int j=0;j<DeliveryStops.Length;j++)RouteRing(DeliveryStops[j]+Vector3.up*.02f,j==work.waypoint?Jade:Muted);
            }
            foreach(var item in work.items){
                Transform obj;
                if(item.kind=="pear"||item.kind=="basket"){
                    obj=Instantiate(world.templates.Find(item.kind).gameObject,workRoot).transform;obj.gameObject.SetActive(true);
                    obj.localScale=Vector3.one*(item.kind=="basket"?.85f:.6f);
                    if(item.kind=="basket"&&state.chapter==0)foreach(Transform child in obj)if(child.childCount==3)child.gameObject.SetActive(false);
                    if(item.bad){var bruise=GameObject.CreatePrimitive(PrimitiveType.Sphere);bruise.name="底面碰伤";bruise.transform.SetParent(obj,false);bruise.transform.localPosition=new Vector3(0,.04f,.199f);bruise.transform.localScale=new Vector3(.16f,.17f,.048f);Destroy(bruise.GetComponent<Collider>());var m=new Material(Shader.Find("Qingming/InkToon"));m.SetColor("_Color",Hex("68422c"));bruise.GetComponent<Renderer>().material=m;}
                }else{
                    obj=new GameObject(item.kind).transform;obj.SetParent(workRoot,false);
                    if(item.kind=="coin")for(int k=0;k<item.value;k++){
                        var c=GameObject.CreatePrimitive(PrimitiveType.Cylinder);c.transform.SetParent(obj,false);c.transform.localPosition=new Vector3(0,k*.018f,0);c.transform.localScale=new Vector3(.16f,.008f,.16f);Destroy(c.GetComponent<Collider>());var m=new Material(Shader.Find("Qingming/InkToon"));m.SetColor("_Color",Hex("a28b49"));c.GetComponent<Renderer>().material=m;
                        var hole=GameObject.CreatePrimitive(PrimitiveType.Cube);hole.transform.SetParent(obj,false);hole.transform.localPosition=new Vector3(0,.01f+k*.018f,0);hole.transform.localScale=new Vector3(.044f,.004f,.044f);Destroy(hole.GetComponent<Collider>());var h=new Material(Shader.Find("Qingming/InkToon"));h.SetColor("_Color",Hex("493d29"));hole.GetComponent<Renderer>().material=h;
                    }else {var c=GameObject.CreatePrimitive(item.kind=="cloth"?PrimitiveType.Cube:PrimitiveType.Sphere);c.transform.SetParent(obj,false);c.transform.localScale=item.kind=="cloth"?new Vector3(.55f,.045f,.4f):new Vector3(.28f,.35f,.25f);Destroy(c.GetComponent<Collider>());var m=new Material(Shader.Find("Qingming/InkToon"));m.SetColor("_Color",Hex(item.kind=="cloth"?"c7b790":"76644a"));c.GetComponent<Renderer>().material=m;}
                }
                obj.name="可拿取_"+item.id;
                foreach(var collider in obj.GetComponentsInChildren<Collider>())collider.enabled=false;
                var box=obj.gameObject.AddComponent<BoxCollider>();box.size=item.kind=="basket"?new Vector3(.8f,.7f,.8f):item.kind=="pear"?new Vector3(.42f,.65f,.42f):item.kind=="coin"?new Vector3(.22f,.17f,.22f):new Vector3(.6f,.4f,.45f);box.center=new Vector3(0,.1f,0);
                obj.gameObject.AddComponent<InteractionTarget>().id="hands_item_"+item.id;
                workObjects[item.id]=obj;obj.localPosition=ItemPosition(item);
            }
            var ring=new GameObject("触及轮廓");ring.transform.SetParent(workRoot,false);hoverRing=ring.AddComponent<LineRenderer>();hoverRing.useWorldSpace=true;hoverRing.loop=true;hoverRing.positionCount=32;hoverRing.startWidth=hoverRing.endWidth=.014f;hoverRing.material=new Material(Shader.Find("Sprites/Default"));hoverRing.startColor=hoverRing.endColor=Hex("e4bd68");hoverRing.enabled=false;
            Physics.SyncTransforms();
        }
        GameObject WorkBox(string name,Vector3 position,Vector3 size,Color color,bool solid=false)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(workRoot,false);o.transform.localPosition=position;o.transform.localScale=size;
            var m=new Material(Shader.Find("Qingming/InkToon"));m.SetColor("_Color",color);o.GetComponent<Renderer>().material=m;o.GetComponent<Collider>().enabled=solid;return o;
        }
        void AddWorkZone(string id,Vector3 p,Vector3 size,Color color){var o=WorkBox("交接位置_"+id,p,size,color,true);o.GetComponent<Collider>().isTrigger=true;o.AddComponent<InteractionTarget>().id="hands_zone_"+id;workZones[id]=p;}
        void RouteRing(Vector3 p,Color color){var o=new GameObject("送货路标");o.transform.SetParent(workRoot,false);var l=o.AddComponent<LineRenderer>();l.useWorldSpace=true;l.loop=true;l.positionCount=48;l.startWidth=l.endWidth=.06f;l.material=new Material(Shader.Find("Sprites/Default"));l.startColor=l.endColor=color;for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;l.SetPosition(i,world.transform.TransformPoint(p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.9f));}}
        Vector3 ItemPosition(HandsWork.Item item)
        {
            if(work.mode==HandsWork.Mode.Carry)return workZones[item.zone=="target"?"target":"source"]+Vector3.up*.26f;
            int index=item.zone=="source"?item.id:work.items.FindAll(x=>x.zone==item.zone).IndexOf(item);
            var center=workZones.ContainsKey(item.zone)?workZones[item.zone]:workZones["source"];
            if(item.kind=="basket"||item.kind=="cloth"||item.kind=="purse")return center+Vector3.up*.28f;
            float front=work.mode==HandsWork.Mode.Inspect&&item.zone!="source"?.16f:.63f;
            return center+new Vector3((index%6-2.5f)*.32f,item.kind=="coin"?.08f:.14f,front-(index/6)*.36f);
        }
        void ArrangePlacedItems(){foreach(var item in work.items)if(item.zone!="held")workObjects[item.id].localPosition=ItemPosition(item);Physics.SyncTransforms();}
        void ClearWorkObjects(){if(!workRoot)return;foreach(var c in workRoot.GetComponentsInChildren<Collider>())c.enabled=false;Destroy(workRoot.gameObject);workRoot=null;}
        void CancelWork(bool showDialogue=true)
        {
            foreach(var r in nearDecorations)if(r)r.enabled=true;nearDecorations.Clear();
            if(work==null)return;ClearWorkObjects();if(work.held>=0&&workObjects.TryGetValue(work.held,out var obj))Destroy(obj.gameObject);workObjects.Clear();workZones.Clear();work=null;workDragging=false;world.SetHandsStage(false,false);world.SetStoryScene(state,viewMode);world.Stock(state.stock);interactionOpen=showDialogue;held="";hand.localPosition=new Vector3(.38f,-.37f,.92f);UpdateHeld();
        }
        public bool GrabWorkItem(int id)
        {
            if(work==null||!workObjects.ContainsKey(id))return false;
            var obj=workObjects[id];if(Vector3.Distance(cam.transform.position,obj.position)>4.6f){WorkSay("还够不到，先用 WASD 走近些。",false);return false;}
            if(!work.Grab(id))return false;foreach(var c in obj.GetComponentsInChildren<Collider>())c.enabled=false;
            obj.SetParent(cam.transform,false);obj.localPosition=new Vector3(.18f,-.17f,1.05f);obj.localRotation=Quaternion.identity;workTurn=0;
            if(work.Held.kind=="basket")obj.localPosition=new Vector3(.35f,-.43f,1.3f);
            WorkSay(work.response,true);sound.Tone("water");return true;
        }
        public bool RotateWorkItem(float degrees){if(work==null||!work.Turn(degrees))return false;workTurn+=degrees;workObjects[work.held].localRotation=Quaternion.Euler(12,workTurn,0);WorkSay(work.response,false);return true;}
        public bool PlaceWorkItem(string zone)
        {
            if(work==null||work.Held==null||!workZones.ContainsKey(zone))return false;
            var point=world.transform.TransformPoint(workZones[zone]);if(Vector3.Distance(world.player.position,point)>4.3f){WorkSay("还没走到交接处。提稳东西，靠近些再放。",false);return false;}
            int id=work.held;bool ok=work.Place(zone);if(ok){var obj=workObjects[id];obj.SetParent(workRoot,false);obj.localPosition=ItemPosition(work.items.Find(x=>x.id==id));obj.localRotation=Quaternion.identity;obj.GetComponent<BoxCollider>().enabled=true;workDragging=false;Physics.SyncTransforms();sound.Tone(work.mode==HandsWork.Mode.Money?"coin":"water");}
            if(ok)ArrangePlacedItems();WorkSay(work.response,true,!ok);return ok;
        }
        void ReturnWorkItem(){if(work==null||work.Held==null)return;int id=work.held;work.ReturnHeld();var obj=workObjects[id];obj.SetParent(workRoot,false);obj.localRotation=Quaternion.identity;obj.GetComponent<BoxCollider>().enabled=true;ArrangePlacedItems();workDragging=false;WorkSay(work.response,false);}
        public bool ConfirmWork()
        {
            if(work==null)return false;
            if(!work.Advance()){WorkSay(work.response,true,true);return false;}
            if(!work.done){BuildWorkScene();WorkSay(work.response,true);return true;}
            string id=work.action;CancelWork(false);resolvingWork=true;
            try {DoAction(id);if(id=="walk"){DoAction("arrive");DoAction("next");}}finally{resolvingWork=false;}
            interactionOpen=false;WorkSay(state.receipt.Length>0?state.receipt:"已经亲手完成交接。靠近人物继续交谈。",true);return true;
        }
        void WorkSay(string line,bool gesture,bool reject=false){workReply=line;replyUntil=Time.unscaledTime+8;if(gesture)world.HandsResponse(reject,work!=null&&work.Held!=null?workObjects[work.held].position:world.player.position);}
        string WorkRayTarget(Vector3 screen)
        {
            var hits=Physics.RaycastAll(cam.ScreenPointToRay(screen),5f,~0,QueryTriggerInteraction.Collide);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits){var t=hit.collider.GetComponent<InteractionTarget>();if(t&&t.id.StartsWith("hands_"))return t.id;}
            return "";
        }
        bool UseWorkTarget(string target){if(work==null)return false;if(target.StartsWith("hands_item_"))return GrabWorkItem(int.Parse(target.Substring(11)));if(target.StartsWith("hands_zone_"))return PlaceWorkItem(target.Substring(11));return false;}
        void UpdateWork()
        {
            if(work==null)return;
            interactionOpen=false;viewMode=1;
            workHover=WorkRayTarget(Input.mousePosition);
            if(hoverRing){hoverRing.enabled=workHover.StartsWith("hands_item_")&&work.Held==null;if(hoverRing.enabled){int id=int.Parse(workHover.Substring(11));Vector3 p=workObjects[id].position;float radius=work.items[id].kind=="basket"?.5f:.18f;for(int i=0;i<32;i++){float a=i*Mathf.PI/16;hoverRing.SetPosition(i,p+new Vector3(Mathf.Cos(a)*radius,-.08f,Mathf.Sin(a)*radius));}}}
            bool pointer=new Rect(54,104,1332,552).Contains(CanvasMouse());
            if(pointer&&Input.GetMouseButtonDown(0)){workMouseDown=Input.mousePosition;bool empty=work.Held==null;UseWorkTarget(workHover);workDragging=empty&&work.Held!=null;}
            if(workDragging&&work.Held!=null&&Input.GetMouseButton(0)){
                var obj=workObjects[work.held];Vector3 screen=Input.mousePosition;screen.z=1.15f;obj.position=Vector3.Lerp(obj.position,cam.ScreenToWorldPoint(screen),1-Mathf.Exp(-Time.deltaTime*18));
            }
            if(Input.GetMouseButtonUp(0)&&workDragging){workDragging=false;if(work.Held!=null&&((Vector2)Input.mousePosition-workMouseDown).magnitude>9&&workHover.StartsWith("hands_zone_"))UseWorkTarget(workHover);}
            if(Input.GetKeyDown(KeyCode.Q))ReturnWorkItem();
            if(work.Held!=null&&work.Held.kind=="pear"){
                if(Input.GetKeyDown(KeyCode.R))RotateWorkItem(90);
                float turn=Input.mouseScrollDelta.y*35;if(Input.GetMouseButton(1))turn+=Input.GetAxis("Mouse X")*9;if(Mathf.Abs(turn)>.01f)RotateWorkItem(turn);
            }
            if(Input.GetKeyDown(KeyCode.E)){
                if(work.Held!=null){if(workHover.StartsWith("hands_zone_"))UseWorkTarget(workHover);else if(work.mode==HandsWork.Mode.Carry&&work.waypoint>=3)PlaceWorkItem("target");else WorkSay("手里拿着物件。看向接收盘按 E 放下，Q 可放回。",false);}
                else if(work.Ready)ConfirmWork();else UseWorkTarget(workHover);
            }
            CheckDeliveryProgress();
        }
        void CheckDeliveryProgress(){if(work!=null&&work.mode==HandsWork.Mode.Carry&&work.Held!=null&&work.waypoint<3&&Vector3.Distance(world.player.localPosition,DeliveryStops[work.waypoint])<1.7f){work.waypoint++;WorkSay(work.waypoint==3?"许娘子：到了，篓先放在门前的绿垫上，我来接。":"前面还通着。篓在手里，沿街再走一段。",true);sound.Tone("chapter");}}
        void MoveOnFoot()
        {
            if(viewMode!=1||interactionOpen||state.rodeBoat&&state.chapter==6)return;
            float x=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),z=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
            Vector3 forward=cam.transform.forward;forward.y=0;forward.Normalize();Vector3 right=cam.transform.right;right.y=0;right.Normalize();
            float speed=work!=null&&work.mode==HandsWork.Mode.Carry?2.1f:Input.GetKey(KeyCode.LeftShift)?3.8f:2.5f;
            Vector3 delta=Vector3.ClampMagnitude(forward*z+right*x,1)*speed*Time.deltaTime;
            WalkStep(delta);
            if(delta.sqrMagnitude>.00001f){stepSound+=Time.deltaTime;if(stepSound>.48f){stepSound=0;sound.Tone("water");}if(work!=null&&work.Held!=null&&work.Held.kind=="basket")workObjects[work.held].localRotation=Quaternion.Euler(Mathf.Sin(Time.time*5)*1.5f,0,Mathf.Sin(Time.time*4)*2);}
        }
        void UpdateFirstPersonCamera()
        {
            bool turningPear=work!=null&&work.Held!=null&&work.Held.kind=="pear";
            if(Input.GetMouseButton(1)&&!turningPear){look.x+=Input.GetAxis("Mouse X")*3;look.y=Mathf.Clamp(look.y-Input.GetAxis("Mouse Y")*3,-70,65);}
            SetCameraGoals();cam.transform.position=Vector3.Lerp(cam.transform.position,goalPosition,1-Mathf.Exp(-Time.deltaTime*12));
            var rotation=Quaternion.LookRotation(goalTarget-goalPosition)*Quaternion.Euler(-look.y,look.x,0);cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,rotation,1-Mathf.Exp(-Time.deltaTime*14));cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,zoom,Time.deltaTime*8);
        }
        void DrawExploreHUD()
        {
            PaperFill(new Rect(80,665,1283,128));
            string line=Time.unscaledTime<replyUntil&&workReply.Length>0?workReply:state.Line();
            if(line.Length>98)line=line.Substring(0,98)+"…（F 展开对话）";
            DrawPortrait(PrimaryPortrait(),new Rect(89,589,142,204));
            Label(Speaker(),new Rect(246,677,780,27),18,Red);Label(line,new Rect(246,712,780,70),18,Ink);
            string action=DefaultWorkAction();if(action.Length>0){if(Button("开始动手  E",new Rect(1061,681,277,43),true))DoAction(action);}
            else ActionButton(state.Complete()?"继续这一日  E":"回应人物  F",new Rect(1061,681,277,43),"open",true);
            ActionButton("展开全部回应  F",new Rect(1061,740,277,35),"open",false,false,15);
            Label("WASD 自由走动 · 按住右键环顾 · E 动手 · F 交谈",new Rect(450,843,730,33),16,Ink,true);
        }
        public void WalkStep(Vector3 delta)
        {
            Vector3 p=world.player.localPosition;bool delivery=work!=null&&work.mode==HandsWork.Mode.Carry;
            Vector3 candidate=p+delta;candidate.y=p.y;
            candidate.x=Mathf.Clamp(candidate.x,delivery?-24:sceneAnchor.x-5,delivery?14:sceneAnchor.x+5);
            candidate.z=Mathf.Clamp(candidate.z,delivery?-12:sceneAnchor.z-4.5f,delivery?-3.8f:sceneAnchor.z+4.5f);
            if(!CanStand(candidate)){var slide=new Vector3(candidate.x,p.y,p.z);if(CanStand(slide))candidate=slide;else {slide=new Vector3(p.x,p.y,candidate.z);candidate=CanStand(slide)?slide:p;}}
            world.player.localPosition=candidate;
        }
        bool CanStand(Vector3 p){Vector3 a=world.transform.TransformPoint(p+Vector3.up*.35f),b=world.transform.TransformPoint(p+Vector3.up*1.4f);return !Physics.CheckCapsule(a,b,.23f,~0,QueryTriggerInteraction.Ignore);}
        string DefaultWorkAction(){foreach(var a in state.Options())if(!a.disabled&&HandsWork.Create(a.id,state)!=null)return a.id;return "";}
        void DrawWorkHUD()
        {
            if(work==null)return;
            PaperFill(new Rect(78,119,855,80));Label(work.title,new Rect(99,130,816,30),22,Red);Label(work.instruction,new Rect(100,166,810,26),15,Ink);
            ActionButton("账本  Tab",new Rect(1171,122,194,38),"journal");
            foreach(var zone in workZones){Vector3 sp=cam.WorldToScreenPoint(world.transform.TransformPoint(zone.Value+Vector3.up*.08f));if(sp.z<=0)continue;var pos=new Vector2((sp.x-CanvasOffset.x)/CanvasScale,(Screen.height-sp.y-CanvasOffset.y)/CanvasScale);
                string caption=zone.Key=="source"?(work.mode==HandsWork.Mode.Money?(work.receiving?"对方钱盘":"自己的钱袋"):"原货 · 可放回"):zone.Key=="bad"?"轻伤盘":"接收处";
                if(work.mode==HandsWork.Mode.Money&&zone.Key=="target")caption=(work.receiving?"收入钱袋 ":"递钱盘 ")+work.Sum+" / "+work.needed+" 枚";
                if(work.mode==HandsWork.Mode.Pears&&zone.Key=="target")caption="交出 "+work.Count+" / "+work.needed+" 只";
                if(pos.x>100&&pos.x<1340&&pos.y>230&&pos.y<643){Fill(new Rect(pos.x-100,pos.y+18,200,28),new Color(Paper.r,Paper.g,Paper.b,.9f));Label(caption,new Rect(pos.x-98,pos.y+21,196,24),14,Ink,true);}
            }
            if(work.mode==HandsWork.Mode.Money)foreach(var i in work.items){if(i.zone=="held")continue;Vector3 sp=cam.WorldToScreenPoint(workObjects[i.id].position+Vector3.up*.1f);if(sp.z>0){float x=(sp.x-CanvasOffset.x)/CanvasScale,y=(Screen.height-sp.y-CanvasOffset.y)/CanvasScale;if(x>55&&x<1385&&y>210&&y<645)Label(i.value.ToString(),new Rect(x-12,y-8,24,20),13,Paper,true);}}
            if(work.mode==HandsWork.Mode.Carry&&work.waypoint<3){Vector3 destination=DeliveryStops[work.waypoint];float distance=Vector3.Distance(world.player.localPosition,destination);Label("送货路标 "+(work.waypoint+1)+" / 3   距离 "+distance.ToString("0.0")+" 步",new Rect(940,178,410,30),18,Ink,true);Vector3 sp=cam.WorldToScreenPoint(world.transform.TransformPoint(destination));if(sp.z>0){float x=Mathf.Clamp((sp.x-CanvasOffset.x)/CanvasScale,110,1330),y=Mathf.Clamp((Screen.height-sp.y-CanvasOffset.y)/CanvasScale,230,620);Fill(new Rect(x-38,y-22,76,44),Jade);Label("走向这里",new Rect(x-36,y-12,72,28),15,Paper,true);}else Label("路标在身后 · 按住右键转身",new Rect(950,217,400,35),16,Red,true);}
            PaperFill(new Rect(78,661,1287,133));
            DrawPortrait(PrimaryPortrait(),new Rect(89,589,142,204));
            Label(workReply.Length>0?workReply:work.response.Length>0?work.response:"先走近案前，鼠标点拿或按 E 拿起物件。",new Rect(246,679,735,72),20,Ink);
            if(work.Held!=null){string heldText=work.Held.kind=="pear"?"手中：梨 · "+(work.Held.inspected?(work.Held.bad?"轻伤":"完好"):"尚未翻看"):work.Held.kind=="coin"?"手中："+work.Held.value+" 枚铜钱":"手中："+(work.Held.kind=="basket"?"竹篮":work.Held.kind=="cloth"?"布巾":"钱袋");Label(heldText,new Rect(246,755,735,25),15,Red);}
            if(Button("确认交接  E",new Rect(1030,677,307,42),work.Ready,!work.Ready,17))ConfirmWork();
            if(Button("先放回，重新商量",new Rect(1030,736,307,35),false,false,15))CancelWork();
            Label("WASD 走动 · 右键环顾 · 点击拿取 / 拖到盘中 · R 翻看 · Q 放回",new Rect(86,843,1250,33),17,Ink,true);
        }
    }
}
