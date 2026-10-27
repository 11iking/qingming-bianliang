using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qingming
{
    public partial class BianliangDemo : MonoBehaviour
    {
        public BianliangWorld world;
        public DayState state=new DayState();
        Camera cam; Transform hand; GameObject heldObject; LineRenderer wineStream;
        DemoSound sound;
        ScrollAtmosphere atmosphere;
        float transitionProgress,paintingOpacity;
        public int viewMode=3,selectedSite=2;
        public bool playing,paused,interactionOpen,watchingBridge;
        string held="",narrativeOverride="",panel="menu",toast="";
        float toastUntil,rowCooldown,zoom=55,scrollX=-3,scrollTarget=-3,scrollVelocity;
        bool transitioning,dragging,didDrag,menuMore,sourceInfo;
        Vector2 mouseDown,lastMouse,look;
        Vector3 goalPosition,goalTarget;
        public static readonly string[] SiteNames={"汴河渡口","孙羊正店","阿安梨摊","虹桥","勾栏听曲"};
        static readonly int[] SiteChapters={3,5,0,2,6};
        static readonly Vector3[] SitePoints={G(-21,2.6f,2.5f),G(-14,3.4f,12.8f),G(-3,3.7f,10.8f),G(8,4.4f,0),G(23,4.7f,12)};
        static readonly string[] SiteLines={
            "船轻轻摇，岸慢慢退。借船夫的一支桨，从水上看两岸人家。",
            "灯笼亮时，老周已经温好了酒。斟上一盏，聊聊今日的生意。",
            "晨起摆梨，日间议价，薄暮收摊。借卖梨人的眼睛，过一日寻常生活。",
            "桥上的人倚着栏杆，船上的人抬起头。帮船夫看一眼水路，等大船平安过桥。",
            "市声渐远，鼓板初响。坐进灯火里，听一折《一担春光》。"};
        public static Vector3 G(float x,float y,float z)=>BianliangWorld.G(x,y,z);
        void Start()
        {
            Application.targetFrameRate=60;
            if(!world)world=Instantiate(Resources.Load<GameObject>("Imported/BianliangWorld")).GetComponent<BianliangWorld>();
            world.Initialize();
            cam=new GameObject("Demo Camera").AddComponent<Camera>();cam.tag="MainCamera";
            cam.nearClipPlane=.06f;cam.farClipPlane=180;cam.fieldOfView=55;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Hex("d9d6c2");
            cam.gameObject.AddComponent<AudioListener>();
            atmosphere=cam.gameObject.AddComponent<ScrollAtmosphere>();
            hand=new GameObject("Held Object").transform;hand.SetParent(cam.transform,false);
            hand.localPosition=new Vector3(.38f,-.37f,.92f);hand.localScale=Vector3.one*.65f;
            sound=gameObject.AddComponent<DemoSound>();
            painting=new PaintingViewer();painting.Load();
            SetMenu();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-qingming-self-test")>=0)StartCoroutine(SelfTest());
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-qingming-capture")>=0)StartCoroutine(CaptureViews());
        }
        public int SiteForChapter()=>state.chapter==2?3:state.chapter==3?0:state.chapter==5?1:state.chapter==6?4:2;
        float TaskX()=>state.chapter==3?world.rowBoat.localPosition.x:SitePoints[SiteForChapter()].x;
        public void SetMenu()
        {
            Time.timeScale=1;
            playing=false;paused=false;panel="menu";world.gameObject.SetActive(false);
            held="";UpdateHeld();look=Vector2.zero;menuMore=false;interactionOpen=false;
            cam.backgroundColor=Hex("e9e0ca");scrollX=scrollTarget=-3;
        }
        IEnumerator EnterPainting(bool preview=false)
        {
            transitioning=true;menuMore=false;sourceInfo=false;
            state=new DayState{sandbox=preview};StartChapter(0);
            atmosphere.pigment=0;paintingOpacity=1;transitionProgress=0;
            // The painting dissolves into brown ink before restrained pigment returns.
            // Unscaled time keeps the transition independent of pause or frame rate.
            for(float t=0;t<4.2f;t+=Time.unscaledDeltaTime){
                transitionProgress=Mathf.Clamp01(t/4.2f);
                paintingOpacity=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.68f,transitionProgress));
                atmosphere.pigment=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,1,transitionProgress));
                cam.fieldOfView=Mathf.Lerp(52,55,Mathf.SmoothStep(0,1,transitionProgress));
                yield return null;
            }
            atmosphere.pigment=1;paintingOpacity=0;cam.fieldOfView=55;transitioning=false;
            Toast(DayState.Hours[0]+"  /  "+DayState.Chapters[0]);
        }
        public void StartChapter(int chapter)
        {
            Time.timeScale=1;
            state.chapter=Mathf.Clamp(chapter,0,7);world.gameObject.SetActive(true);
            playing=true;paused=false;panel=chapter==7?"summary":"game";held="";
            narrativeOverride="";watchingBridge=false;interactionOpen=false;look=Vector2.zero;
            world.SetTime(state.chapter);world.SetTargets(state.chapter);
            atmosphere.night=state.chapter>=5;atmosphere.pigment=1;
            world.Stock(state.chapter>0?state.stock:state.placed*4);
            world.crossing=state.bridge_helped;
            var cargo=world.cargoBoat.localPosition;cargo.x=state.bridge_watched?14:-5;world.cargoBoat.localPosition=cargo;
            world.boatProgress=state.strokes/10f;
            world.rowBoat.localPosition=G(-17+world.boatProgress*19,0,2.5f);
            var positions=new[]{G(-3,.22f,9.25f),G(-3,.22f,9.25f),G(6.7f,3.65f,0),G(-17,.5f,2.5f),G(-3,.22f,9.25f),G(-14,0,12.8f),G(22,.1f,15.1f),G(-3,.22f,9.25f)};
            world.player.localPosition=positions[state.chapter];
            world.player.localRotation=Quaternion.Euler(0,state.chapter>=5?180:0,0);
            world.transform.Find("FriendCup").localPosition=G(-14,1.13f,10.7f);
            if(chapter==1&&state.customer_ready)held="pear";
            SetView(3);interactionOpen=false;scrollX=scrollTarget=TaskX();scrollVelocity=0;
            if(chapter==7){state.finished=true;goalPosition=G(30,23,35);goalTarget=Vector3.zero;}
            SetCameraGoals();
            cam.transform.SetPositionAndRotation(goalPosition,Quaternion.LookRotation(goalTarget-goalPosition));
            UpdateHeld();Save();sound?.Tone("chapter");
            Toast(DayState.Hours[state.chapter]+"  /  "+DayState.Chapters[state.chapter]);
            cam.backgroundColor=Hex(state.chapter>=5?"36484e":state.chapter==4?"ccaa8a":"dad7c2");
        }
        public void SetView(int mode)
        {
            viewMode=Mathf.Clamp(mode,1,3);look=Vector2.zero;zoom=55;interactionOpen=mode!=3;
            if(mode==3){selectedSite=SiteForChapter();scrollTarget=TaskX();scrollVelocity=0;}
            world.player.gameObject.SetActive(mode!=1);
            world.customer.gameObject.SetActive(state.chapter==1&&state.customer_index<3&&mode!=2);
            world.friend.gameObject.SetActive(!(state.chapter==5&&mode==2));
            hand.gameObject.SetActive(mode!=3);
            SetCameraGoals();
        }
        void SetCameraGoals()
        {
            if(state.chapter==7){goalPosition=G(30,23,35);goalTarget=Vector3.zero;return;}
            if(viewMode==3){goalPosition=G(scrollX+2.8f,17.5f,32);goalTarget=G(scrollX,1.2f,5.8f);return;}
            bool first=viewMode==1;float x=world.rowBoat.localPosition.x;
            switch(state.chapter){
                case 2:goalPosition=first?G(6.25f,4.35f,.2f):G(9.45f,4.35f,.5f);goalTarget=first?G(-1,.7f,0):G(6.5f,3.8f,0);break;
                case 3:goalPosition=first?G(x-.8f,1.65f,2.5f):G(x+1,1.65f,2.5f);goalTarget=first?G(x+7,1,1.8f):G(x-.8f,1.35f,2.5f);break;
                case 5:goalPosition=first?G(-14,1.65f,12.8f):G(-14,1.65f,9.65f);goalTarget=first?G(-14,1.19f,10.5f):G(-14,1.3f,12.8f);break;
                case 6:goalPosition=first?G(22,1.85f,13.9f):G(24,1.85f,14.1f);goalTarget=first?G(23,2.1f,11):G(22,1.5f,15.1f);break;
                default:goalPosition=first?G(-3,1.79f,9.3f):G(-3,1.73f,12.7f);goalTarget=first?G(-3,1.03f,12):G(-3,1.3f,9.3f);break;
            }
        }
        Vector2 CanvasMouse()=>new Vector2((Input.mousePosition.x-CanvasOffset.x)/CanvasScale,(Screen.height-Input.mousePosition.y-CanvasOffset.y)/CanvasScale);
        void Update()
        {
            if(!cam)return;
            rowCooldown=Mathf.Max(0,rowCooldown-Time.deltaTime);
            if(Input.GetKeyDown(KeyCode.F11))Screen.fullScreen=!Screen.fullScreen;
            if(Input.GetKeyDown(KeyCode.M))sound.Toggle();
            if(transitioning)return;
            if(!playing){if(panel=="menu"&&!menuMore&&!sourceInfo)painting.UpdateInput(CanvasMouse(),CanvasScale);return;}
            if(Input.GetKeyDown(KeyCode.Escape)){
                if(state.chapter==7)SetMenu();
                else if(paused)Resume();else Pause("pause");
            }
            if(Input.GetKeyDown(KeyCode.Tab)&&state.chapter<7){if(paused)Resume();else Pause("journal");}
            if(paused||state.chapter==7)return;
            if(Input.GetKeyDown(KeyCode.Alpha1))SetView(1);
            if(Input.GetKeyDown(KeyCode.Alpha2))SetView(2);
            if(Input.GetKeyDown(KeyCode.Alpha3))SetView(3);
            if(Input.GetKeyDown(KeyCode.R)){SetView(viewMode);selectedSite=SiteForChapter();}
            if(Input.GetKeyDown(KeyCode.E)&&held!="jug")PrimaryInteraction();
            if(state.chapter==3){
                if(Input.GetKeyDown(KeyCode.A))Row("left");
                if(Input.GetKeyDown(KeyCode.D))Row("right");
                var p=world.player.localPosition;p.x=world.rowBoat.localPosition.x-.8f;world.player.localPosition=p;
                world.player.localRotation=Quaternion.Euler(0,-90,0);
            }
            Vector2 mouse=CanvasMouse();bool inWorld=new Rect(54,104,1332,interactionOpen?499:700).Contains(mouse);
            if(Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1)){
                dragging=inWorld;didDrag=false;mouseDown=lastMouse=mouse;
            }
            if(dragging&&(Input.GetMouseButton(0)||Input.GetMouseButton(1))){
                Vector2 delta=mouse-lastMouse;if((mouse-mouseDown).magnitude>6)didDrag=true;
                if(didDrag){
                    if(viewMode==3){scrollTarget=Mathf.Clamp(scrollTarget-delta.x*.026f,-23,23);scrollVelocity=Mathf.Clamp(-delta.x*.026f/Mathf.Max(.001f,Time.deltaTime),-18,18);interactionOpen=false;}
                    else if(Input.GetMouseButton(1)){look.x=Mathf.Clamp(look.x+delta.x*.2f,-86,86);look.y=Mathf.Clamp(look.y-delta.y*.2f,-43,37);}
                }
                lastMouse=mouse;
            }
            if(Input.GetMouseButtonUp(0)||Input.GetMouseButtonUp(1)){
                if(dragging&&!didDrag&&Input.GetMouseButtonUp(0)){string target=RayTarget();if(target!="")InteractTarget(target);}
                dragging=false;
            }
            float direction=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);
            if(state.chapter!=3)direction+=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
            if(viewMode==3){
                if(direction!=0){scrollTarget+=direction*Time.deltaTime*15;scrollVelocity=0;}
                if(!dragging){scrollTarget+=scrollVelocity*Time.deltaTime;scrollVelocity=Mathf.MoveTowards(scrollVelocity,0,Time.deltaTime*26);}
                scrollTarget=Mathf.Clamp(scrollTarget,-23,23);scrollX=Mathf.Lerp(scrollX,scrollTarget,1-Mathf.Exp(-Time.deltaTime*8));
            }else if(inWorld)zoom=Mathf.Clamp(zoom-Input.mouseScrollDelta.y*3,35,75);
            SetCameraGoals();
            cam.transform.position=Vector3.Lerp(cam.transform.position,goalPosition,1-Mathf.Exp(-Time.deltaTime*5));
            var rotation=Quaternion.LookRotation(goalTarget-goalPosition)*Quaternion.Euler(-look.y,look.x,0);
            cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,rotation,1-Mathf.Exp(-Time.deltaTime*5));
            cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,zoom,1-Mathf.Exp(-Time.deltaTime*7));
            if(watchingBridge&&world.cargoBoat.localPosition.x>=11){
                state.bridge_watched=true;watchingBridge=false;Save();Toast("船已过桥。船夫拱手谢过，你也记住了这一刻。");
            }
            bool pouring=held=="jug"&&state.pour<1&&(Input.GetKey(KeyCode.E)||(Input.GetMouseButton(0)&&inWorld&&!didDrag));
            if(heldObject&&held=="jug")heldObject.transform.localRotation=Quaternion.Euler(0,0,pouring?-32:0);
            if(wineStream)wineStream.enabled=pouring;
            if(pouring){
                state.pour=Mathf.Clamp01(state.pour+Time.deltaTime/3);
                if(state.pour>=1){state.Memory("亲手斟了一盏酒");held="";UpdateHeld();Save();Toast("酒已斟好，拿起酒盏，与老周碰杯。");}
            }
        }
        public void BrowseSite(int site)
        {
            if(selectedSite==site&&site==SiteForChapter()){SetView(1);return;}
            SetView(3);selectedSite=site;scrollTarget=SitePoints[site].x;interactionOpen=true;
        }
        string RayTarget()
        {
            return Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition),out var hit,80)
                ?hit.collider.GetComponent<InteractionTarget>()?.id??"":"";
        }
        public void InteractTarget(string id)
        {
            if(state.chapter==0){if(id=="basket"&&held=="")DoAction("pick_basket");else if(id=="counter"&&held=="basket")DoAction("place");}
            else if(state.chapter==1&&(id=="counter"||id=="customer")&&!state.customer_ready)DoAction("show_pear");
            else if(state.chapter==4&&(id=="basket"||id=="counter"))DoAction(state.packed?"count":"pack");
            else if(state.chapter==5){if(id=="jug"&&state.paid_wine)DoAction("pick_jug");else if(id=="cup"&&state.pour>=1)DoAction("toast");}
        }
        void PrimaryInteraction()
        {
            if(viewMode==3&&selectedSite!=SiteForChapter()){SetView(3);return;}
            if(state.chapter==1&&state.customer_ready){interactionOpen=true;return;}
            var actions=Actions();if(actions.Count>0&&!actions[0].disabled)DoAction(actions[0].id);
        }
        void UpdateHeld()
        {
            world.transform.Find("WineJug").gameObject.SetActive(held!="jug");
            world.transform.Find("PlayerCup").gameObject.SetActive(held!="cup");
            if(heldObject)Destroy(heldObject);wineStream=null;
            if(held=="")return;
            heldObject=Instantiate(world.templates.Find(held).gameObject,hand,false);heldObject.SetActive(true);
            heldObject.transform.localPosition=Vector3.zero;heldObject.transform.localRotation=Quaternion.identity;
            if(held=="jug"){
                wineStream=heldObject.AddComponent<LineRenderer>();wineStream.useWorldSpace=false;wineStream.positionCount=2;
                wineStream.SetPositions(new[]{G(.34f,.43f,0),G(.54f,-.5f,0)});wineStream.startWidth=wineStream.endWidth=.018f;
                wineStream.material=new Material(Shader.Find("Sprites/Default"));wineStream.startColor=wineStream.endColor=Hex("d3c99e");wineStream.enabled=false;
            }
        }
        public void Action(string id)
        {
            if(transitioning)return;sound?.Tone();
            if(id.StartsWith("view_")){SetView(int.Parse(id.Substring(5)));return;}
            if(id.StartsWith("chapter_")){state=new DayState();state.PrepareChapter(int.Parse(id.Substring(8)));StartChapter(state.chapter);return;}
            switch(id){
                case "new":StartCoroutine(EnterPainting());break;
                case "continue":state=DayState.Load()??new DayState();StartChapter(state.chapter);break;
                case "menu":if(playing)Save();SetMenu();break;
                case "chapters":Pause("chapters");menuMore=false;break;
                case "pause":Pause("pause");break;
                case "journal":Pause("journal");break;
                case "resume":Resume();break;
                case "sound":sound.Toggle();break;
                case "quit":Save();Application.Quit();break;
                case "open":interactionOpen=true;break;
                case "close":interactionOpen=false;break;
                case "home":SetView(3);break;
                default:DoAction(id);break;
            }
        }
        public void DoAction(string id)
        {
            if(paused||!playing)return;
            interactionOpen=true;narrativeOverride="";
            switch(id){
                case "pick_basket":if(state.placed>=3)return;held="basket";SetView(1);Toast("已拿起竹篮。点击摊面，或按 E 放下。");break;
                case "place":if(held!="basket"||state.placed>=3)return;held="";state.placed++;world.Stock(state.placed*4);world.Goods();if(state.placed==3)state.Memory("晨雾里支起梨摊");break;
                case "show_pear":if(state.customer_index>=3)return;state.customer_ready=true;held="pear";SetView(1);break;
                case "fair":case "deal":case "high":
                    narrativeOverride=state.Trade(id,out bool ok);if(ok){held="";world.Stock(state.stock);sound?.Tone("coin");if(state.customer_index==3){state.Memory("卖出了十二只鲜梨");world.customer.gameObject.SetActive(false);}Toast(narrativeOverride);}
                    world.Gesture(world.customer);break;
                case "bridge_call":if(!state.bridge_helped){state.bridge_helped=true;state.reputation++;world.crossing=true;state.Memory("虹桥上帮船夫看水路");}break;
                case "bridge_watch":if(!state.bridge_helped)return;watchingBridge=true;SetView(3);Toast("货船正缓缓过桥。左右拖动画卷，跟着它看一会儿。");break;
                case "row_left":Row("left");return;
                case "row_right":Row("right");return;
                case "pack":state.packed=true;world.Stock(0);held="basket";break;
                case "count":state.counted=true;held="";state.Memory("薄暮清账，收摊歇脚");sound?.Tone("coin");break;
                case "buy_wine":if(!state.paid_wine&&state.Pay(6))state.paid_wine=true;SetView(1);break;
                case "pick_jug":if(!state.paid_wine||state.pour>=1)return;held="jug";SetView(1);break;
                case "pour_hint":Toast("按住 E，或在画面中按住鼠标左键约三秒。");break;
                case "toast":if(state.pour<1||state.toasted)return;state.toasted=true;state.reputation++;held="cup";SetView(1);state.Memory("与老友碰杯话家常");sound?.Tone("toast");StartCoroutine(ToastCups());break;
                case "talk":SetView(2);narrativeOverride="老周望着你笑：“你看这河上的船，来了又去。钱挣多挣少，日子总得过得热乎。”";break;
                case "buy_show":if(!state.theatre_paid&&state.Pay(4))state.theatre_paid=true;SetView(1);break;
                case "listen":if(state.theatre_paid&&state.verses<3){state.verses++;world.Flourish();sound?.Tone("chapter");if(state.verses==3)state.Memory("灯下听完一折市井故事");}break;
                case "next":if(state.Complete())StartChapter(state.chapter+1);return;
                case "to_close":StartChapter(4);return;
                case "to_theatre":StartChapter(6);return;
                case "finish":StartChapter(7);return;
                default:return;
            }
            if(id=="pick_basket"||id=="show_pear"||id=="pack"||id=="pick_jug"||id=="toast"||id=="bridge_call")world.Gesture(world.player);
            UpdateHeld();Save();
        }
        IEnumerator ToastCups()
        {
            var cup=world.transform.Find("FriendCup");var initial=cup.localPosition;
            for(float t=0;t<1.6f;t+=Time.deltaTime){cup.localPosition=initial+G(0,Mathf.Sin(t/1.6f*Mathf.PI)*.28f,.35f*Mathf.Sin(t/1.6f*Mathf.PI));yield return null;}
            cup.localPosition=initial;
        }
        public void Row(string side)
        {
            if(state.strokes>=10||rowCooldown>0)return;
            if(state.last_side==side){Toast("换到另一侧划桨，船才走得稳。");return;}
            rowCooldown=.38f;state.last_side=side;state.strokes++;world.boatProgress=state.strokes/10f;world.Stroke(side);
            if(viewMode==3)scrollTarget=Mathf.Clamp(-17+world.boatProgress*19,-23,23);
            if(state.strokes>=10){state.Memory("摇桨行过一段汴河");Toast("稳稳靠岸。两岸的叫卖声，又近了。");}
            sound?.Tone("water");Save();
        }
        void Pause(string screen){paused=true;panel=screen;Time.timeScale=0;if(playing)Save();}
        void Resume(){paused=false;Time.timeScale=1;panel=playing?(state.chapter==7?"summary":"game"):"menu";}
        void Save(){try{state.Save();}catch(Exception e){Debug.LogWarning(e.Message);Toast("保存失败，请检查可用空间。");}}
        void Toast(string text){toast=text;toastUntil=Time.unscaledTime+3.4f;}
        void OnApplicationQuit(){if(playing)Save();}
        void OnDestroy(){Time.timeScale=1;painting?.Dispose();}
        static Color Hex(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
    }
}
