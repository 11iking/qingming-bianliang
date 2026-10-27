using System.Collections.Generic;
namespace Qingming
{
    public partial class BianliangDemo
    {
        public class Choice {public string id,text;public bool disabled;public Choice(string i,string t,bool d=false){id=i;text=t;disabled=d;}}
        public List<Choice> Actions(){var r=new List<Choice>();foreach(var a in state.Options())r.Add(new Choice(a.id,a.text,a.disabled));return r;}
        public string Speaker()=>state.Speaker();
        public string Narrative()=>state.Line();
    }
}
