using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    /// <summary>Saved title-only composition. Gameplay panels retain their compact authored geometry.</summary>
    [DisallowMultipleComponent]
    public sealed class TitleMenuLayout : MonoBehaviour
    {
        public PresentationSurface Surface;
        public RectTransform Plaque,Crest,ViewBorder,MenuShade;
        public Text Subtitle;
        public NativeRow PresenterRow;
        public RectTransform Companion;
        public Image CompanionPortrait;
        public Text CompanionIdentity,CompanionStatus;
        public Button CompanionHelp;
        public NativeEnglishStringTable Catalogue;
        public float MenuWidth=462;
        public void RefreshLabels(){Subtitle.text=Catalogue.Get("title.subtitle");CompanionIdentity.text=Catalogue.Get("title.leena.identity");CompanionStatus.text=Catalogue.Get("title.leena.status");}
        public void Apply(float scale)
        {
            var owner=Surface.GetComponentInParent<Canvas>();var canvas=owner?(RectTransform)owner.transform:null;float width=canvas?canvas.rect.width:1280,height=canvas?canvas.rect.height:720;
            bool large=scale>=1.5f;float menuWidth=large?Mathf.Min(620,width*.53f):MenuWidth;
            var frame=Surface.Frame;Stretch(frame);Stretch(ViewBorder,10);
            Place(MenuShade,22,22,large?menuWidth+40:menuWidth+68,height-44);
            float left=large?38:52,top=large?40:112;
            Place(Plaque,left,top,menuWidth,large?176:142);Place(Crest,left+menuWidth*.5f-55,top+12,110,44);
            var heading=Surface.Heading;heading.color=NativeUiPalette.Ink;heading.alignment=TextAnchor.MiddleCenter;heading.fontSize=large?Mathf.RoundToInt(24*scale):Mathf.RoundToInt(30*scale);heading.horizontalOverflow=HorizontalWrapMode.Wrap;heading.verticalOverflow=VerticalWrapMode.Truncate;
            Place(heading.rectTransform,left+18,top+50,menuWidth-36,large?110:58);
            Subtitle.fontSize=large?Mathf.RoundToInt(14*scale):Mathf.RoundToInt(16*scale);Subtitle.alignment=TextAnchor.MiddleCenter;
            Place(Subtitle.rectTransform,left,top+(large?190:156),menuWidth,large?84:52);
            float actionTop=top+(large?286:224),buttonHeight=large?54:46,gap=large?10:10;
            float actionWidth=menuWidth-(large?8:72),actionLeft=left+(menuWidth-actionWidth)*.5f;
            var buttons=new[]{Surface.Primary,PresenterRow.Action,Surface.Secondary,Surface.Tertiary};
            for(int i=0;i<buttons.Length;i++)
            {
                var b=buttons[i];var rt=(RectTransform)b.transform;
                if(b==PresenterRow.Action){Place((RectTransform)PresenterRow.transform,actionLeft,actionTop+i*(buttonHeight+gap),actionWidth,buttonHeight);Stretch(rt);}
                else Place(rt,actionLeft,actionTop+i*(buttonHeight+gap),actionWidth,buttonHeight);
                var label=b.GetComponentInChildren<Text>();label.fontSize=large?Mathf.RoundToInt(15*scale):Mathf.RoundToInt(18*scale);label.alignment=TextAnchor.MiddleCenter;Stretch(label.rectTransform,6);
                foreach(var icon in b.GetComponentsInChildren<NativeIcon>(true))icon.gameObject.SetActive(false);
            }
            Surface.Status.fontSize=large?Mathf.RoundToInt(10*scale):Mathf.RoundToInt(12*scale);Surface.Status.alignment=TextAnchor.MiddleCenter;Surface.Status.color=new Color(1,.97f,.88f);
            Place(Surface.Status.rectTransform,left,height-(large?104:104),menuWidth,large?44:28);
            if(large)
            {Place(Companion,width-354,height-128,326,96);CompanionIdentity.fontSize=22;CompanionStatus.fontSize=18;Place(CompanionIdentity.rectTransform,76,10,244,35);Place(CompanionStatus.rectTransform,76,45,244,46);}
            else
            {Place(Companion,left+58,height-78,menuWidth-116,58);CompanionIdentity.fontSize=16;CompanionStatus.fontSize=13;Place(CompanionIdentity.rectTransform,62,6,menuWidth-190,22);Place(CompanionStatus.rectTransform,62,30,menuWidth-190,20);}
            Place(CompanionPortrait.rectTransform,8,5,large?60:44,large?86:48);
            Surface.Scroll.gameObject.SetActive(false);Surface.Paper.gameObject.SetActive(false);Surface.Icon.gameObject.SetActive(false);Surface.Close.gameObject.SetActive(false);
            Surface.Primary.transform.SetAsLastSibling();PresenterRow.transform.SetAsLastSibling();Surface.Secondary.transform.SetAsLastSibling();Surface.Tertiary.transform.SetAsLastSibling();Companion.SetAsLastSibling();
        }
        public static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        public static void Stretch(RectTransform r,float inset=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(inset,inset);r.offsetMax=new Vector2(-inset,-inset);}
    }
}
