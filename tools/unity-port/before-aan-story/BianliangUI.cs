using System;
using UnityEngine;
namespace Qingming
{
    public partial class BianliangDemo
    {
        PaintingViewer painting;
        Font serif,sans;
        Texture2D paperTexture;
        GUIStyle textStyle,buttonStyle;
        float CanvasScale=>Mathf.Min(Screen.width/1440f,Screen.height/900f);
        Vector2 CanvasOffset=>new Vector2((Screen.width-1440*CanvasScale)/2,(Screen.height-900*CanvasScale)/2);
        static readonly Color Paper=Hex("e9e0ca"),Ink=Hex("494536"),Red=Hex("99543f"),Muted=Hex("80745c"),Jade=Hex("597365");
        void InitUI()
        {
            if(textStyle!=null)return;
            serif=Resources.Load<Font>("Fonts/NotoSerifSC");sans=Resources.Load<Font>("Fonts/NotoSansSC");
            textStyle=new GUIStyle{font=serif,wordWrap=true,richText=false,alignment=TextAnchor.UpperLeft};
            buttonStyle=new GUIStyle{clipping=TextClipping.Clip,font=serif,alignment=TextAnchor.MiddleCenter,fontSize=17};
            paperTexture=new Texture2D(128,128,TextureFormat.RGBA32,false);var pixels=new Color[128*128];
            var rng=new System.Random(1085);
            for(int i=0;i<pixels.Length;i++){float v=.97f+(float)rng.NextDouble()*.05f;pixels[i]=new Color(Paper.r*v,Paper.g*v,Paper.b*v,1);}
            paperTexture.SetPixels(pixels);paperTexture.Apply();paperTexture.wrapMode=TextureWrapMode.Repeat;
        }
        void OnGUI()
        {
            if(painting==null)return;
            InitUI();var oldMatrix=GUI.matrix;
            GUI.matrix=Matrix4x4.TRS(new Vector3(CanvasOffset.x,CanvasOffset.y,0),Quaternion.identity,Vector3.one*CanvasScale);
            if(!playing)Fill(new Rect(0,0,1440,900),Paper);
            if(panel=="menu"){painting.Draw();Frame();DrawMenu();}
            else if(panel=="game"){
                if(transitioning){
                    painting.Draw(paintingOpacity,false);Frame();
                    Label("清明上河图",new Rect(63,24,340,55),32,Ink);
                    Label("绢色渐开 · 墨中生景",new Rect(365,40,450,30),16,Muted);
                    Label(transitionProgress<.55f?"循着墨色，走近汴梁。":"淡彩渐起，市井声来。",new Rect(320,838,800,35),19,Ink,true);
                    Fill(new Rect(590,882,260,2),new Color(Ink.r,Ink.g,Ink.b,.13f));
                    Fill(new Rect(590,882,260*transitionProgress,2),Muted);
                }else{Frame();DrawGame();}
            }
            else if(panel=="chapters")DrawChapters();
            else if(panel=="pause")DrawPause();
            else if(panel=="journal")DrawJournal();
            else if(panel=="summary")DrawSummary();
            if(Time.unscaledTime<toastUntil&&playing&&!paused&&!transitioning){Fill(new Rect(300,111,840,42),new Color(.25f,.25f,.2f,.8f));Label(toast,new Rect(315,120,810,30),16,Paper,true);}
            GUI.matrix=oldMatrix;
        }
        void Fill(Rect rect,Color color){var previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;}
        void PaperFill(Rect rect){GUI.DrawTextureWithTexCoords(rect,paperTexture,new Rect(0,0,rect.width/128,rect.height/128));}
        void Label(string text,Rect rect,int size,Color color,bool center=false)
        {
            textStyle.fontSize=size;textStyle.normal.textColor=color;textStyle.alignment=center?TextAnchor.UpperCenter:TextAnchor.UpperLeft;
            GUI.Label(rect,text,textStyle);
        }
        bool Button(string caption,Rect rect,bool primary=false,bool disabled=false,int size=17)
        {
            bool hover=rect.Contains(Event.current.mousePosition);
            if(primary)Fill(rect,hover?Hex("496758"):Jade);
            else if(hover&&!disabled)Fill(rect,new Color(1,1,1,.28f));
            Fill(new Rect(rect.x,rect.yMax-1,rect.width,1),primary?Jade:Hex("b7a98b"));
            buttonStyle.fontSize=size;buttonStyle.normal.textColor=disabled?Hex("aba18a"):primary?Paper:Ink;
            buttonStyle.hover.textColor=buttonStyle.normal.textColor;buttonStyle.active.textColor=Red;
            bool wasEnabled=GUI.enabled;GUI.enabled=!disabled&&!transitioning;
            bool clicked=GUI.Button(rect,caption,buttonStyle);GUI.enabled=wasEnabled;return clicked;
        }
        void ActionButton(string caption,Rect rect,string action,bool primary=false,bool disabled=false,int size=17){if(Button(caption,rect,primary,disabled,size))Action(action);}
        void Frame()
        {
            PaperFill(new Rect(0,0,1440,104));PaperFill(new Rect(0,804,1440,96));
            PaperFill(new Rect(0,104,54,700));PaperFill(new Rect(1386,104,54,700));
            Fill(new Rect(52,102,1336,2),Hex("a39170"));Fill(new Rect(52,804,1336,2),Hex("a39170"));
            for(int side=0;side<2;side++){
                int x=side==0?31:1395;Fill(new Rect(x,94,14,720),Hex("665138"));
                Fill(new Rect(x+3,94,6,720),Hex("ad8b5f"));Fill(new Rect(x-2,86,18,10),Hex("493b2b"));Fill(new Rect(x-2,812,18,10),Hex("493b2b"));
            }
        }
        void DrawMenu()
        {
            Label("清明上河图",new Rect(62,20,310,53),34,Ink);
            Seal("观画",new Rect(269,33,29,29));
            Label("北宋 · 张择端   /   故宫博物院藏",new Rect(324,37,660,26),14,Muted);
            if(Button("卷外小记",new Rect(1243,30,132,38),false,false,15))menuMore=!menuMore;
            Label("拖动展读 · 滚轮细看",new Rect(62,844,246,27),14,Muted);
            ActionButton("入画  →",new Rect(1193,830,184,55),"new",true,false,26);
            if(menuMore){
                Fill(new Rect(0,93,1440,711),new Color(.2f,.18f,.13f,.20f));PaperFill(new Rect(960,93,422,425));
                Label("卷外小记",new Rect(989,113,360,45),27,Ink);
                string[] names={"全卷展读","回到虹桥","原始像素 1 : 1","原作与图源","继续上次","重游某一刻"};
                for(int i=0;i<6;i++)if(Button(names[i],new Rect(988,171+i*44,357,37),false,i==4&&!System.IO.File.Exists(DayState.SavePath))){
                    menuMore=false;
                    switch(i){case 0:painting.Full();break;case 1:painting.Bridge();break;case 2:painting.ZoomAt(1,PaintingViewer.Detail.center);break;case 3:sourceInfo=true;break;case 4:Action("continue");break;case 5:Action("chapters");break;}
                }
                if(Button("合上",new Rect(1241,460,104,36),true))menuMore=false;
            }
            if(sourceInfo){
                Fill(new Rect(0,0,1440,900),new Color(.15f,.17f,.14f,.65f));PaperFill(new Rect(320,235,800,416));
                Label("原作与图源",new Rect(354,260,720,53),32,Ink);
                Label("张择端《清明上河图》 · 故宫博物院藏。\n本程序沿用原 Demo 内置的公共领域数字复制图，\n尺寸 38,414 × 1,800 像素，完整画心从卷首至卷末保留。\n高清细节直接使用原图像素；未进行 AI 补画、扩图或重绘。",new Rect(358,337,724,151),18,Ink);
                Label("commons.wikimedia.org/wiki/File:Alongtheriver_QingMing.jpg",new Rect(358,508,736,28),14,Muted);
                if(Button("合上，继续观画",new Rect(818,571,264,47),true))sourceInfo=false;
            }
        }
        void Seal(string caption,Rect r){Fill(r,Red);Label(caption,r,Mathf.RoundToInt(r.height*.46f),Paper,true);}
        void DrawGame()
        {
            Label("汴梁一日",new Rect(63,22,195,46),28,Ink);Seal("入画",new Rect(226,30,28,28));
            Label(DayState.Hours[state.chapter].Split('·')[0]+" / "+DayState.Chapters[state.chapter],new Rect(279,33,660,27),16,Muted);
            ActionButton("卷录  ·  Esc",new Rect(1235,27,144,39),"pause",false,false,14);
            bool preview=viewMode==3&&selectedSite!=SiteForChapter();
            if(viewMode==3){
                for(int i=0;i<5;i++){
                    Vector3 p=cam.WorldToScreenPoint(SitePoints[i]);if(p.z<=0)continue;
                    Vector2 pos=new Vector2((p.x-CanvasOffset.x)/CanvasScale,(Screen.height-p.y-CanvasOffset.y)/CanvasScale);
                    if(pos.x<85||pos.x>1355||pos.y<135||pos.y>760)continue;
                    Rect r=new Rect(pos.x-18,pos.y-18,36,36);Fill(r,i==SiteForChapter()?Red:Hex("7b795f"));
                    if(Button("印",r,false,false,18)&&!didDrag)BrowseSite(i);
                    if(r.Contains(Event.current.mousePosition)||i==selectedSite){Fill(new Rect(pos.x-85,pos.y+23,170,30),new Color(Paper.r,Paper.g,Paper.b,.91f));Label(SiteNames[i],new Rect(pos.x-83,pos.y+26,166,25),16,Ink,true);}
                }
                ActionButton((preview?"此处  ·  "+SiteNames[selectedSite]:"此刻  ·  "+DayState.Chapters[state.chapter])+"  ↗",new Rect(59,840,312,40),"open",false,false,14);
                Label("向左右展读，循一枚印记走入人间",new Rect(410,847,620,27),16,Muted,true);
                ActionButton("回到阿安  ↗",new Rect(1221,840,170,40),"home",false,false,14);
            }else{
                ActionButton("←  退回画卷",new Rect(56,840,170,40),"view_3",false,false,15);
                ActionButton("人物对话  ↗",new Rect(1221,840,170,40),"open",false,false,14);
                Label("右键环顾  ·  点击物件或 E 交互",new Rect(415,847,610,27),14,Muted,true);
                Label("·",new Rect(711,440,18,24),20,Paper);
            }
            if(!interactionOpen)return;
            Fill(new Rect(54,603,1332,201),new Color(Paper.r,Paper.g,Paper.b,.95f));
            Label(preview?SiteNames[selectedSite]:Speaker(),new Rect(138,642,600,27),18,Red);
            if(!preview){
                Label($"钱袋 {state.coins} 文  /  鲜梨 {state.stock}",new Rect(744,646,254,23),12,Muted);
                string[] names={"亲历","对坐","观卷"};for(int i=0;i<3;i++)ActionButton(names[i],new Rect(1018+i*65,639,58,32),"view_"+(i+1),viewMode==i+1,false,13);
            }
            ActionButton("收起",new Rect(1220,638,76,34),"close",false,false,13);
            Label(preview?SiteLines[selectedSite]:Narrative(),new Rect(138,680,1160,62),18,Ink);
            if(preview){ActionButton("走入这一刻  →",new Rect(138,751,255,40),"chapter_"+SiteChapters[selectedSite],true);ActionButton("回到阿安的一天",new Rect(421,751,255,40),"home");}
            else{
                float x=138;var choices=Actions();
                for(int i=0;i<choices.Count;i++){
                    var a=choices[i];float w=Mathf.Clamp(a.text.Length*17+42,190,365);
                    ActionButton(a.text,new Rect(x,751,w,40),a.id,i==0,a.disabled);x+=w+28;
                }
                if(state.chapter==5&&state.paid_wine&&!state.toasted){
                    Fill(new Rect(824,765,185,4),Hex("cec3aa"));Fill(new Rect(824,765,185*state.pour,4),Jade);
                    Label("斟酒  "+Mathf.RoundToInt(state.pour*100)+"%",new Rect(1032,754,170,28),13,Jade);
                }
            }
        }
        void Scrim(){Fill(new Rect(0,0,1440,900),new Color(.12f,.18f,.16f,.55f));}
        void DrawPause()
        {
            Scrim();PaperFill(new Rect(443,130,554,632));Label("画卷暂歇",new Rect(490,168,470,64),42,Ink);
            Label("进度已自动保存。\n右键环顾，数字键 1 / 2 / 3 切换身份视角。\n点击场景里的物件，或按 E 完成当前交互。",new Rect(492,257,460,115),18,Muted);
            string[] names={"回到画中","看看今日账本","声音："+(sound.muted?"关闭":"开启"),"返回观画","退出体验"};
            string[] ids={"resume","journal","sound","menu","quit"};
            for(int i=0;i<5;i++)ActionButton(names[i],new Rect(491,406+i*61,459,49),ids[i],i==0);
        }
        void DrawChapters()
        {
            Scrim();PaperFill(new Rect(130,85,1180,730));Label("选一段人生",new Rect(180,121,800,60),42,Ink);
            Label("独立体验不会覆盖主线存档。完整的一天，从晨起出摊开始。",new Rect(182,193,1000,35),18,Muted);
            string[] descriptions={"拿起竹篮，摆好三筐鲜梨","招呼客人，在价钱与人情间取舍","站在虹桥，看大船穿桥而过","左右交替划桨，顺流抵达码头","收好竹篮，清点今天的账","亲手斟酒，和老友碰一盏","坐进勾栏，听一折市井故事"};
            for(int i=0;i<7;i++){
                float x=180+i%3*362,y=259+i/3*149;Fill(new Rect(x,y,338,130),Hex("e4dcc7"));
                Label($"0{i+1}  /  {DayState.Hours[i].Split('·')[0]}",new Rect(x+18,y+10,300,25),13,Red);
                ActionButton(DayState.Chapters[i]+"  →",new Rect(x+12,y+39,314,40),"chapter_"+i);
                Label(descriptions[i],new Rect(x+18,y+91,310,24),14,Muted);
            }
            ActionButton("返回",new Rect(1125,735,135,44),"menu");
        }
        void DrawJournal()
        {
            Scrim();PaperFill(new Rect(240,100,960,690));
            Label("一日小账",new Rect(295,139,820,65),40,Ink);
            Label($"晨起盘缠  18 文    售梨收入  {state.revenue} 文    今日花费  {state.expenses} 文\n钱袋余钱  {state.coins} 文    剩余鲜梨  {state.stock} 只    人情  {state.reputation}",new Rect(295,225,820,100),21,Ink);
            int y=340;foreach(var sale in state.sales){Label($"{sale.customer}    {sale.qty} 只 × {sale.price} 文 = {sale.total} 文",new Rect(295,y,820,33),18,Muted);y+=40;}
            Label("留在画中的小事",new Rect(295,492,820,36),23,Red);
            Label(string.Join("  ·  ",state.memories),new Rect(295,540,820,125),18,Ink);
            ActionButton("合上账本",new Rect(895,706,240,47),"resume",true);
        }
        void DrawSummary()
        {
            Scrim();PaperFill(new Rect(250,95,940,710));
            Label("一日入画",new Rect(300,130,840,75),44,Ink,true);
            Label("晨起的梨摊、汴河的船影、灯下的一声相识。\n这一日，也成为画中的寻常故事。",new Rect(320,224,800,90),23,Muted,true);
            Label($"售梨 {state.revenue} 文    花费 {state.expenses} 文    余钱 {state.coins} 文    人情 {state.reputation}",new Rect(320,338,800,45),22,Ink,true);
            Label(string.Join("\n",state.memories),new Rect(420,410,620,250),20,Ink);
            ActionButton("再过一日",new Rect(480,718,220,50),"new",true);
            ActionButton("回到长卷",new Rect(740,718,220,50),"menu");
        }
    }
}
