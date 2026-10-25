using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    public sealed class NativeSettingRow : MonoBehaviour
    {
        public string SettingId;public int Tab;public Text Label;public RectTransform Control;
        public void ApplyScale(float scale)
        {GetComponent<LayoutElement>().preferredHeight=76*scale;Label.fontSize=Mathf.RoundToInt(20*scale);foreach(var text in Control.GetComponentsInChildren<Text>(true))text.fontSize=Mathf.RoundToInt(19*scale);foreach(var toggle in Control.GetComponentsInChildren<Toggle>(true)){var background=toggle.transform.Find("Background") as RectTransform;if(background)background.sizeDelta=new Vector2(24*scale,24*scale);var caption=toggle.GetComponentInChildren<Text>();caption.rectTransform.offsetMin=new Vector2(34*scale,0);}foreach(var slider in Control.GetComponentsInChildren<Slider>(true))slider.handleRect.sizeDelta=new Vector2(24*scale,0);}
    }
}
