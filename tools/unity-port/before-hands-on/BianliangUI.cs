using System;
using UnityEngine;
namespace Qingming
{
    public partial class BianliangDemo
    {
        PaintingViewer painting;
        Vector2 dialogueScroll,journalScroll;
        int journalTab,selectedChoice;
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
            buttonStyle=new GUIStyle{wordWrap=true,clipping=TextClipping.Clip,font=serif,alignment=TextAnchor.MiddleCenter,fontSize=17};
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
            if(Time.unscaledTime<toastUntil&&playing&&!paused&&!transitioning&&panel=="game"){Fill(new Rect(300,111,840,42),new Color(.25f,.25f,.2f,.8f));Label(toast,new Rect(315,120,810,30),16,Paper,true);}
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
            Label($"{state.chapter+1:00} / 14   {DayState.Hours[state.chapter]}  ·  {DayState.Chapters[state.chapter]}",new Rect(279,29,865,31),17,Muted);
            Label(state.sandbox?"重游片段 · 不保存主线进度":"立秋后的一天 · 北宋末年生活背景下的虚构人物故事",new Rect(280,65,850,24),13,Muted);
            ActionButton("账本与人物  Tab",new Rect(1165,24,210,36),"journal",false,false,15);
            ActionButton("暂歇  Esc",new Rect(1244,65,130,28),"pause",false,false,13);
            bool preview=viewMode==3&&selectedSite!=SiteForChapter();
            if(viewMode==3){
                for(int i=0;i<SiteNames.Length;i++){
                    Vector3 p=cam.WorldToScreenPoint(SitePoints[i]);if(p.z<=0)continue;
                    Vector2 pos=new Vector2((p.x-CanvasOffset.x)/CanvasScale,(Screen.height-p.y-CanvasOffset.y)/CanvasScale);
                    if(pos.x<85||pos.x>1355||pos.y<135||pos.y>(interactionOpen?400:760))continue;
                    Rect r=new Rect(pos.x-18,pos.y-18,36,36);Fill(r,i==SiteForChapter()?Red:Hex("7b795f"));
                    if(Button("印",r,false,false,18)&&!didDrag)BrowseSite(i);
                    if(r.Contains(Event.current.mousePosition)||i==selectedSite){Fill(new Rect(pos.x-85,pos.y+23,170,30),Paper);Label(SiteNames[i],new Rect(pos.x-83,pos.y+26,166,25),16,Ink,true);}
                }
            }
            ActionButton(viewMode==3?"走近阿安  1":"退回画卷  3",new Rect(62,838,205,42),viewMode==3?"view_1":"view_3");
            ActionButton("对坐视角  2",new Rect(286,838,170,42),"view_2",false,false,15);
            Label("点击人物交谈 · ↑↓ 选回应 / Enter 确认 · E 单步交互",new Rect(468,850,690,27),14,Muted,true);
            ActionButton(interactionOpen?"收起对话":"人物对话",new Rect(1190,838,184,42),interactionOpen?"close":"open",true);
            if(state.sandbox)ActionButton("回主线",new Rect(63,113,140,36),"restore_main",true);
            if(!interactionOpen)return;
            PaperFill(new Rect(76,430,1288,374));Fill(new Rect(76,430,5,374),Red);
            Label(preview?SiteNames[selectedSite]:Speaker(),new Rect(103,450,735,34),22,Red);
            Label($"钱 {state.coins} 枚   好梨 {state.good} / 轻伤 {state.bruised}   预留 {state.reserved}",new Rect(103,488,745,27),15,Muted);
            if(preview){Label(SiteLines[selectedSite],new Rect(103,544,720,120),21,Ink);ActionButton("独立重游此段",new Rect(911,544,420,52),"chapter_"+SiteChapters[selectedSite],true);ActionButton("回阿安当前进度",new Rect(911,613,420,52),"home");return;}
            string prose=(state.receipt.Length>0?"刚才的回应 · "+state.receipt+"\n\n":"")+Narrative();
            textStyle.fontSize=20;textStyle.normal.textColor=Ink;textStyle.alignment=TextAnchor.UpperLeft;
            float h=Mathf.Max(260,textStyle.CalcHeight(new GUIContent(prose),730)+18);
            dialogueScroll=GUI.BeginScrollView(new Rect(103,532,768,255),dialogueScroll,new Rect(0,0,738,h));
            Label(prose,new Rect(0,0,730,h),20,Ink);GUI.EndScrollView();
            var choices=Actions();for(int i=0;i<choices.Count;i++){
                var c=choices[i];ActionButton((selectedChoice==i?"›  ":"")+c.text,new Rect(908,451+i*52,426,45),c.id,i==selectedChoice,c.disabled,16);
            }
            if(Button("本场依据与创作边界",new Rect(923,766,400,28),false,false,13)){journalTab=3;journalScroll=Vector2.zero;Action("journal");}
        }
        void Scrim(){Fill(new Rect(0,0,1440,900),new Color(.12f,.18f,.16f,.55f));}
        void DrawPause()
        {
            Scrim();PaperFill(new Rect(443,130,554,632));Label("画卷暂歇",new Rect(490,168,470,64),42,Ink);
            Label("进度已自动保存。\n右键环顾，数字键 1 / 2 / 3 切换身份视角。\n点击人物与物件；↑↓ 选择，Enter 确认，E 单步。",new Rect(492,257,460,115),18,Muted);
            string[] names={"回到画中","看看今日账本","声音："+(sound.muted?"关闭":"开启"),"返回观画","退出体验"};
            string[] ids={"resume","journal","sound","menu","quit"};
            for(int i=0;i<5;i++)ActionButton(names[i],new Rect(491,406+i*61,459,49),ids[i],i==0);
        }
        void DrawChapters()
        {
            Scrim();PaperFill(new Rect(100,70,1240,760));Label("重游阿安的一天",new Rect(145,105,950,60),38,Ink);
            Label("十四场独立预览采用正常履约的前情；不会覆盖主线。主线中的选择会跨场保留。",new Rect(146,179,1110,35),17,Muted);
            for(int i=0;i<14;i++){
                float x=145+i%4*287,y=240+i/4*126;Fill(new Rect(x,y,266,109),Hex("e4dcc7"));
                Label($"{i+1:00}  {DayState.Hours[i]}",new Rect(x+13,y+10,240,26),13,Red);
                ActionButton(DayState.Chapters[i]+"  →",new Rect(x+8,y+42,250,57),"chapter_"+i,false,false,17);
            }
            ActionButton("返回观画",new Rect(1110,760,180,44),"menu");
        }
        void DrawJournal()
        {
            Scrim();PaperFill(new Rect(165,75,1110,750));Label("阿安的一日手记",new Rect(208,102,890,58),36,Ink);
            string[] tabs={"钱货账","人物与约定","对白回看","史料与边界"};
            for(int i=0;i<4;i++)if(Button(tabs[i],new Rect(209+i*256,174,241,43),journalTab==i)){journalTab=i;journalScroll=Vector2.zero;}
            string content=JournalText();textStyle.fontSize=19;textStyle.alignment=TextAnchor.UpperLeft;
            float height=Mathf.Max(470,textStyle.CalcHeight(new GUIContent(content),974)+30);
            journalScroll=GUI.BeginScrollView(new Rect(209,240,1023,465),journalScroll,new Rect(0,0,984,height));
            Label(content,new Rect(0,0,974,height),19,Ink);GUI.EndScrollView();
            if(journalTab==3&&Button("打开本场史料原文",new Rect(216,746,278,45)))Application.OpenURL(AnHistory.Urls[state.chapter]);
            ActionButton("回到刚才",new Rect(981,746,244,45),"resume",true);
        }
        string JournalText()
        {
            if(journalTab==0)return $"期初本钱 80 枚  +  累计收款 {state.revenue} 枚  −  支出、退款及家用转出 {state.expenses} 枚  =  钱袋 {state.coins} 枚\n\n现存：完好 {state.good} / 轻伤 {state.bruised}；其中预留好梨 {state.reserved}。\n累计进货 {state.purchased}；净售出 {state.sold}；赠出 {state.gifts}；损失 {state.losses}；现存 {state.stock}。\n钱货核对："+(state.Balanced()?"相符":"需要检查")+"。数字均为虚构玩法设定；本账以实际铜钱枚数计，不换算短陌，不将余额差称为利润。\n\n"+string.Join("\n",state.ledger);
            if(journalTab==1)return state.Obligations()+"\n\n安母｜原订单与本钱；等阿安归家交代。\n顾嫂｜"+(state.guDispute?(state.guResolved?"果品争议已解决。":"果品争议未解决。") :"买卖成色已当面说明。")+(state.watchAgreed?"照看到午后的约定已记下。":"没有全天托看约定。")+"\n借物｜"+(state.cloth?(state.clothReturned?"布巾已还。":"布巾仍在篓中。") :"未借布巾。")+"\n沈客｜按实物枚数结算；还持有 "+state.shenOwned+" 只已购梨。\n许娘子｜只凭订单、来话和实际交付验收。\n何四｜"+(state.bridge_helped?"亲眼见过阿安岸上让路。":"未受阿安帮忙；船员仍完成本职工作。")+"\n老周｜"+(state.porterAgreed?(state.porterDebt>0?"受托帮运，工钱待付。":"受托帮运，工钱已清。") :"未受托搬运。")+"\n阿绣｜"+(state.axiuKnows?"演前听过阿安主动讲述送货经过。":"没有获知茶坊事件，不会提未听说的私事。")+"\n\n记住的小事\n"+string.Join("\n",state.memories);
            if(journalTab==2)return state.dialogue.Count==0?"尚未做出回应。每次选择后，对话会保存在这里。":string.Join("\n\n────────\n\n",state.dialogue);
            return "本场 · "+DayState.Chapters[state.chapter]+"\n\n"+AnHistory.Notes[state.chapter]+"\n\n共同边界\n阿安及其人际关系为虚构；故事取立秋后的一天，不断言《清明上河图》描绘秋季。空间经过压缩，人物模型与操作为示意。按钮、字幕、视角与分支属于现代交互设计。\n\n每个历史背景都有文献入口；具体对白、买卖数量、收费和一日事件不是史书实录。语音、手部追踪、真实绳索与专业茶事复原尚未启用。\n\n完整的三十条研究记录与四十四项动作映射，见项目“剧本”目录的阅读版。";
        }
        void DrawSummary()
        {
            Scrim();PaperFill(new Rect(220,86,1000,730));
            Label(state.Ending,new Rect(270,120,900,68),40,Ink,true);
            string ending=state.Unresolved.Count>0?"安母：篓可以放下，欠着的话不能一并放下。明日先去见该见的人。":state.coins<48?"安母：事情说清了，本钱还要安排。先把明日能做的买卖定下来。":state.hadBreach?"安母：后来补过了，也是该做的。明日早点去，把答应的接上。":"安母：数对了，话也对上了。把明日的本钱留下，歇吧。";
            Label(ending,new Rect(290,218,860,92),23,Muted);
            Label($"钱袋 {state.coins} 枚 · 完好梨 {state.good} · 轻伤梨 {state.bruised}\n茶坊验收 {state.accepted} / 现约 {state.order} 只 · 搬运欠款 {state.porterDebt} 枚 · 家用另拨 {state.familyFunds} 枚",new Rect(290,333,860,78),21,Ink,true);
            Label(state.Obligations(),new Rect(290,443,860,133),20,Ink);
            Label("明日进货参考本钱：48 枚。余钱不等于利润。\n这一天由具体约定与钱货留下结果，没有统一的人情分。",new Rect(290,600,860,66),17,Muted);
            ActionButton("回看账本",new Rect(277,727,245,49),"journal");
            ActionButton("再过一日",new Rect(590,727,245,49),"new",true);
            ActionButton("回到长卷",new Rect(903,727,245,49),"menu");
        }
    }
}
