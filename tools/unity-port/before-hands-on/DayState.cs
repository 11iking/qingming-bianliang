using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Qingming
{
    [Serializable] public partial class DayState
    {
        public static readonly string[] Chapters={"门还没开","一筐梨有几种价","声音要让人听见","顾嫂不是来讨便宜","生客的钱","桥口不能只看热闹","船边的活","十二只与一句实话","回来的人","钱要数两遍","一壶酒不抵一句话","棚里唱的是谁","夜里仍有人做事","篓放在门里"};
        public static readonly string[] Hours={"破晓 · 家门","清晨 · 果行","辰时 · 梨摊","上午 · 梨摊","近午 · 梨摊","午间 · 虹桥","午后 · 河岸","午后 · 茶坊","下午 · 梨摊","薄暮 · 收摊","入夜 · 小酒肆","夜间 · 瓦舍","夜深 · 街巷","归家 · 灯下"};
        public static readonly int[] LastSteps={3,3,3,3,4,2,2,4,4,2,4,7,2,2};
        public int version=2,chapter,step,coins=80,revenue,expenses,good,bruised,purchased,sold,gifts,losses,returned;
        public int order=12,accepted,porterDebt,teaPractice,replenishVisits,shenOwned,guGood,guBruised,guPaid,familyFunds;
        public bool finished,ropeFixed,inspected,discountBatch,cloth,clothReturned,watchAgreed,watchNotified,guDispute,guResolved;
        public bool notified,consent,diverted,hadBreach,teaDeferred,teaClosed,teaFollowup,repairDone,lateSold,bridge_helped,bridge_watched;
        public bool porterAgreed,porterPaid,rodeBoat,paid_wine,toasted,theatre_paid,axiuKnows,backstage,packed,counted,jugReady;
        public float pour;
        public string route="",drink="",audience="",receipt="";
        public List<string> memories=new List<string>();
        public List<string> ledger=new List<string>();
        public List<string> dialogue=new List<string>();
        [NonSerialized] public bool sandbox;
        public int stock=>good+bruised;
        public int reserved=>Math.Min(good,Math.Max(0,order-accepted));
        public int retail=>Math.Max(0,good-reserved);
        public int deficit=>Math.Max(0,order-accepted-good);
        public bool Complete()=>step>=LastSteps[chapter];
        public int Stage=>chapter==5?2:chapter==6&&rodeBoat?3:chapter==10?5:chapter==11?6:chapter>=8?4:chapter>=3?1:0;
        public bool Balanced()=>purchased+returned==stock+sold+gifts+losses&&coins==80+revenue-expenses&&good>=0&&bruised>=0&&coins>=0;
        public string Ending=>Unresolved.Count>0?"丙 · 篓里还有话":coins<48?"丁 · 先把本钱留下":hadBreach?"乙 · 迟了也得去":"甲 · 明日照旧";
        public List<string> Unresolved {
            get {var r=new List<string>();if(guDispute&&!guResolved)r.Add("顾嫂的果品争议尚未解决");if(porterDebt>0)r.Add("欠老周约定工钱 "+porterDebt+" 枚");if(accepted<order&&!teaFollowup)r.Add("茶坊仍缺 "+(order-accepted)+" 只，未获同意改约");if(cloth&&!clothReturned)r.Add("顾嫂的布巾尚未归还");return r;}
        }
        public string Obligations()=>"茶坊：约定 "+order+" 只 / 已验收 "+accepted+" 只"+(teaFollowup?"（已当面同意明日补交）":consent?"（双方已改约）":notified?"（仅已通知，未获同意）":"")+"\n"+(Unresolved.Count==0?"当前没有未解决的约定。":string.Join("；",Unresolved));
        public bool Pay(int amount,string reason="支出") {if(amount<0||coins<amount)return false;coins-=amount;expenses+=amount;ledger.Add(reason+"  −"+amount+" 枚");return true;}
        public bool Sell(int g,int b,int amount,string buyer) {if(g<0||b<0||amount<0||good<g||bruised<b)return false;good-=g;bruised-=b;sold+=g+b;coins+=amount;revenue+=amount;ledger.Add(buyer+"：完好 "+g+" / 轻伤 "+b+"，收 "+amount+" 枚");return true;}
        public bool Buy(int g,int b,int amount,string seller){if(g<0||b<0||!Pay(amount,seller))return false;good+=g;bruised+=b;purchased+=g+b;return true;}
        public void Memory(string t){if(!memories.Contains(t))memories.Add(t);}
        public void Next(){if(!Complete()||chapter>=13)return;chapter++;step=0;receipt="";}
        public void PrepareChapter(int index){sandbox=true;index=Mathf.Clamp(index,0,13);int guard=0;while(chapter<index&&guard++<150){if(Complete())Next();else {var a=Options();Apply(a[0].id);}}receipt="";}
        public static string SavePath=>Path.Combine(Application.persistentDataPath,"qingming_aan_day_v2.json");
        public void Save(){if(sandbox)return;Directory.CreateDirectory(Application.persistentDataPath);string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(this,true));if(File.Exists(SavePath))File.Copy(SavePath,SavePath+".bak",true);File.Copy(temp,SavePath,true);File.Delete(temp);}
        public static DayState Decode(string json){
            try {var s=JsonUtility.FromJson<DayState>(json);
                if(s==null||s.version!=2||s.chapter<0||s.chapter>13||s.step<0||s.step>LastSteps[s.chapter]||!s.Balanced()||s.purchased<0||s.sold<0||s.gifts<0||s.losses<0||s.returned!=0||s.order<0||s.order>12||s.accepted<0||s.accepted>s.order||s.porterDebt<0||s.porterDebt>4||s.replenishVisits<0||s.replenishVisits>1||s.teaPractice<0||s.teaPractice>7||s.familyFunds<0||s.familyFunds>48||s.finished&&(s.chapter!=13||!s.Complete()))return null;
                s.memories=s.memories??new List<string>();s.ledger=s.ledger??new List<string>();s.dialogue=s.dialogue??new List<string>();return s;
            }catch{return null;}
        }
        public static DayState Load(){foreach(var p in new[]{SavePath,SavePath+".bak"})try {if(!File.Exists(p))continue;var s=Decode(File.ReadAllText(p));if(s!=null)return s;}catch(Exception e){Debug.LogWarning(e.Message);}return null;}
    }
}
