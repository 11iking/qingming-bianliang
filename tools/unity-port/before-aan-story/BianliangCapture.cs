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
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../captures/unity"));
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
            Test(state.sandbox&&playing&&state.chapter==0,"transition preview preserves saves");
            Debug.Log("QINGMING_STYLE_PASS: transition completes, pigment restored, preview save isolation.");
            yield return new WaitForSecondsRealtime(3.6f);
            yield return CaptureFrame(folder,"02_入画街景");
            SetView(1);interactionOpen=true;
            yield return new WaitForSecondsRealtime(3.6f);
            yield return CaptureFrame(folder,"03_梨摊交互");
            state=new DayState();state.PrepareChapter(5);StartChapter(5);SetView(1);
            yield return new WaitForSecondsRealtime(3.6f);
            yield return CaptureFrame(folder,"04_夜间酒肆");
            state=new DayState();state.PrepareChapter(6);StartChapter(6);SetView(1);
            yield return new WaitForSecondsRealtime(3.6f);
            yield return CaptureFrame(folder,"05_勾栏听曲");
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
