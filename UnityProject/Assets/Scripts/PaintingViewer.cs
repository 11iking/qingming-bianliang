using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Qingming
{
    public class PaintingViewer : IDisposable
    {
        public const float Width=38414,Height=1800;
        public static readonly Rect Detail=new Rect(54,104,1332,700),Overview=new Rect(322,838,796,37.3f);
        public Vector2 focus=new Vector2(19550,900);
        public float zoom=700f/1800f;
        public float MinZoom=>Detail.width/Width;
        public Texture2D overview;
        readonly Dictionary<int,Texture2D> tiles=new Dictionary<int,Texture2D>();
        bool dragging,overviewDrag;
        Vector2 last;
        float lastClick=-1;
        public void Load(){overview=Read("overview.jpg");Refresh();}
        Texture2D Read(string file){
            var tex=new Texture2D(2,2,TextureFormat.RGB24,false);
            if(!tex.LoadImage(File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath,"Painting",file)),true))throw new IOException("Cannot decode "+file);
            tex.filterMode=FilterMode.Bilinear;tex.wrapMode=TextureWrapMode.Clamp;tex.name=file;return tex;
        }
        public Vector2 Origin=>Detail.center-focus*zoom;
        public Rect Visible=>new Rect((Detail.position-Origin)/zoom,Detail.size/zoom);
        public void Refresh()
        {
            zoom=Mathf.Clamp(zoom,MinZoom,1.5f);
            var half=Detail.size/(2*zoom);
            focus.x=half.x>=Width*.5f?Width*.5f:Mathf.Clamp(focus.x,half.x,Width-half.x);
            focus.y=half.y>=Height*.5f?Height*.5f:Mathf.Clamp(focus.y,half.y,Height-half.y);
            if(zoom<=.07f)return;
            int first=Mathf.Clamp(Mathf.FloorToInt(Visible.xMin/2048),0,18),lastIndex=Mathf.Clamp(Mathf.FloorToInt(Visible.xMax/2048),0,18);
            for(int i=first;i<=lastIndex;i++)if(!tiles.ContainsKey(i))tiles[i]=Read("tile_"+i.ToString("00")+".png");
            var keys=new List<int>(tiles.Keys);foreach(int i in keys)if(i<first-1||i>lastIndex+1){UnityEngine.Object.Destroy(tiles[i]);tiles.Remove(i);}
        }
        public void ZoomAt(float value,Vector2 pivot)
        {
            var pixel=(pivot-Origin)/zoom;zoom=Mathf.Clamp(value,MinZoom,1.5f);focus=pixel-(pivot-Detail.center)/zoom;Refresh();
        }
        public void Full(){focus=new Vector2(Width/2,Height/2);zoom=MinZoom;Refresh();}
        public void Bridge(){focus=new Vector2(19550,900);zoom=700f/1800;Refresh();}
        public void UpdateInput(Vector2 mouse,float scale)
        {
            if(Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1)){
                dragging=Detail.Contains(mouse);overviewDrag=Overview.Contains(mouse);last=mouse;
                if(dragging&&Time.unscaledTime-lastClick<.3f)ZoomAt(zoom<.8f?1:700f/1800,mouse);
                lastClick=Time.unscaledTime;
            }
            if(Input.GetMouseButtonUp(0)||Input.GetMouseButtonUp(1)){dragging=false;overviewDrag=false;}
            if(dragging&&(Input.GetMouseButton(0)||Input.GetMouseButton(1))){focus-=(mouse-last)/zoom;last=mouse;Refresh();}
            if(overviewDrag){focus.x=Mathf.Clamp01((mouse.x-Overview.x)/Overview.width)*Width;Refresh();}
            if(Input.mouseScrollDelta.y!=0)ZoomAt(zoom*Mathf.Pow(1.24f,Input.mouseScrollDelta.y),Detail.Contains(mouse)?mouse:Detail.center);
            float direction=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);
            if(direction!=0){focus.x+=direction*Time.deltaTime*700/zoom;Refresh();}
        }
        public void Draw(float opacity=1,bool showOverview=true)
        {
            if(opacity<=0)return;
            Color old=GUI.color;
            // One alpha layer: drawing an alpha background beneath the tiles would
            // brighten the dissolve and recreate the old paper flash.
            if(opacity>=1){GUI.color=new Color(.78f,.72f,.58f);GUI.DrawTexture(Detail,Texture2D.whiteTexture);}
            GUI.color=new Color(1,1,1,opacity);
            GUI.BeginGroup(Detail);
            Vector2 localOrigin=Origin-Detail.position;
            if(zoom<=.07f)GUI.DrawTexture(new Rect(localOrigin,new Vector2(Width,Height)*zoom),overview,ScaleMode.StretchToFill);
            else foreach(var entry in tiles){
                float tileWidth=Mathf.Min(2048,Width-entry.Key*2048);
                GUI.DrawTexture(new Rect(localOrigin+new Vector2(entry.Key*2048*zoom,0),new Vector2(tileWidth,Height)*zoom),entry.Value,ScaleMode.StretchToFill);
            }
            GUI.EndGroup();
            if(!showOverview){GUI.color=old;return;}
            GUI.DrawTexture(Overview,overview,ScaleMode.StretchToFill);
            Rect v=Visible;float x=Overview.x+Mathf.Max(0,v.x)*Overview.width/Width;
            float w=Mathf.Min(Width,v.width)*Overview.width/Width;
            GUI.color=new Color(.58f,.24f,.18f);GUI.DrawTexture(new Rect(x,Overview.y,w,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x,Overview.yMax-2,w,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x,Overview.y,2,Overview.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x+w-2,Overview.y,2,Overview.height),Texture2D.whiteTexture);GUI.color=old;
        }
        public void Dispose(){foreach(var texture in tiles.Values)UnityEngine.Object.Destroy(texture);if(overview)UnityEngine.Object.Destroy(overview);}
    }
}
