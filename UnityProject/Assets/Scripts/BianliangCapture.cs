using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Qingming
{
    public partial class BianliangDemo
    {
        IEnumerator CaptureViews()
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../captures/aan-story"));
            Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(3.6f);
            yield return CaptureFrame(folder,"01_原画长卷");
            var entry=StartCoroutine(EnterPainting(true));
            yield return new WaitForSecondsRealtime(.95f);
            yield return CaptureFrame(folder,"01a_绢色入墨");
            yield return new WaitForSecondsRealtime(.6f);
            yield return CaptureFrame(folder,"01b_墨中生景");
            yield return entry;
            Test(!transitioning&&paintingOpacity==0&&atmosphere.pigment==1,"painting transition completes");
            Test(!atmosphere.IsTinting,"original scene colours restored without wash");
            Test(state.sandbox&&playing&&state.chapter==0,"transition preview preserves saves");
            Debug.Log("QINGMING_STYLE_PASS: transition completes, pigment restored, preview save isolation.");
            yield return new WaitForSecondsRealtime(3.6f);
            yield return CaptureFrame(folder,"02_入画街景");
            SetView(1);interactionOpen=true;
            yield return new WaitForSecondsRealtime(3.6f);
            yield return CaptureFrame(folder,"03_安母家门");
            foreach(int chapter in new[]{1,3,4,7,8,10,11,12,13}){
                state=new DayState();state.PrepareChapter(chapter);StartChapter(chapter);SetView(1);
                if(chapter==4)DoAction("money_count");
                if(chapter==11){DoAction("show_pay");DoAction("axiu_quiet");}
                yield return new WaitForSecondsRealtime(1.2f);
                yield return CaptureFrame(folder,$"S{chapter+1:00}_"+DayState.Chapters[chapter]);
            }
            journalTab=0;Action("journal");yield return CaptureFrame(folder,"15_钱货账");Action("resume");
            state=Route();StartChapter(13);yield return new WaitForSecondsRealtime(.8f);yield return CaptureFrame(folder,"16_结局甲");
            Action("chapters");yield return CaptureFrame(folder,"17_十四场重游");
            SetMenu();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-quit-after-capture")>=0)Application.Quit();
        }
        IEnumerator CaptureFrame(string folder,string name)
        {
            yield return new WaitForEndOfFrame();
            string path=Path.Combine(folder,name+".png");
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(.6f);
            Debug.Log("QINGMING_CAPTURE "+path);
        }
    }
}
