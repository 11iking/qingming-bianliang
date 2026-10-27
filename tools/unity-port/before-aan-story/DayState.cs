using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Qingming
{
    [Serializable]
    public class DayState
    {
        public static readonly string[] Chapters = {"晨起出摊","市井买卖","虹桥观舟","汴河行船","薄暮收摊","小酌叙旧","勾栏听曲","一日入画"};
        public static readonly string[] Hours = {"卯时 · 06:00","巳时 · 10:00","午时 · 12:00","未时 · 14:00","酉时 · 18:00","戌时 · 19:00","亥时 · 21:00","夜深 · 22:00"};
        public static readonly Customer[] Customers = {
            new Customer("早起的邻里","阿安，家里孩子惦记这口甜。三只梨，能算便宜些么？",3,3,5,2,4),
            new Customer("赶路的客商","这梨倒新鲜。我要四只带上船，价钱合适就买。",4,4,6,3,5),
            new Customer("茶坊的掌柜","剩下这几只，我拿去配茶。咱们往后常做生意，如何？",5,4,5,3,4)
        };
        [Serializable] public class Sale { public string customer; public int qty,price,total; }
        public class Customer
        {
            public string name,line; public int qty,fair,high,deal,max;
            public Customer(string n,string l,int q,int f,int h,int d,int m)
            {name=n;line=l;qty=q;fair=f;high=h;deal=d;max=m;}
        }
        public int version=1, chapter, coins=18, stock=12, reputation, revenue, expenses, placed, customer_index, strokes, verses;
        public bool haggled,customer_ready,bridge_helped,bridge_watched,packed,counted,paid_wine,toasted,theatre_paid,finished;
        public string last_side="";
        public float pour;
        public List<string> memories=new List<string>();
        public List<Sale> sales=new List<Sale>();
        [NonSerialized] public bool sandbox;
        public static string SavePath => Path.Combine(Application.persistentDataPath,"qingming_unity_day_v1.json");
        public bool Complete()
        {
            switch(chapter) {
                case 0:return placed>=3; case 1:return customer_index>=3;
                case 2:return bridge_helped&&bridge_watched; case 3:return strokes>=10;
                case 4:return packed&&counted; case 5:return toasted;
                case 6:return verses>=3; case 7:return true;
                default:return false;
            }
        }
        public string Trade(string choice,out bool ok)
        {
            ok=false;
            if(customer_index>=3)return "今日的客人都招呼过了。";
            if(!customer_ready)return "先拿起一只梨，请客人瞧瞧。";
            var c=Customers[customer_index];
            if(choice=="high"&&!haggled){haggled=true;return $"“{c.high} 文一只？太贵啦！最多 {c.max} 文，再商量商量？”";}
            int price=choice=="deal"?c.deal:choice=="high"?c.max:c.fair;
            reputation+=choice=="deal"?2:choice=="high"?-1:1;
            int qty=Math.Min(stock,c.qty),total=price*qty;
            coins+=total;revenue+=total;stock-=qty;
            sales.Add(new Sale{customer=c.name,qty=qty,price=price,total=total});
            customer_index++;customer_ready=false;haggled=false;ok=true;
            return $"成交！{qty} 只鲜梨，共 {total} 文。"+(choice=="deal"?"客人记下了这份人情。":"钱货两清，互道一声好。");
        }
        public bool Pay(int amount){if(amount<0||coins<amount)return false;coins-=amount;expenses+=amount;return true;}
        public void Memory(string text){if(!memories.Contains(text))memories.Add(text);}
        public void PrepareChapter(int index)
        {sandbox=true;chapter=Mathf.Clamp(index,0,7);if(index>0)placed=3;if(index>=4){coins=65;revenue=47;stock=0;customer_index=3;}}
        public void Save()
        {
            if(sandbox)return;
            Directory.CreateDirectory(Application.persistentDataPath);
            var temp=SavePath+".tmp";
            File.WriteAllText(temp,JsonUtility.ToJson(this,true));
            if(File.Exists(SavePath))File.Copy(SavePath,SavePath+".bak",true);
            File.Copy(temp,SavePath,true);File.Delete(temp);
        }
        public static DayState Load()
        {
            foreach(var path in new[]{SavePath,SavePath+".bak"})try {
                if(!File.Exists(path))continue;
                var s=JsonUtility.FromJson<DayState>(File.ReadAllText(path));
                if(s==null||s.version!=1)continue;
                s.chapter=Mathf.Clamp(s.chapter,0,7);s.coins=Math.Max(0,s.coins);
                s.stock=Mathf.Clamp(s.stock,0,12);s.placed=Mathf.Clamp(s.placed,0,3);
                s.customer_index=Mathf.Clamp(s.customer_index,0,3);s.strokes=Mathf.Clamp(s.strokes,0,10);
                s.pour=Mathf.Clamp01(s.pour);s.verses=Mathf.Clamp(s.verses,0,3);
                s.memories=s.memories??new List<string>();s.sales=s.sales??new List<Sale>();
                return s;
            } catch(Exception e){Debug.LogWarning("Unable to load Qingming save: "+e.Message);}
            return null;
        }
    }
}
