using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Qingming
{
    public partial class BianliangDemo
    {
        IEnumerator SelfTest()
        {
            yield return null;
            string result;
            try{
                Test(playing==false,"starts in painting viewer");
                var wash=Resources.Load<Shader>("ScrollWash");
                Test(wash&&wash.isSupported&&atmosphere,"scroll wash available in player");
                painting.Full();Test(Mathf.Abs(painting.Visible.width-PaintingViewer.Width)<1,"full scroll");
                painting.Bridge();painting.ZoomAt(1,PaintingViewer.Detail.center);Test(painting.zoom==1,"native pixels");
                var pivot=PaintingViewer.Detail.center+new Vector2(110,-30);
                var anchor=(pivot-painting.Origin)/painting.zoom;painting.ZoomAt(1.24f,pivot);
                Test(Vector2.Distance((pivot-painting.Origin)/painting.zoom,anchor)<.1f,"zoom anchor");
                state=new DayState{sandbox=true};StartChapter(0);
                for(int i=0;i<3;i++){DoAction("pick_basket");Test(held=="basket","pickup basket");DoAction("place");}
                Test(state.Complete()&&state.placed==3,"stock pears");
                DoAction("next");
                for(int i=0;i<3;i++){DoAction("show_pear");DoAction("fair");}
                Test(state.customer_index==3&&state.stock==0&&state.revenue==45&&state.coins==63,"fair trades");
                DoAction("next");DoAction("bridge_call");DoAction("bridge_watch");
                world.cargoBoat.localPosition=G(12,0,.2f);state.bridge_watched=true;watchingBridge=false;
                Test(state.Complete(),"bridge chapter");DoAction("next");
                for(int i=0;i<10;i++){rowCooldown=0;Row(i%2==0?"left":"right");}
                Test(state.Complete()&&world.boatProgress==1,"ten alternating strokes");
                DoAction("next");DoAction("pack");DoAction("count");Test(state.Complete(),"closing stall");
                DoAction("next");DoAction("buy_wine");DoAction("buy_wine");Test(state.coins==57,"wine charged once");
                DoAction("pick_jug");Test(held=="jug","jug pickup");state.pour=1;DoAction("toast");DoAction("toast");Test(state.toasted,"toast");
                DoAction("next");DoAction("buy_show");DoAction("buy_show");for(int i=0;i<3;i++)DoAction("listen");
                Test(state.Complete()&&state.coins==53&&state.expenses==10,"theatre and accounting");
                DoAction("finish");Test(state.finished&&state.chapter==7,"summary");
                var preview=new DayState();preview.PrepareChapter(5);Test(preview.sandbox,"preview save isolation");
                var haggling=new DayState{customer_ready=true};
                haggling.Trade("high",out bool first);Test(!first&&haggling.coins==18,"haggling does not transact");
                haggling.Trade("high",out bool second);Test(second&&haggling.coins==30&&haggling.reputation==-1,"accept counteroffer");
                var generous=new DayState{customer_ready=true};generous.Trade("deal",out bool deal);
                Test(deal&&generous.coins==24&&generous.reputation==2,"generous trade");
                Test(!new DayState{coins=3}.Pay(4),"insufficient funds");
                var roundTrip=JsonUtility.FromJson<DayState>(JsonUtility.ToJson(state));
                Test(roundTrip.coins==53&&roundTrip.finished&&roundTrip.sales.Count==3,"save serialization");
                result="PASS: scroll wash shader, original scroll, zoom anchoring, baskets, all trades, haggling, reputation, bridge, alternating rowing, closing, wine, toast, theatre, summary, accounting, preview isolation, save serialization.";
                Debug.Log("QINGMING_SELF_TEST_PASS "+result);
            }catch(Exception e){result="FAIL: "+e;Debug.LogError(result);}
            var output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../tools/unity-port/UnityRuntimeTest.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,result);
            SetMenu();painting.Bridge();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-quit-after-test")>=0)Application.Quit(result.StartsWith("PASS")?0:1);
        }
        static void Test(bool condition,string name){if(!condition)throw new Exception("Check failed: "+name);}
    }
}
