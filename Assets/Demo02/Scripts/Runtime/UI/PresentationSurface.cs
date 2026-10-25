using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    public sealed class PresentationSurface : MonoBehaviour
    {
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
        public bool TitleComposition;
        public NativeSettingRow[] SettingRows;
        public Slider MasterSlider;public Text MasterValue;
        public Toggle SilentToggle,MotionToggle,ContrastToggle;public Dropdown TextSizeDropdown;
        public void ApplyScale(float scale)
        {
            bool large=scale>=1.5f;
            Frame.anchorMin=new Vector2(large?.02f:Wide?.06f:.21f,large?.24f:.20f);
            Frame.anchorMax=new Vector2(large?.98f:Wide?.94f:.79f,.98f);Frame.offsetMin=Frame.offsetMax=Vector2.zero;
            Heading.fontSize=Mathf.RoundToInt((TitleComposition?40:30)*scale);
            Status.fontSize=Mathf.RoundToInt(17*scale);
            Heading.rectTransform.offsetMin=new Vector2(large?84:78,-(large?94:62));Heading.rectTransform.offsetMax=new Vector2(large?-150:-120,-16);
            Paper.offsetMax=new Vector2(-12,-(TitleComposition?12:large?104:68));
            ((RectTransform)Close.transform).sizeDelta=new Vector2(large?120:90,large?64:44);Close.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(17*scale);
            Icon.rectTransform.sizeDelta=new Vector2(large?48:42,large?48:42);
            float tabsHeight=Tabs&&Tabs.gameObject.activeSelf?(large?74:52):0;
            if(Tabs){Tabs.offsetMin=new Vector2(18,-(large?104:68)-tabsHeight);Tabs.offsetMax=new Vector2(-18,-(large?104:68));}
            if(TabButtons!=null)foreach(var b in TabButtons)if(b)b.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(17*scale);
            bool hasFooter=Primary.gameObject.activeSelf||Secondary.gameObject.activeSelf||Tertiary.gameObject.activeSelf;
            float footer=hasFooter?(large?98:66):12,status=large?90:62,composer=Composer?(large?124:126):0;
            var sr=(RectTransform)Scroll.transform;sr.offsetMin=new Vector2(large?20:Sidebar?240:20,footer+status+composer);sr.offsetMax=new Vector2(-20,-(large?112:72)-tabsHeight);
            Status.rectTransform.offsetMin=new Vector2(20,footer);Status.rectTransform.offsetMax=new Vector2(-20,footer+status-6);
            if(Sidebar){Sidebar.gameObject.SetActive(!large);Sidebar.offsetMin=new Vector2(18,footer+status);Sidebar.offsetMax=new Vector2(226,-82-tabsHeight);}
            var buttons=new[]{Primary,Secondary,Tertiary};int count=0;foreach(var b in buttons)if(b&&b.gameObject.activeSelf)count++;
            int index=0;foreach(var b in buttons)if(b&&b.gameObject.activeSelf)
            {var r=(RectTransform)b.transform;r.anchorMin=new Vector2((float)index/count,0);r.anchorMax=new Vector2((float)(index+1)/count,0);r.offsetMin=new Vector2(18,14);r.offsetMax=new Vector2(-18,footer-8);b.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(20*scale);index++;}
            if(Composer)
            {ComposerFrame.offsetMin=new Vector2(20,footer+status);ComposerFrame.offsetMax=new Vector2(-20,footer+status+composer-8);Composer.textComponent.fontSize=Mathf.RoundToInt(21*scale);if(Composer.placeholder is Text p)p.fontSize=Mathf.RoundToInt(20*scale);Send.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(20*scale);CancelRequest.GetComponentInChildren<Text>().fontSize=Mathf.RoundToInt(17*scale);}
            foreach(var row in Rows.GetComponentsInChildren<NativeRow>(true))row.ApplyScale(scale);
            if(SettingRows!=null)foreach(var setting in SettingRows)setting.ApplyScale(scale);
            if(TitleComposition)
            {
                Heading.alignment=TextAnchor.MiddleCenter;Heading.color=NativeUiPalette.Ink;Heading.rectTransform.offsetMin=new Vector2(30,-156);Heading.rectTransform.offsetMax=new Vector2(-30,-76);Icon.rectTransform.anchorMin=Icon.rectTransform.anchorMax=new Vector2(.5f,1);Icon.rectTransform.anchoredPosition=new Vector2(0,-16);Icon.color=NativeUiPalette.Ink;var titleScroll=(RectTransform)Scroll.transform;titleScroll.offsetMax=new Vector2(-20,-172);
                if(!large){titleScroll.offsetMin=new Vector2(20,236);Status.rectTransform.offsetMin=new Vector2(30,174);Status.rectTransform.offsetMax=new Vector2(-30,230);int order=0;foreach(var b in new[]{Primary,Secondary,Tertiary}){var r=(RectTransform)b.transform;r.anchorMin=new Vector2(.22f,0);r.anchorMax=new Vector2(.78f,0);r.offsetMin=new Vector2(0,126-order*54);r.offsetMax=new Vector2(0,170-order*54);order++;}}
            }
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
