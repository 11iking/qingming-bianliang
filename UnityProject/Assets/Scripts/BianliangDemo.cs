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
        bool transitioning,dragging,didDrag,menuMore,sourceInfo,automatedRun;
        Vector2 mouseDown,lastMouse,look;
        Vector3 goalPosition,goalTarget;
        public static readonly string[] SiteNames={"汴河渡口","临河小酒肆","阿安梨摊","虹桥","瓦舍","安家","晨市果行","许家茶坊"};
        static readonly int[] SiteChapters={6,10,3,5,11,0,1,7};
        static readonly Vector3[] SitePoints={G(-21,2.6f,2.5f),G(-14,3.4f,12.8f),G(-3,3.7f,10.8f),G(8,4.4f,0),G(23,4.7f,12),G(-25,2.8f,9),G(-18,2.8f,9),G(10,2.8f,9)};
        static readonly string[] SiteLines={"水陆有别，各有人的工作；问过再托付。","同桌喝酒、喝茶或歇脚，把白日的话接着说完。","果品、钱货、照看与回访，都在这一方摊面上。","船员各守位置，阿安依明确指示在岸上让路。","阿绣演原创《一担梨》，可以接话，也可以静听。","一份家中本钱，一句送货约定；日终回家点交。","翻看篓底，区分好梨与轻伤梨，预留订货。","许娘子按实际成色与数量验收，少的当面说清。"};
        public static Vector3 G(float x,float y,float z)=>BianliangWorld.G(x,y,z);
        void Start()
        {
            Application.targetFrameRate=60;
            automatedRun=Array.Exists(Environment.GetCommandLineArgs(),arg=>arg=="-qingming-self-test"||arg=="-qingming-capture"||arg=="-hands-capture"||arg=="-environment-capture"||arg=="-portrait-capture");
            if(!world)world=Instantiate(Resources.Load<GameObject>("Imported/BianliangWorld")).GetComponent<BianliangWorld>();
            world.Initialize();world.InitializeStory();
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
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hands-capture")>=0)StartCoroutine(CaptureHands());
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-environment-capture")>=0)StartCoroutine(CaptureEnvironment());
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-portrait-capture")>=0)StartCoroutine(CapturePortraits());
            else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-hands-preview")>=0){state=new DayState();state.PrepareChapter(1);StartChapter(1);DoAction("inspect");}
        }
        public int SiteForChapter()=>state.chapter==0||state.chapter==13?5:state.chapter==1?6:state.chapter==7?7:state.chapter==5?3:state.chapter==6?0:state.chapter==10?1:state.chapter==11?4:2;
        float TaskX()=>state.chapter==6&&state.rodeBoat?world.rowBoat.localPosition.x:SitePoints[SiteForChapter()].x;
        public void SetMenu()
        {
            CancelWork(false);
            Time.timeScale=1;
            portraitBeat="";
            playing=false;paused=false;panel="menu";world.gameObject.SetActive(false);
            held="";UpdateHeld();look=Vector2.zero;menuMore=false;interactionOpen=false;
            cam.backgroundColor=Hex("e9e0ca");scrollX=scrollTarget=-3;
        }
        IEnumerator EnterPainting(bool preview=false)
        {
            transitioning=true;menuMore=false;sourceInfo=false;
            state=new DayState{sandbox=preview};StartChapter(0);
            atmosphere.pigment=0;paintingOpacity=1;transitionProgress=0;
            // The painting dissolves into brown ink, then returns to the original 3D colours.
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
            CancelWork(false);
            Time.timeScale=1;state.chapter=Mathf.Clamp(chapter,0,13);world.gameObject.SetActive(true);cam.rect=new Rect(0,0,1,1);
            playing=true;paused=false;panel=state.finished?"summary":"game";held="";
            narrativeOverride="";watchingBridge=false;interactionOpen=false;look=Vector2.zero;dialogueScroll=Vector2.zero;
            world.SetTime(chapter>=10?5:chapter>=9?4:0);atmosphere.night=chapter>=10;atmosphere.pigment=1;
            world.Stock(state.stock);world.crossing=chapter>=5;
            var cargo=world.cargoBoat.localPosition;cargo.x=state.bridge_watched?14:-5;world.cargoBoat.localPosition=cargo;
            world.boatProgress=0;world.rowBoat.localPosition=G(-17,0,2.5f);
            var pos=BianliangWorld.StoryPosition(chapter);
            if(chapter==5)pos=G(6.7f,3.65f,0);
            if(chapter==6)pos=state.rodeBoat?G(-17,.5f,2.5f):G(-20,.22f,6);
            if(chapter==10)pos=G(-14,0,12.8f);
            if(chapter==11)pos=G(22,.1f,15.1f);
            world.player.localPosition=pos;world.player.localRotation=Quaternion.Euler(0,chapter==10||chapter==11?180:0,0);
            sceneAnchor=pos;world.storyAnchor=pos;
            SetView(1);interactionOpen=false;scrollX=scrollTarget=TaskX();scrollVelocity=0;
            SetCameraGoals();cam.transform.SetPositionAndRotation(goalPosition,Quaternion.LookRotation(goalTarget-goalPosition));
            UpdateHeld();Save();sound?.Tone("chapter");
            Toast(DayState.Hours[chapter]+"  /  "+DayState.Chapters[chapter]);
            cam.backgroundColor=Hex(chapter>=10?"36484e":chapter==9?"ccaa8a":"dad7c2");
        }
        public void SetView(int mode)
        {
            if(work!=null&&mode!=1){WorkSay("先放稳手里的东西；需要离开操作可选‘先放回，重新商量’。",false);return;}
            viewMode=Mathf.Clamp(mode,1,3);look=Vector2.zero;zoom=55;interactionOpen=mode==2;
            if(mode==3){selectedSite=SiteForChapter();scrollTarget=TaskX();scrollVelocity=0;}
            world.player.gameObject.SetActive(mode!=1);world.SetStoryScene(state,mode);
            hand.gameObject.SetActive(mode!=3);SetCameraGoals();
        }
        void SetCameraGoals()
        {
            if(state.finished){goalPosition=G(30,23,35);goalTarget=Vector3.zero;return;}
            if(work!=null){var position=world.player.localPosition;bool carrying=work.mode==HandsWork.Mode.Carry;goalPosition=position+new Vector3(0,carrying?1.85f:2.1f,carrying?.8f:1.2f);goalTarget=position+new Vector3(0,carrying?1.6f:.7f,-3);return;}
            if(viewMode==3){goalPosition=G(scrollX+2.8f,17.5f,32);goalTarget=G(scrollX,1.2f,8);return;}
            bool first=viewMode==1;float x=world.rowBoat.localPosition.x;var footOffset=first?world.player.localPosition-sceneAnchor:Vector3.zero;
            if(state.chapter==5){goalPosition=(first?G(6.25f,4.6f,1.4f):G(9.45f,4.6f,.5f))+footOffset;goalTarget=(first?G(8,4.3f,0):G(6.5f,4.1f,0))+footOffset;return;}
            if(state.chapter==6&&state.rodeBoat){goalPosition=first?G(x-.8f,1.65f,2.5f):G(x+1,1.65f,2.5f);goalTarget=first?G(x+7,1,1.8f):G(x-.8f,1.35f,2.5f);return;}
            if(state.chapter==10){goalPosition=(first?G(-14,1.65f,12.8f):G(-14,1.65f,9.65f))+footOffset;goalTarget=(first?G(-14,1.19f,10.5f):G(-14,1.3f,12.8f))+footOffset;return;}
            if(state.chapter==11){goalPosition=(first?G(22,1.85f,13.9f):G(24,1.85f,14.1f))+footOffset;goalTarget=(first?G(23,2.1f,11):G(22,1.5f,15.1f))+footOffset;return;}
            Vector3 p=world.player.localPosition;
            goalPosition=p+new Vector3(first?0:1.2f,1.65f,first?.8f:-3.8f);
            goalTarget=p+new Vector3(0,1.25f,first?-2.7f:0);
        }
        Vector2 CanvasMouse()=>new Vector2((Input.mousePosition.x-CanvasOffset.x)/CanvasScale,(Screen.height-Input.mousePosition.y-CanvasOffset.y)/CanvasScale);
        void Update()
        {
            if(!cam)return;
            if(!Application.isFocused&&!automatedRun)return;
            cam.rect=new Rect(0,0,1,1);
            rowCooldown=Mathf.Max(0,rowCooldown-Time.deltaTime);
            if(Input.GetKeyDown(KeyCode.F11))Screen.fullScreen=!Screen.fullScreen;
            if(Input.GetKeyDown(KeyCode.M))sound.Toggle();
            if(transitioning)return;
            if(!playing){if(panel=="menu"&&!menuMore&&!sourceInfo)painting.UpdateInput(CanvasMouse(),CanvasScale);return;}
            if(Input.GetKeyDown(KeyCode.Escape)){
                if(state.finished)SetMenu();
                else if(paused)Resume();else Pause("pause");
            }
            if(Input.GetKeyDown(KeyCode.Tab)&&!state.finished){if(paused)Resume();else Pause("journal");}
            if(paused||state.finished)return;
            if(work!=null){MoveOnFoot();UpdateWork();UpdateFirstPersonCamera();return;}
            if(Input.GetKeyDown(KeyCode.F))interactionOpen=!interactionOpen;
            if(Input.GetKeyDown(KeyCode.Alpha1))SetView(1);
            if(Input.GetKeyDown(KeyCode.Alpha2))SetView(2);
            if(Input.GetKeyDown(KeyCode.Alpha3))SetView(3);
            if(Input.GetKeyDown(KeyCode.R)){SetView(viewMode);selectedSite=SiteForChapter();}
            if(Input.GetKeyDown(KeyCode.E)&&held!="jug")PrimaryInteraction();
            if(interactionOpen&&!(viewMode==3&&selectedSite!=SiteForChapter())){
                var a=Actions();if(a.Count>0){
                    if(Input.GetKeyDown(KeyCode.DownArrow))selectedChoice=(selectedChoice+1)%a.Count;
                    if(Input.GetKeyDown(KeyCode.UpArrow))selectedChoice=(selectedChoice+a.Count-1)%a.Count;
                    selectedChoice=Mathf.Clamp(selectedChoice,0,a.Count-1);
                    if(Input.GetKeyDown(KeyCode.Return)&&!a[selectedChoice].disabled)DoAction(a[selectedChoice].id);
                }
            }
            if(state.chapter==6&&state.rodeBoat){world.boatProgress=Mathf.Min(1,world.boatProgress+Time.deltaTime*.09f);var p=world.player.localPosition;p.x=world.rowBoat.localPosition.x-.8f;world.player.localPosition=p;}
            MoveOnFoot();
            Vector2 mouse=CanvasMouse();bool inWorld=new Rect(54,104,1332,interactionOpen?326:700).Contains(mouse);
            if(Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1)){
                dragging=inWorld;didDrag=false;mouseDown=lastMouse=mouse;
            }
            if(dragging&&(Input.GetMouseButton(0)||Input.GetMouseButton(1))){
                Vector2 delta=mouse-lastMouse;if((mouse-mouseDown).magnitude>6)didDrag=true;
                if(didDrag){
                    if(viewMode==3){scrollTarget=Mathf.Clamp(scrollTarget-delta.x*.026f,-28,26);scrollVelocity=Mathf.Clamp(-delta.x*.026f/Mathf.Max(.001f,Time.deltaTime),-18,18);interactionOpen=false;}
                    else if(Input.GetMouseButton(1)){look.x+=delta.x*.2f;look.y=Mathf.Clamp(look.y-delta.y*.2f,-65,65);}
                }
                lastMouse=mouse;
            }
            if(Input.GetMouseButtonUp(0)||Input.GetMouseButtonUp(1)){
                if(dragging&&!didDrag&&Input.GetMouseButtonUp(0)){string target=RayTarget();if(target!="")InteractTarget(target);}
                dragging=false;
            }
            float direction=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);
            direction+=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
            if(viewMode==3){
                if(direction!=0){scrollTarget+=direction*Time.deltaTime*15;scrollVelocity=0;}
                if(!dragging){scrollTarget+=scrollVelocity*Time.deltaTime;scrollVelocity=Mathf.MoveTowards(scrollVelocity,0,Time.deltaTime*26);}
                scrollTarget=Mathf.Clamp(scrollTarget,-28,26);scrollX=Mathf.Lerp(scrollX,scrollTarget,1-Mathf.Exp(-Time.deltaTime*8));
            }else if(inWorld)zoom=Mathf.Clamp(zoom-Input.mouseScrollDelta.y*3,35,75);
            SetCameraGoals();
            cam.transform.position=Vector3.Lerp(cam.transform.position,goalPosition,1-Mathf.Exp(-Time.deltaTime*5));
            var rotation=Quaternion.LookRotation(goalTarget-goalPosition)*Quaternion.Euler(-look.y,look.x,0);
            cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,rotation,1-Mathf.Exp(-Time.deltaTime*5));
            cam.fieldOfView=Mathf.Lerp(cam.fieldOfView,zoom,1-Mathf.Exp(-Time.deltaTime*7));
            if(watchingBridge&&world.cargoBoat.localPosition.x>=11){
                state.bridge_watched=true;watchingBridge=false;Save();Toast("船已过桥。船夫拱手谢过，你也记住了这一刻。");
            }
            bool pouring=held=="jug"&&state.chapter==10&&state.pour<1&&(Input.GetKey(KeyCode.E)||(Input.GetMouseButton(0)&&inWorld&&!didDrag));
            if(heldObject&&held=="jug")heldObject.transform.localRotation=Quaternion.Euler(0,0,pouring?-32:0);
            if(wineStream)wineStream.enabled=pouring;
            if(pouring){state.pour=Mathf.Clamp01(state.pour+Time.deltaTime/3);if(state.pour>=1){held="";state.receipt="一盏斟好了。慢慢说话，不以斟满比例评价人情。";UpdateHeld();Save();}}
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
            interactionOpen=true;
            if(id=="story_person"){world.Gesture(world.StoryPerson);return;}
            if(id=="basket"||id=="counter"||id=="story_prop"||id=="jug"||id=="cup")PrimaryInteraction();
        }
        void PrimaryInteraction()
        {
            if(!interactionOpen){string action=DefaultWorkAction();if(action.Length>0){DoAction(action);return;}interactionOpen=true;return;}
            var actions=Actions();
            if(actions.Count==1&&!actions[0].disabled)DoAction(actions[0].id);
            else Toast("请点选明确的回应；有后果的选择不会由 E 自动代选。");
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
            if(id.StartsWith("chapter_")){if(playing)Save();state=new DayState();state.PrepareChapter(int.Parse(id.Substring(8)));StartChapter(state.chapter);return;}
            switch(id){
                case "new":StartCoroutine(EnterPainting());break;
                case "continue":case "restore_main":state=DayState.Load()??new DayState();StartChapter(state.chapter);break;
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
            if(paused||!playing||state.finished)return;
            if(!resolvingWork&&BeginWork(id))return;
            int previous=state.chapter;
            if(!state.Apply(id))return;
            dialogueScroll=Vector2.zero;selectedChoice=0;interactionOpen=true;
            if(id=="next"||state.chapter!=previous||state.finished){StartChapter(state.chapter);return;}
            held=id=="place"||id=="reserve"||id=="pack"?"basket":id=="inspect"?"pear":id=="pour_wine"?"jug":id=="warm"?"cup":"";
            if(id=="bridge_help"||id=="bridge_stand")world.crossing=true;
            if(id=="boat"){world.player.localPosition=G(-17,.5f,2.5f);world.boatProgress=0;}
            if(id=="show_open"||id=="show_end"||id.StartsWith("answer_"))world.Flourish();
            world.Stock(state.stock);world.SetStoryScene(state,viewMode);world.Gesture(world.StoryPerson);world.Gesture(world.player);
            UpdateHeld();Save();sound?.Tone(id.Contains("pay")||id.Contains("buy")?"coin":"chapter");
        }
        IEnumerator ToastCups()
        {
            var cup=world.transform.Find("FriendCup");var initial=cup.localPosition;
            for(float t=0;t<1.6f;t+=Time.deltaTime){cup.localPosition=initial+G(0,Mathf.Sin(t/1.6f*Mathf.PI)*.28f,.35f*Mathf.Sin(t/1.6f*Mathf.PI));yield return null;}
            cup.localPosition=initial;
        }
        public void Row(string side){Toast("这一程由熟手操舟。阿安坐稳看岸景，无需划满固定桨数。");}
        void Pause(string screen){paused=true;panel=screen;Time.timeScale=0;if(playing)Save();}
        void Resume(){paused=false;Time.timeScale=1;panel=playing?(state.finished?"summary":"game"):"menu";}
        void Save(){try{state.Save();}catch(Exception e){Debug.LogWarning(e.Message);Toast("保存失败，请检查可用空间。");}}
        void Toast(string text){toast=text;toastUntil=Time.unscaledTime+3.4f;}
        void OnApplicationQuit(){if(playing)Save();}
        void OnDestroy(){Time.timeScale=1;painting?.Dispose();}
        static Color Hex(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
    }
}
