using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Qingming
{
    public partial class BianliangDemo
    {
        IEnumerator CapturePortraits()
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../captures/portraits"));
            Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(1);
            TestPortraits();
            foreach(int chapter in new[]{0,3,7,8,11}){
                state=new DayState();state.PrepareChapter(chapter);
                if(chapter==8)state.step=2;
                if(chapter==11)state.step=3;
                StartChapter(chapter);interactionOpen=true;portraitBeat="";
                yield return new WaitForSecondsRealtime(1.2f);
                yield return CaptureFrame(folder,$"{chapter:00}_人物对话");
            }
            state=new DayState();state.PrepareChapter(1);StartChapter(1);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return CaptureFrame(folder,"14_行走对话");
            DoAction("inspect");
            yield return new WaitForSecondsRealtime(1.2f);
            yield return CaptureFrame(folder,"15_操作回应");
            Debug.Log("QINGMING_PORTRAIT_CAPTURE_PASS: dialogue, multiple speakers, exploration and hands layouts captured.");
            SetMenu();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-quit-after-capture")>=0)Application.Quit();
        }
    }
}
