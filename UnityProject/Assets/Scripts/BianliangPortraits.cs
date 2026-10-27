using System.Collections.Generic;
using UnityEngine;
namespace Qingming
{
    public partial class BianliangDemo
    {
        static readonly string[] PortraitIds={"aan","anmu","vendor","gusao","shen","hesi","laozhou","xuniang","axiu","partner","patrol","repair","boatcrew"};
        static readonly string[] PortraitNames={"阿安","安母","果行卖者","顾嫂","沈客","何四","老周","许娘子","阿绣","搭档","铺兵","修篓人","船工"};
        static readonly string[][] PortraitCast={
            new[]{"anmu","aan"},new[]{"vendor","aan"},new[]{"gusao","laozhou","aan"},new[]{"gusao","aan"},new[]{"shen","aan"},
            new[]{"hesi","boatcrew","aan"},new[]{"laozhou","hesi","aan"},new[]{"xuniang","aan"},new[]{"gusao","shen","repair","aan"},
            new[]{"aan"},new[]{"laozhou","hesi","aan"},new[]{"axiu","partner","aan"},new[]{"patrol","gusao","aan"},new[]{"anmu","aan"}};
        readonly Dictionary<string,Texture2D> portraitTextures=new Dictionary<string,Texture2D>();
        string portraitSelection="",portraitBeat="";
        Texture2D PortraitTexture(string id)
        {
            if(!portraitTextures.TryGetValue(id,out var texture)){texture=Resources.Load<Texture2D>("Portraits/"+id);portraitTextures[id]=texture;}
            return texture;
        }
        string PortraitName(string id){int i=System.Array.IndexOf(PortraitIds,id);return i<0?"":PortraitNames[i];}
        string PrimaryPortrait()
        {
            switch(state.chapter){
                case 0:case 13:return "anmu";
                case 1:return "vendor";
                case 2:return state.step==2?"laozhou":"gusao";
                case 3:return "gusao";
                case 4:return "shen";
                case 5:return "hesi";
                case 6:return state.porterAgreed?"laozhou":state.rodeBoat?"hesi":"aan";
                case 7:return "xuniang";
                case 8:return state.step==2?"repair":state.step==1&&state.teaDeferred?"shen":"gusao";
                case 9:return "aan";
                case 10:return state.step==3&&state.bridge_helped?"hesi":"laozhou";
                case 11:return state.step==3||state.step==4?"partner":"axiu";
                case 12:return state.step==0?"patrol":"gusao";
                default:return "aan";
            }
        }
        void DrawPortrait(string id,Rect rect)
        {
            var texture=PortraitTexture(id);if(!texture)return;
            Color old=GUI.color;GUI.color=Color.white;
            GUI.DrawTexture(rect,texture,ScaleMode.ScaleToFit,true);GUI.color=old;
        }
        void DrawDialoguePortraits()
        {
            string beat=state.chapter+":"+state.step;
            if(beat!=portraitBeat){portraitBeat=beat;portraitSelection=PrimaryPortrait();}
            Fill(new Rect(91,445,204,346),Hex("ded2b7"));
            DrawPortrait(portraitSelection,new Rect(94,447,198,297));
            Label(PortraitName(portraitSelection),new Rect(99,751,188,32),22,Ink,true);
            var cast=PortraitCast[state.chapter];
            for(int i=0;i<cast.Length;i++)if(Button(PortraitName(cast[i]),new Rect(311+i*137,510,128,31),cast[i]==portraitSelection,false,14))portraitSelection=cast[i];
        }
        void TestPortraits()
        {
            foreach(string id in PortraitIds)Test(PortraitTexture(id)!=null,"portrait included: "+id);
            foreach(var cast in PortraitCast)foreach(string id in cast)Test(System.Array.IndexOf(PortraitIds,id)>=0,"portrait cast mapping: "+id);
            Debug.Log("QINGMING_PORTRAITS_PASS: 13 individual portraits and all 14 scene casts available.");
        }
    }
}
