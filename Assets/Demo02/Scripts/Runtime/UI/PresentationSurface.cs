using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    public sealed class PresentationSurface : MonoBehaviour
    {
        public NativeUiStyle Style;
        public string SurfaceId;
        public RectTransform Frame,Paper,Sidebar;
        public Text Heading,Status,SideText;
        public NativeIcon Icon;
        public NativeRowPool Rows;
        public ScrollRect Scroll;
        public Button Primary,Secondary,Tertiary,Close;
        public RectTransform Tabs;
        public Button[] TabButtons;
        public InputField Composer;
        public NativeRow ComposerDraft;
        public RectTransform ComposerFrame;
        public Button Send,CancelRequest;
        public KeyboardFocus Focus;
        public bool Wide;
        public bool RoomComposition;
        public bool TitleComposition;
        public TitleMenuLayout TitleLayout;
        public NativeSettingRow[] SettingRows;
        public Slider MasterSlider;public Text MasterValue;
        public Toggle SilentToggle,MotionToggle,ContrastToggle;public Dropdown TextSizeDropdown;
        public ulong BindingGeneration { get; private set; }
        public void BeginBinding()=>BindingGeneration++;
        float appliedScale=1,lastWidth=-1,lastHeight=-1;
        void LateUpdate()
        {
            if(!Frame||!Frame.gameObject.activeInHierarchy)return;
            var canvas=Frame.GetComponentInParent<Canvas>();if(!canvas)return;var viewport=(RectTransform)canvas.transform;
            if(Mathf.Abs(viewport.rect.width-lastWidth)>1||Mathf.Abs(viewport.rect.height-lastHeight)>1)ApplyScale(appliedScale);
        }
        public void ApplyScale(float scale)
        {
            if(TitleLayout){appliedScale=scale;TitleLayout.Apply(scale);var titleCanvas=Frame.GetComponentInParent<Canvas>();if(titleCanvas){lastWidth=((RectTransform)titleCanvas.transform).rect.width;lastHeight=((RectTransform)titleCanvas.transform).rect.height;}return;}
            bool large=scale>=1.5f;var canvas=Frame.GetComponentInParent<Canvas>();var viewport=canvas?(RectTransform)canvas.transform:null;
            float width=viewport?viewport.rect.width:1280,height=viewport?viewport.rect.height:720;appliedScale=scale;lastWidth=width;lastHeight=height;
            bool shortPanel=SurfaceId=="pause"||SurfaceId=="confirmation"||SurfaceId=="loading"||SurfaceId=="title"||SurfaceId=="conversation"||SurfaceId=="settings";
            bool loading=SurfaceId=="loading";
            Frame.anchorMin=Frame.anchorMax=Frame.pivot=new Vector2(1,1);Frame.anchoredPosition=new Vector2(-14,large?-12:-64);
            Frame.sizeDelta=new Vector2(loading?(large?Mathf.Min(width-28,680):(Style?Style.PanelWidth:430)):(large?width-28:(Style?Style.PanelWidth:430)),loading?(large?Mathf.Min(height-126,390):Mathf.Min(245,height-150)):(large?height-126:Mathf.Min(shortPanel?(Style?Style.ShortPanelHeight:390):(Style?Style.PanelHeight:500),height-150)));
            Heading.fontSize=Mathf.RoundToInt((TitleComposition?(Style?Style.TitleFont:24):(Style?Style.HeadingFont:20))*scale);Status.fontSize=Mathf.RoundToInt((Style?Style.SecondaryFont:12)*scale);
            Heading.alignment=TextAnchor.MiddleLeft;Heading.color=new Color(1,.97f,.87f);
            Heading.rectTransform.offsetMin=new Vector2(loading?12+22*scale+12:(large?52:42),large?-62:-38);Heading.rectTransform.offsetMax=new Vector2(large?-100:-72,-6);
            Paper.offsetMin=new Vector2(4,4);Paper.offsetMax=new Vector2(-4,large?-70:-44);
            var close=(RectTransform)Close.transform;close.sizeDelta=new Vector2(large?88:60,30*scale);close.anchoredPosition=new Vector2(-8,-7);var closeLabel=Close.GetComponentInChildren<Text>();closeLabel.fontSize=Mathf.RoundToInt((Style?Style.ControlFont:13)*scale);closeLabel.rectTransform.offsetMin=Vector2.zero;closeLabel.rectTransform.offsetMax=Vector2.zero;
            Icon.color=NativeUiPalette.Gold;Icon.rectTransform.anchorMin=Icon.rectTransform.anchorMax=new Vector2(0,1);Icon.rectTransform.pivot=new Vector2(0,1);Icon.rectTransform.anchoredPosition=new Vector2(12,-12);Icon.rectTransform.sizeDelta=new Vector2(22*scale,22*scale);
            float header=large?70:44,tabsHeight=Tabs&&Tabs.gameObject.activeSelf?34*scale:0;
            if(Tabs){Tabs.offsetMin=new Vector2(10,-header-tabsHeight);Tabs.offsetMax=new Vector2(-10,-header);}
            if(TabButtons!=null)foreach(var b in TabButtons)if(b){b.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt((Style?Style.SecondaryFont:12)*scale);var t=b.GetComponentInChildren<Text>().rectTransform;t.offsetMin=new Vector2(2,0);t.offsetMax=new Vector2(-2,0);}
            bool hasFooter=Primary.gameObject.activeSelf||Secondary.gameObject.activeSelf||Tertiary.gameObject.activeSelf;
            float footer=hasFooter?42*scale:8,status=string.IsNullOrEmpty(Status.text)?8:Mathf.Clamp(Status.preferredHeight+12,40*scale,88*scale),composer=Composer?96*scale:0;
            var sr=(RectTransform)Scroll.transform;sr.offsetMin=new Vector2(12,footer+status+composer);sr.offsetMax=new Vector2(-12,-header-tabsHeight-8);
            Status.alignment=TextAnchor.UpperLeft;Status.verticalOverflow=VerticalWrapMode.Truncate;Status.rectTransform.anchorMin=Vector2.zero;Status.rectTransform.anchorMax=new Vector2(1,0);Status.rectTransform.offsetMin=new Vector2(12,footer);Status.rectTransform.offsetMax=new Vector2(-12,footer+status-4);
            if(Sidebar)Sidebar.gameObject.SetActive(false);
            var buttons=new[]{Primary,Secondary,Tertiary};int count=0;foreach(var b in buttons)if(b&&b.gameObject.activeSelf)count++;
            int index=0;foreach(var b in buttons)if(b&&b.gameObject.activeSelf)
            {var r=(RectTransform)b.transform;r.anchorMin=new Vector2((float)index/count,0);r.anchorMax=new Vector2((float)(index+1)/count,0);r.offsetMin=new Vector2(index==0?12:4,8);r.offsetMax=new Vector2(index==count-1?-12:-4,footer-2);b.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt((Style?Style.ControlFont:13)*scale);index++;}
            if(Composer)
            {
                ComposerFrame.offsetMin=new Vector2(12,footer+status);ComposerFrame.offsetMax=new Vector2(-12,footer+status+composer-6);
                var input=(RectTransform)Composer.transform;input.anchorMin=Vector2.zero;input.anchorMax=Vector2.one;input.offsetMin=new Vector2(0,36*scale);input.offsetMax=Vector2.zero;
                Composer.textComponent.fontSize=Mathf.RoundToInt((Style?Style.BodyFont:14)*scale);if(Composer.placeholder is Text p){p.fontSize=Mathf.RoundToInt((Style?Style.BodyFont:14)*scale);p.rectTransform.offsetMin=new Vector2(8,6);p.rectTransform.offsetMax=new Vector2(-8,-6);p.color=new Color(.23f,.26f,.28f);}Composer.textComponent.rectTransform.offsetMin=new Vector2(8,6);Composer.textComponent.rectTransform.offsetMax=new Vector2(-8,-6);
                if(ComposerDraft&&ComposerDraft.Caption){ComposerDraft.Caption.gameObject.SetActive(true);}
                var send=(RectTransform)Send.transform;send.anchorMin=new Vector2(.64f,0);send.anchorMax=new Vector2(1,0);send.offsetMin=new Vector2(4,0);send.offsetMax=new Vector2(0,32*scale);
                var cancel=(RectTransform)CancelRequest.transform;cancel.anchorMin=Vector2.zero;cancel.anchorMax=new Vector2(.64f,0);cancel.offsetMin=Vector2.zero;cancel.offsetMax=new Vector2(-4,32*scale);
                Send.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt((Style?Style.ControlFont:13)*scale);CancelRequest.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt((Style?Style.SecondaryFont:12)*scale);
            }
            foreach(var row in Rows.GetComponentsInChildren<NativeRow>(true))row.ApplyScale(scale);
            if(SettingRows!=null)foreach(var setting in SettingRows)setting.ApplyScale(scale);
        }
    }
    [System.Serializable]public sealed class LocalPresentationPreferences
    {
        public float Master=.8f;public bool Silent;public float TextScale=1;
        public bool ReducedMotion;public bool HighContrast;
        public LocalPresentationPreferences Copy()=>new LocalPresentationPreferences{Master=Master,Silent=Silent,TextScale=TextScale,ReducedMotion=ReducedMotion,HighContrast=HighContrast};
        public static LocalPresentationPreferences Load()=>new LocalPresentationPreferences{Master=PlayerPrefs.GetFloat("D02.preference.master",.8f),Silent=PlayerPrefs.GetInt("D02.preference.silent",0)==1,TextScale=Mathf.Clamp(PlayerPrefs.GetFloat("D02.preference.text",1),1,2),ReducedMotion=PlayerPrefs.GetInt("D02.preference.reducedMotion",0)==1,HighContrast=PlayerPrefs.GetInt("D02.preference.contrast",0)==1};
        public void Save()
        {PlayerPrefs.SetFloat("D02.preference.master",Master);PlayerPrefs.SetInt("D02.preference.silent",Silent?1:0);PlayerPrefs.SetFloat("D02.preference.text",TextScale);PlayerPrefs.SetInt("D02.preference.reducedMotion",ReducedMotion?1:0);PlayerPrefs.SetInt("D02.preference.contrast",HighContrast?1:0);PlayerPrefs.Save();}
    }
}
