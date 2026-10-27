using UnityEngine;
namespace Qingming
{
    public class DemoSound:MonoBehaviour
    {
        AudioSource music,fx;
        public bool muted;
        void Start()
        {
            music=gameObject.AddComponent<AudioSource>();fx=gameObject.AddComponent<AudioSource>();
            int rate=22050;var data=new float[rate*24];float prev=0;
            float[] notes={220,293.66f,329.63f,440,493.88f,440,329.63f,293.66f};
            var rng=new System.Random(2026);
            for(int i=0;i<data.Length;i++){
                float t=(float)i/rate,nt=t%3,f=notes[(int)(t/3)%8];
                float envelope=Mathf.Exp(-nt*2.2f)*Mathf.Min(nt*30,1);
                float pluck=(Mathf.Sin(2*Mathf.PI*f*t)+.35f*Mathf.Sin(2*Mathf.PI*f*2.01f*t)+.14f*Mathf.Sin(2*Mathf.PI*f*3*t))*envelope*.13f;
                prev=Mathf.Lerp(prev,(float)rng.NextDouble()*2-1,.025f);
                float bird=Mathf.Sin(2*Mathf.PI*(1450+Mathf.Sin(t*18)*230)*t)*Mathf.Max(0,Mathf.Sin(t*.8f-1.5f)-.93f)*.3f;
                data[i]=Mathf.Clamp(pluck+prev*.17f+bird,-1,1);
            }
            music.clip=AudioClip.Create("Bianliang original ambience",data.Length,1,rate,false);music.clip.SetData(data,0);
            music.loop=true;music.volume=.126f;fx.volume=.224f;
            music.Play();
        }
        public void Toggle(){muted=!muted;if(music)music.mute=muted;}
        public void Tone(string kind="click")
        {
            if(muted||!fx)return;
            int rate=22050;float seconds=kind=="chapter"?.7f:.35f;
            float frequency=kind=="coin"?1108:kind=="water"?180:kind=="toast"?1568:kind=="chapter"?440:660;
            var data=new float[(int)(seconds*rate)];
            for(int i=0;i<data.Length;i++){
                float t=(float)i/rate;
                float v=Mathf.Sin(2*Mathf.PI*frequency*t)*Mathf.Exp(-t*12)*.35f;
                if(kind=="water")v=Mathf.Sin(2*Mathf.PI*(frequency-t*150)*t)*Mathf.Exp(-t*9)*.25f;
                if(kind=="toast")v+=Mathf.Sin(2*Mathf.PI*frequency*2.7f*t)*Mathf.Exp(-t*22)*.12f;
                data[i]=v;
            }
            var clip=AudioClip.Create("Interaction_"+kind,data.Length,1,rate,false);clip.SetData(data,0);fx.PlayOneShot(clip);Destroy(clip,seconds+1);
        }
    }
}
