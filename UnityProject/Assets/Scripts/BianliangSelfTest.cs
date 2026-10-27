using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Qingming
{
    public partial class BianliangDemo
    {
        static DayState Route(Dictionary<string,string> picks=null,int random=-1)
        {
            var s=new DayState{sandbox=true};var rng=new System.Random(Math.Max(0,random));int guard=0;
            while(!s.finished&&guard++<160){
                string key=s.chapter+":"+s.step;var available=s.Options().FindAll(a=>!a.disabled);Test(available.Count>0,"reachable response "+key);
                string id=available[0].id;
                if(random>=0)id=available[rng.Next(available.Count)].id;
                if(picks!=null&&picks.TryGetValue(key,out var preferred)&&available.Exists(a=>a.id==preferred))id=preferred;
                Test(!string.IsNullOrEmpty(s.Line())&&!string.IsNullOrEmpty(s.Speaker()),"dialogue "+key);
                Test(s.Apply(id),"accepted response "+id);Test(s.Balanced(),"conservation after "+id);
                if(id!="tea_step"&&id!="next"&&id!="finish")Test(!s.Apply(id),"duplicate response rejected "+id);
                var copy=DayState.Decode(JsonUtility.ToJson(s));Test(copy!=null,"valid save after "+id);copy.sandbox=true;
                Test(copy.Balanced()&&copy.chapter==s.chapter&&copy.step==s.step&&copy.Ending==s.Ending,"resume every beat");s=copy;
            }
            Test(s.finished,"route terminates");return s;
        }
        IEnumerator SelfTest()
        {
            yield return null;string result;
            try{
                TestPortraits();
                Test(!playing,"painting starts first");Test(Resources.Load<Shader>("ScrollWash").isSupported,"painting shader");
                painting.Full();Test(Mathf.Abs(painting.Visible.width-PaintingViewer.Width)<1,"original full scroll");
                var basic=Route();Test(basic.Ending.StartsWith("甲")&&basic.accepted==12,"original agreement ending A");
                var money=Route(new Dictionary<string,string>{{"3:0","gu_honest"},{"6:0","porter"}});
                Test(money.coins==87&&money.revenue==70&&money.expenses==63&&money.good==2&&money.bruised==1,"explicit economy: 80-48+70-4-3-6-2=87");
                var approved=Route(new Dictionary<string,string>{{"4:1","ask_order"}});
                Test(approved.order==9&&approved.accepted==9&&approved.consent&&!approved.hadBreach&&approved.Ending.StartsWith("甲"),"consent before diversion");
                var recovered=Route(new Dictionary<string,string>{{"4:1","sell_reserved"},{"8:1","buyback"}});
                Test(recovered.accepted==12&&recovered.replenishVisits==1&&recovered.hadBreach&&recovered.Ending.StartsWith("乙"),"refund and return loop ending B");
                var purchased=Route(new Dictionary<string,string>{{"4:1","sell_reserved"},{"8:1","restock"}});
                Test(purchased.purchased==27&&purchased.accepted==12&&purchased.replenishVisits==1,"fresh stock loop");
                var notice=Route(new Dictionary<string,string>{{"4:1","notify_order"},{"7:1","leave_short"}});
                Test(notice.notified&&!notice.consent&&notice.order==12&&notice.accepted==9&&notice.Ending.StartsWith("丙"),"notification never implies consent");
                var shortage=Route(new Dictionary<string,string>{{"9:1","family_funds"}});
                Test(shortage.familyFunds==48&&shortage.coins<48&&shortage.Unresolved.Count==0&&shortage.Ending.StartsWith("丁"),"operating capital ending D");
                var quality=Route(new Dictionary<string,string>{{"3:0","gu_hide"}});Test(quality.guResolved&&quality.guBruised==0,"quality swap");
                var refund=Route(new Dictionary<string,string>{{"3:0","gu_hide"},{"8:0","gu_refund"}});Test(refund.guResolved&&refund.guGood==0&&refund.guBruised==0,"quality refund ownership");
                var quiet=Route(new Dictionary<string,string>{{"5:0","bridge_stand"},{"6:0","walk"},{"10:1","drink_water"},{"11:0","show_outside"},{"11:1","axiu_quiet"},{"11:3","answer_silent"},{"11:5","no_backstage"}});
                Test(!quiet.bridge_helped&&!quiet.rodeBoat&&!quiet.paid_wine&&!quiet.theatre_paid&&!quiet.axiuKnows&&quiet.finished,"optional interactions never gate ending");
                var debt=Route(new Dictionary<string,string>{{"6:0","porter"},{"6:1","porter_later"},{"10:0","debt_keep"}});Test(debt.porterDebt==4&&debt.Ending.StartsWith("丙"),"unpaid work retained");
                var promised=Route(new Dictionary<string,string>{{"4:1","sell_reserved"},{"7:1","tomorrow"}});Test(promised.teaFollowup&&promised.accepted<12&&promised.Ending.StartsWith("乙"),"agreed tomorrow obligation explicit");
                for(int i=0;i<240;i++)Route(random:i);
                Test(DayState.Decode("not-json")==null&&DayState.Decode("{\"version\":1}")==null,"corrupt and v1 saves rejected");
                var corrupt=new DayState{coins=-1};Test(DayState.Decode(JsonUtility.ToJson(corrupt))==null,"negative cash rejected");
                var empty=new DayState{coins=0,expenses=80};Test(!empty.Pay(1)&&empty.Balanced(),"insufficient cash never negative");
                for(int i=0;i<14;i++){var preview=new DayState();preview.PrepareChapter(i);Test(preview.sandbox&&preview.chapter==i&&preview.Balanced(),"all scene previews isolated");}
                state=new DayState{sandbox=true};StartChapter(0);
                int steps=0;while(!state.finished&&steps++<160){var a=Actions().Find(x=>!x.disabled);DoAction(a.id);if(work!=null)CompleteWorkForTest();Test(state.Balanced(),"controller integration");}
                Test(state.finished&&state.chapter==13,"actual controller reaches home and summary");
                Test(world.StoryPerson&&world.StoryPerson.name.Contains("安母"),"mother in final scene");
                TestHandsOn();
                result="PASS: 14 scenes; 4 endings; 12 directed routes; 240 seeded routes; money/inventory conservation after every action; duplicate-action rejection; consent vs notification; quality swap/refund; one replenishment loop; NPC knowledge; optional boat/wine/show; work debt; household transfer; every-beat serialization; 14 isolated previews; actual Unity controller integration; original scroll and shader.";
                Debug.Log("QINGMING_SELF_TEST_PASS "+result);
            }catch(Exception e){result="FAIL: "+e;Debug.LogError(result);}
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../tools/unity-port/UnityRuntimeTest.txt"));Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,result);
            SetMenu();painting.Bridge();if(Array.IndexOf(Environment.GetCommandLineArgs(),"-quit-after-test")>=0)Application.Quit(result.StartsWith("PASS")?0:1);
        }
        static void Test(bool condition,string name){if(!condition)throw new Exception("Check failed: "+name);}
    }
}
