using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    public sealed class NativeSettingRow : MonoBehaviour
    {
        public NativeUiStyle Style;
        public string SettingId;public int Tab;public Text Label;public RectTransform Control;
        public void ApplyScale(float scale)
        {GetComponent<LayoutElement>().preferredHeight=56*scale;Label.color=NativeUiPalette.Ink;Label.fontSize=Mathf.RoundToInt((Style?Style.BodyFont:14)*scale);foreach(var text in Control.GetComponentsInChildren<Text>(true)){text.color=NativeUiPalette.Ink;text.fontSize=Mathf.RoundToInt((Style?Style.ControlFont:13)*scale);}foreach(var toggle in Control.GetComponentsInChildren<Toggle>(true)){var background=toggle.transform.Find("Background") as RectTransform;if(background)background.sizeDelta=new Vector2(18*scale,18*scale);var caption=toggle.GetComponentInChildren<Text>();caption.rectTransform.offsetMin=new Vector2(26*scale,0);}foreach(var slider in Control.GetComponentsInChildren<Slider>(true))slider.handleRect.sizeDelta=new Vector2(18*scale,0);}
    }
}
