using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Qingming
{
    public partial class BianliangDemo
    {
        IEnumerator CaptureEnvironment()
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../captures/street-environment"));
            Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(2);
            state=new DayState();state.PrepareChapter(3);StartChapter(3);SetView(3);
            var street=world.GetComponent<StreetEnvironment>();
            Test(street&&street.DetailCount>4000&&street.BatchCount<=20,"street details generated and batched");
            paused=true;panel="street-preview";toastUntil=0;
            yield return EnvironmentShot(folder,"01_街区全景",G(-.2f,17.5f,32),G(-3,1.2f,8));
            yield return EnvironmentShot(folder,"02_沿街铺地与店面",G(-28,2.25f,10),G(-3,1.65f,13));
            yield return EnvironmentShot(folder,"03_安家门前",G(-25,2.1f,10.8f),G(-25,1.5f,15));
            yield return EnvironmentShot(folder,"04_河岸与码头",G(-28,3.6f,2.2f),G(-5,.7f,5.1f));
            world.SetTime(5);atmosphere.night=true;cam.backgroundColor=Hex("36484e");
            yield return EnvironmentShot(folder,"05_暮色街巷",G(6,3.2f,15),G(-14,2,12));
            Debug.Log("QINGMING_ENVIRONMENT_PASS: detail batching, five street viewpoints; original colour output="+!atmosphere.IsTinting);
            SetMenu();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-quit-after-capture")>=0)Application.Quit();
        }
        IEnumerator EnvironmentShot(string folder,string name,Vector3 position,Vector3 target)
        {
            cam.fieldOfView=60;cam.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
            yield return new WaitForSecondsRealtime(1.8f);
            yield return CaptureFrame(folder,name);
        }
    }
}
