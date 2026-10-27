using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Qingming
{
    public partial class BianliangDemo
    {
        void CompleteWorkForTest()
        {
            int guard=0;
            while(work!=null&&guard++<160){
                Test(state.Balanced(),"money/inventory unchanged during handling");
                if(work.Ready){Test(ConfirmWork(),"confirm completed work");continue;}
                if(work.mode==HandsWork.Mode.Carry){
                    if(work.Held==null)Test(GrabWorkItem(0),"pick delivery basket");
                    for(int j=0;j<3;j++){int walkGuard=0;while(work.waypoint<=j&&walkGuard++<650){Vector3 d=DeliveryStops[j]-world.player.localPosition;d.y=0;WalkStep(Vector3.ClampMagnitude(d,.12f));CheckDeliveryProgress();}Test(work.waypoint>j,"walk through delivery stop "+j);}
                    Test(PlaceWorkItem("target"),"put basket at tea house");continue;
                }
                HandsWork.Item item=null;
                if(work.mode==HandsWork.Mode.Inspect){bool bad=work.Count>=4;item=work.items.Find(i=>i.zone=="source"&&i.bad==bad);}
                else if(work.mode==HandsWork.Mode.Money){int remaining=work.needed-work.Sum;item=work.items.Find(i=>i.zone=="source"&&i.value==5&&i.value<=remaining)??work.items.Find(i=>i.zone=="source"&&i.value<=remaining);}
                else {int bads=work.items.FindAll(i=>i.zone=="target"&&i.bad).Count;bool wantBad=work.Count>=work.needed-work.neededBad;item=work.items.Find(i=>i.zone=="source"&&i.bad==wantBad);}
                Test(item!=null,"available physical item");Test(GrabWorkItem(item.id),"grab item "+item.id);
                if(item.kind=="pear")Test(RotateWorkItem(180),"rotate inspected pear");
                Test(PlaceWorkItem(work.mode==HandsWork.Mode.Inspect&&item.bad?"bad":"target"),"place actual item");
            }
            Test(work==null,"physical work terminates");
        }
        void TestHandsOn()
        {
            state=new DayState();state.PrepareChapter(3);StartChapter(3);int before=state.coins,stock=state.stock;
            DoAction("gu_good");Test(work!=null&&state.step==0&&state.coins==before,"choice opens physical work without a sale");
            var bad=work.items.Find(i=>i.bad);Test(GrabWorkItem(bad.id),"grab bruised pear");Test(RotateWorkItem(180),"inspect underside");Test(!PlaceWorkItem("target"),"NPC rejects bruised pear from good order");Test(state.coins==before&&state.stock==stock,"rejection has no accounting side effects");ReturnWorkItem();
            Test(!ConfirmWork(),"early handover blocked");CompleteWorkForTest();Test(state.coins==before+12&&state.stock==stock-3,"actual sale commits once");Test(!ConfirmWork(),"no double settlement");
            state=new DayState();state.PrepareChapter(1);StartChapter(1);DoAction("inspect");Test(work!=null,"inspection bench");Test(GrabWorkItem(0),"inspection pickup");Test(!PlaceWorkItem("target"),"must turn fruit before grading");ReturnWorkItem();CompleteWorkForTest();
            DoAction("batch_keep");Test(work.mode==HandsWork.Mode.Money,"count payment");Test(!ConfirmWork(),"underpayment rejected");
            foreach(var i in work.items){if(work.Sum>work.needed)break;Test(GrabWorkItem(i.id),"count coins");Test(PlaceWorkItem("target"),"put coin on tray");}
            Test(work.Sum>work.needed&&!ConfirmWork(),"excess payment rejected");int moneyBefore=state.coins;CancelWork();Test(state.coins==moneyBefore&&state.stock==0,"cancel escrow restores full ownership");
            DoAction("batch_keep");CompleteWorkForTest();Test(state.coins==32&&state.stock==24,"exact payment commits batch");DoAction("reserve");CompleteWorkForTest();Test(state.stock==24&&state.reserved==12,"reservation does not duplicate goods");
            state=new DayState();state.PrepareChapter(6);StartChapter(6);DoAction("walk");CheckDeliveryProgress();Test(work.waypoint==0,"no route credit without basket");Test(GrabWorkItem(0),"carry pickup");Test(!PlaceWorkItem("target"),"cannot deliver remotely");ReturnWorkItem();CompleteWorkForTest();Test(state.chapter==7&&state.route=="沿岸步行","walking delivers to actual tea scene");
            DoAction("deliver");CompleteWorkForTest();Test(state.accepted==12,"twelve physical pears accepted");
            state=new DayState{sandbox=true};StartChapter(0);Vector3 origin=world.player.localPosition;WalkStep(new Vector3(1,0,0));Test(world.player.localPosition.x>origin.x+.5f,"first-person movement changes location");
            WalkStep(new Vector3(100,0,0));Test(world.player.localPosition.x<=sceneAnchor.x+5.01f,"walkable region respected");
            Debug.Log("QINGMING_HANDS_PASS: real pickup/rotation/drop; NPC quality rejection; money escrow, under/overpayment and cancellation; reservation conservation; physical delivery checkpoints; walking bounds; commit once.");
        }
        IEnumerator CaptureHands()
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../captures/hands-on"));Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(2);
            state=new DayState();state.PrepareChapter(1);StartChapter(1);DoAction("inspect");
            yield return new WaitForSecondsRealtime(2);yield return CaptureFrame(folder,"01_可走动的验货台");
            GrabWorkItem(23);RotateWorkItem(180);yield return new WaitForSecondsRealtime(1);yield return CaptureFrame(folder,"02_翻面看碰伤");ReturnWorkItem();CompleteWorkForTest();
            DoAction("batch_keep");for(int i=0;i<4;i++){GrabWorkItem(i);PlaceWorkItem("target");}
            yield return new WaitForSecondsRealtime(1);yield return CaptureFrame(folder,"03_亲手数钱");CompleteWorkForTest();
            state=new DayState();state.PrepareChapter(3);StartChapter(3);DoAction("gu_good");GrabWorkItem(work.items.Find(i=>i.bad).id);RotateWorkItem(180);PlaceWorkItem("target");
            yield return new WaitForSecondsRealtime(.25f);yield return CaptureFrame(folder,"04_顾嫂拒收轻伤梨");CancelWork(false);
            state=new DayState();state.PrepareChapter(6);StartChapter(6);DoAction("walk");GrabWorkItem(0);look.x=-90;
            yield return new WaitForSecondsRealtime(1.4f);yield return CaptureFrame(folder,"05_提篓沿街送货");CompleteWorkForTest();DoAction("deliver");
            Test(work!=null&&work.action=="deliver","capture enters tea handover");for(int i=0;i<4;i++){Test(GrabWorkItem(i),"capture grabs tea pear");RotateWorkItem(180);Test(PlaceWorkItem("target"),"capture stages tea pear");}
            yield return new WaitForSecondsRealtime(1);yield return CaptureFrame(folder,"06_茶坊逐只交货");CompleteWorkForTest();
            Debug.Log("QINGMING_HANDS_CAPTURE_PASS");SetMenu();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-quit-after-capture")>=0)Application.Quit();
        }
    }
}
