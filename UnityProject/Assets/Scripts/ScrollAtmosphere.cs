using UnityEngine;

namespace Qingming
{
    // Applied before IMGUI: the original painting and readable interface stay untouched.
    [RequireComponent(typeof(Camera))]
    public sealed class ScrollAtmosphere : MonoBehaviour
    {
        [Range(0,1)] public float pigment=1;
        public bool night;
        public bool IsTinting=>pigment<1;
        Material wash;
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(!IsTinting){Graphics.Blit(source,destination);return;}
            if(!wash){
                var shader=Resources.Load<Shader>("ScrollWash");
                if(!shader||!shader.isSupported){Graphics.Blit(source,destination);return;}
                wash=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            }
            wash.SetFloat("_Pigment",pigment);
            wash.SetFloat("_Night",night?1:0);
            Graphics.Blit(source,destination,wash);
        }
        void OnDestroy(){if(wash)Destroy(wash);}
    }
}
