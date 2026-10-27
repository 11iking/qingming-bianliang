using System.Collections.Generic;
namespace Qingming
{
    public partial class BianliangDemo
    {
        public class Choice {
            public string id,text;public bool disabled;
            public Choice(string i,string t,bool d=false){id=i;text=t;disabled=d;}
        }
        public List<Choice> Actions()
        {
            var a=new List<Choice>();
            switch(state.chapter){
                case 0:a.Add(state.Complete()?new Choice("next","迎接第一位客人  →"):held=="basket"?new Choice("place","E  放到摊面"):new Choice("pick_basket","E  拿起一筐梨"));break;
                case 1:
                    if(state.Complete()){a.Add(new Choice("next","去虹桥看热闹  →"));a.Add(new Choice("to_close","略过游逛，准备收摊"));}
                    else if(!state.customer_ready)a.Add(new Choice("show_pear","E  拿起鲜梨，招呼客人"));
                    else{var c=DayState.Customers[state.customer_index];a.Add(new Choice("fair",$"公道价  ·  {c.fair} 文 / 只"));a.Add(new Choice("high",state.haggled?$"接受还价  ·  {c.max} 文":$"试探开价  ·  {c.high} 文"));a.Add(new Choice("deal",$"让些利  ·  {c.deal} 文 / 只"));}break;
                case 2:
                    if(!state.bridge_helped)a.Add(new Choice("bridge_call","E  招手，照应船夫"));
                    else if(!state.bridge_watched)a.Add(new Choice("bridge_watch",watchingBridge?"货船正在穿桥…":"E  看船过桥",watchingBridge));
                    else{a.Add(new Choice("next","去汴河划船  →"));a.Add(new Choice("to_close","回去收摊"));}break;
                case 3:
                    if(state.Complete())a.Add(new Choice("next","靠岸，回去收摊  →"));
                    else{a.Add(new Choice("row_left","A  划左桨",state.last_side=="left"));a.Add(new Choice("row_right","D  划右桨",state.last_side=="right"));}break;
                case 4:
                    if(!state.packed)a.Add(new Choice("pack","E  收起竹篮"));
                    else if(!state.counted)a.Add(new Choice("count","E  清点今日铜钱"));
                    else{a.Add(new Choice("next","去酒馆找老周  →"));a.Add(new Choice("to_theatre","直接去勾栏听戏"));}break;
                case 5:
                    if(state.toasted){a.Add(new Choice("talk","换作老周，聊聊家常"));a.Add(new Choice("next","去勾栏听曲  →"));a.Add(new Choice("finish","酒足夜深，回家"));}
                    else if(!state.paid_wine){a.Add(new Choice("buy_wine","要一壶酒  ·  6 文",state.coins<6));a.Add(new Choice("to_theatre","改去听戏"));}
                    else if(state.pour>=1)a.Add(new Choice("toast","E  端起酒盏，碰杯"));
                    else if(held=="jug")a.Add(new Choice("pour_hint","按住 E / 鼠标左键斟酒"));
                    else a.Add(new Choice("pick_jug","E  拿起青瓷酒壶"));break;
                case 6:
                    if(state.Complete())a.Add(new Choice("finish","一日已尽，为画卷落款  →"));
                    else if(!state.theatre_paid){a.Add(new Choice("buy_show","入座听曲  ·  4 文",state.coins<4));a.Add(new Choice("finish","留些盘缠，回家"));}
                    else a.Add(new Choice("listen",state.verses==2?"E  听终场，作揖谢幕":"E  听下一折"));break;
            }
            return a;
        }
        public string Speaker()
        {
            switch(state.chapter){
                case 0:return "阿安  /  清晨的自语";
                case 1:return state.customer_index<3?DayState.Customers[state.customer_index].name:"阿安  /  生意告一段落";
                case 2:return "虹桥  /  桥上与船上的呼应";
                case 3:return "船夫  /  借你一支桨";
                case 4:return "阿安  /  薄暮清账";
                case 5:return viewMode==2?"你是老周  /  看着阿安的一天":"老周  /  同桌的朋友";
                case 6:return "勾栏  /  灯火中的一折小戏";
                default:return "汴梁一日";
            }
        }
        public string Narrative()
        {
            if(narrativeOverride!="")return narrativeOverride;
            switch(state.chapter){
                case 0:
                    if(state.placed>=3)return "三筐鲜梨摆好了。河风吹起布幌，街上的脚步渐渐密起来。该迎接第一位客人了。";
                    if(held=="basket")return "竹篮有些分量。把这筐梨放到摊面上，摆得齐整些，客人才愿意停下脚步。";
                    return "天刚亮，阿安已把小车推到虹桥边。拿起左边的竹篮，把十二只鲜梨分三次摆上摊。";
                case 1:
                    if(state.customer_index>=3)return "最后一位客人走远，梨摊空了，钱袋沉了些。桥上传来一阵喧闹，要不要去看看？";
                    var c=DayState.Customers[state.customer_index];
                    if(!state.customer_ready)return c.name+"走到摊前。先拿起一只梨，递近些，让客人看看成色。";
                    return state.haggled?$"“这个价有些高。最多 {c.max} 文一只，行不行？”你可以接受还价，也可以换个价钱。":"“"+c.line+"”";
                case 2:
                    if(state.Complete())return "船缓缓穿过桥影，船夫向岸上拱了拱手。热闹散开，汴河依旧向前。去水上走一段吧。";
                    if(watchingBridge)return "你暂且停下脚步。货船正沿着水路穿过虹桥，船身轻轻起伏，桥上的人也跟着松了一口气。";
                    return state.bridge_helped?"“多谢照应！”船夫在水面上应声。换到画外视角，看舟船怎样从桥下经过。":"桥上的人纷纷倚栏。一艘货船正驶向桥洞，船夫忙着看水路。你站在桥头，替他照应一声。";
                case 3:return state.Complete()?"十桨过后，船轻轻贴近码头。你听懂了这条河的节奏：左一下，右一下，日子往前走。":"“别急，用腰带着手。”按 A 划左桨，按 D 划右桨；也可以点下面的按钮。左右交替，划满十桨。";
                case 4:
                    if(state.Complete())return $"售梨收入 {state.revenue} 文，钱袋共 {state.coins} 文。日头落了，去酒馆找老周坐坐，或直接去勾栏听戏。";
                    return state.packed?"收起竹篮，再把铜钱一枚枚理清。今天挣下的不只是盘缠，还有熟客的一声招呼。":"夕照落在空空的摊面上。把竹篮收上车，清点今日所得，给忙碌的一天留个收尾。";
                case 5:
                    if(state.toasted)return "“来，为今日的好生意。”两盏相碰，清脆一声。可以换到朋友的视角，再看看阿安。";
                    if(state.pour>=1)return "酒斟到八分，给余下的留点人情。拿起桌上的酒盏，与老周碰一杯。";
                    if(held=="jug")return "慢慢倾斜酒壶。按住 E 或鼠标左键斟酒，松开便停下；下方刻度会告诉你斟了多少。";
                    return state.paid_wine?"一壶薄酒，两个粗瓷盏。拿起桌上的青瓷酒壶，为这位老朋友斟酒。":"老周早在孙羊正店等着：“忙了一日，坐。今夜的河风正好，咱们喝一壶？”酒钱六文。";
                case 6:
                    if(!state.theatre_paid)return "鼓板响了，勾栏里还有空位。花四文入座，听一折为本体验创作的市井小戏《一担春光》。";
                    return new[]{
                        "开场 · “汴水悠悠接远帆，一担春光过市南。”台上的卖货郎挑担而来，故事刚刚开始。",
                        "第二折 · “三文买梨嫌价贵，半盏清茶话短长。”客人几番还价，卖货郎却多赠了一只。",
                        "第三折 · “莫道市声皆为利，一声相识便心安。”归家的路上，他又遇见了白日的客人。",
                        "终场 · “画中不记寻常事，灯火深深各自还。”台上人作揖，台下人轻轻拍手。该回家了。"}[state.verses];
                default:return "";
            }
        }
    }
}
