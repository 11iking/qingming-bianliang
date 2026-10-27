using System;
using System.Collections.Generic;
using System.Linq;
namespace Qingming
{
    // A reversible workbench reservation. DayState is changed only after the final handover.
    public class HandsWork
    {
        public enum Mode { Inspect, Pears, Money, Carry, Move }
        public class Item {
            public int id,value=1; public string kind="pear",zone="source",returnZone="source";
            public bool bad,inspected; public float turned;
        }
        public readonly string action;
        public Mode mode=Mode.Pears;
        public readonly List<Item> items=new List<Item>();
        public int needed,neededBad,payment,availableCash,held=-1,waypoint;
        public bool receiving,done;
        public string response="",title="",instruction="";
        public Item Held=>held<0?null:items.Find(x=>x.id==held);
        public int Count=>items.Count(x=>x.zone=="target");
        public int BadCount=>items.Count(x=>x.zone=="bad"||x.zone=="target"&&x.bad);
        public int Sum=>items.Where(x=>x.zone=="target").Sum(x=>x.value);
        public bool Ready=>held<0&&(mode==Mode.Inspect?Count>=4&&BadCount>=2:mode==Mode.Money?Sum==needed:mode==Mode.Carry?waypoint>=3&&Count==1:Count==needed&&items.Count(x=>x.zone=="target"&&x.bad)==neededBad);
        HandsWork(string id){action=id;}
        public static HandsWork Create(string id,DayState s)
        {
            var w=new HandsWork(id){availableCash=s.coins};int g=s.good,b=s.bruised;
            switch(id){
            case "inspect":w.mode=Mode.Inspect;w.needed=6;g=22;b=2;w.title="果行 · 翻面验货";break;
            case "reserve":w.needed=12;w.title="十二只好梨 · 逐只装入订货篓";break;
            case "gu_good":w.needed=3;w.payment=12;w.receiving=true;w.title="顾嫂 · 三只完好梨";break;
            case "gu_honest":w.needed=3;w.neededBad=1;w.payment=10;w.receiving=true;w.title="顾嫂 · 两好一伤，说明后交易";break;
            case "gu_hide":w.needed=3;w.neededBad=1;w.payment=12;w.receiving=true;w.title="顾嫂 · 未说明碰伤的交易";break;
            case "money_count":case "money_demo":w.needed=4;w.payment=16;w.receiving=true;w.title="沈客 · 挑梨、交货、收钱";break;
            case "ask_order":case "notify_order":case "sell_reserved":w.needed=s.good-9;w.payment=w.needed*4;w.receiving=true;w.title="沈客增购 · 数清实际交出的梨";break;
            case "deliver":w.needed=Math.Min(s.good,s.order-s.accepted);if(w.needed==0)return null;w.payment=w.needed*3;w.receiving=true;w.title="许娘子 · 逐只验收订货";break;
            case "gu_swap":w.needed=1;w.title="顾嫂 · 换回一只好梨";break;
            case "gu_refund":w.needed=s.guGood+s.guBruised;w.neededBad=s.guBruised;g=s.guGood;b=s.guBruised;w.payment=s.guPaid;w.title="收回原货 · 再退还货款";break;
            case "late_sell":w.needed=2;w.payment=8;w.receiving=true;w.title="收摊前 · 再交两只好梨";break;
            case "gift":w.needed=1;w.neededBad=b>0?1:0;w.title="赠梨 · 亲手交给顾嫂";break;
            case "batch_keep":case "batch_replace":w.payment=48;break;
            case "batch_discount":w.payment=46;break;
            case "porter_pay":case "debt_pay":w.payment=s.porterDebt;break;
            case "buyback":case "restock":w.payment=(s.order-s.accepted)*4;break;
            case "repair":case "boat":w.payment=3;break;
            case "buy_wine":w.payment=6;break;
            case "drink_tea":case "show_pay":w.payment=2;break;
            case "family_funds":w.payment=48;break;
            case "walk":w.mode=Mode.Carry;w.needed=1;w.title="亲自送货 · 提篓沿岸走到茶坊";w.Add("basket");w.RefreshInstructions();return w;
            case "rope_fix":case "place":case "pack":case "bridge_help":case "home_rest":
                w.mode=Mode.Move;w.needed=1;w.title=id=="rope_fix"?"检查空篓 · 提起试重后放稳":id=="bridge_help"?"让出桥口 · 移开自己的篓":"搬放竹篮 · 拿起后放到标记位置";w.Add("basket");w.RefreshInstructions();return w;
            case "return_cloth":if(!s.cloth||s.clothReturned)return null;w.mode=Mode.Move;w.needed=1;w.title="归还借物 · 把布巾递给顾嫂";w.Add("cloth");w.RefreshInstructions();return w;
            case "mother":w.mode=Mode.Move;w.needed=1;w.title="接过家中本钱 · 收好钱袋";w.Add("purse");w.RefreshInstructions();return w;
            default:return null;
            }
            if(w.needed>0){if(w.mode!=Mode.Inspect)w.mode=Mode.Pears;for(int i=0;i<g+b;i++)w.Add("pear",i>=g);}
            else w.OpenMoney();
            w.RefreshInstructions();return w;
        }
        void Add(string kind,bool bad=false,int value=1){items.Add(new Item{id=items.Count,kind=kind,bad=bad,value=value});}
        public void OpenMoney(){
            mode=Mode.Money;needed=payment;held=-1;items.Clear();
            int pool=receiving?payment:Math.Min(availableCash,payment+8);
            int singles=Math.Min(10,pool);for(int i=0;i<singles;i++)Add("coin",false,1);pool-=singles;
            while(pool>=5){Add("coin",false,5);pool-=5;}while(pool-->0)Add("coin",false,1);
            title=receiving?"收款 · 将对方的钱逐枚 / 逐叠收入钱袋":"付款 · 数好铜钱，递到对方钱盘";
            response=receiving?"对方已将议定货款摆在钱盘上。你再数一遍，钱货才算两清。":"对方等你把钱数清。盘中不足或多放，都可以取回重数。";RefreshInstructions();
        }
        public bool Grab(int id){var i=items.Find(x=>x.id==id);if(done||Held!=null||i==null)return false;held=id;i.returnZone=i.zone;i.zone="held";response=i.kind=="pear"?(i.inspected?(i.bad?"这只梨有轻伤。看清后再决定放哪里。":"这只表皮完好。可以放入对应的篓或盘。"):"梨拿在手里了。按 R 或滚轮转动，检查背面和底部。") :i.kind=="coin"?"手里是 "+i.value+" 枚实物铜钱。放错可拿回，不会自动扣钱。":"已经拿稳。靠近标记处，再放下。";return true;}
        public bool Turn(float degrees){if(Held==null)return false;Held.turned+=Math.Abs(degrees);if(Held.turned>=120){Held.inspected=true;if(Held.kind=="pear")response=Held.bad?"翻到底面，发现一处褐色碰伤。它不是腐坏，须说明后另售。":"转过背面与底部，这只没有发现碰伤。";}return true;}
        public bool Place(string zone){
            var i=Held;if(i==null||done)return false;
            if(zone=="source"){i.zone="source";held=-1;response="先放回原处。没有成交，也没有丢失物品。";return true;}
            if(zone!="target"&&zone!="bad")return false;
            if(mode==Mode.Inspect){if(!i.inspected){response="卖者：先转到背面看看，别只瞧朝上的一面。";return false;}if(i.bad!=(zone=="bad")){response=i.bad?"这只有碰伤，放到轻伤盘再核对。":"这只没有碰伤，放到好梨盘。";return false;}}
            else {
                if(zone=="bad")return false;
                if(mode==Mode.Pears){int stagedBad=items.Count(x=>x.zone=="target"&&x.bad);int stagedGood=Count-stagedBad;
                    if(i.bad&&stagedBad>=neededBad){response="对方把梨推回：这只碰伤了，咱们说好的是完好梨。请换一只。";i.inspected=true;return false;}
                    if(!i.bad&&stagedGood>=needed-neededBad||Count>=needed){response="对方抬手示意：数量或成色与刚才约定不符，先放回去。";return false;}
                }
                if(mode==Mode.Carry&&waypoint<3){response="还没到茶坊。提稳篓，沿路找到下一个标记。";return false;}
            }
            i.zone=zone;held=-1;
            response=mode==Mode.Money?"盘中已有 "+Sum+" 枚，应交 "+needed+" 枚。"+(Sum>needed?"对方指出：多了 "+(Sum-needed)+" 枚，取回一些。":Sum==needed?"数目相符，可以当面确认。":"继续数，别急着结。"):
                mode==Mode.Inspect?"已经检查：好梨 "+Count+" 只，轻伤 "+BadCount+" 只。":mode==Mode.Pears?"对方伸手看过，盘中 "+Count+" / "+needed+" 只。"+(Ready?"这一份齐了，确认交接后再结钱。":"还差 "+(needed-Count)+" 只。") :"已经放稳。可以确认这一趟交接。";
            return true;
        }
        public void ReturnHeld(){if(Held==null)return;Held.zone=Held.returnZone;held=-1;response="手里的物件放回拿起的位置。";}
        public bool Advance(){if(!Ready){response=Held!=null?"先把手里的东西放稳。":mode==Mode.Money?"钱数还没有对上。可以拿回盘中的钱，再重新摆放。":"还没有完成检查或数量没有对齐。";return false;}
            if(mode==Mode.Pears&&payment>0){OpenMoney();return true;}done=true;return true;}
        void RefreshInstructions(){instruction=mode==Mode.Inspect?"检查至少六只，找出两只轻伤梨 · 把好梨和轻伤梨分开放":mode==Mode.Money?"实物铜钱按 1 枚 / 5 枚一叠摆放 · 放到右侧钱盘，数对后确认":mode==Mode.Carry?"先拿起竹篮，再 WASD 沿街行走 · 到茶坊门前放下":mode==Mode.Pears?"拿起左侧的梨，翻看后放到右侧 · 约定 "+needed+" 只，其中轻伤 "+neededBad+" 只":"拿起物件，移动或转身，放到另一处标记";}
    }
}
