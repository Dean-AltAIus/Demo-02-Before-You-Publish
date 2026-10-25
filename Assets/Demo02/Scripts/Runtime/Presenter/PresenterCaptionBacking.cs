using UnityEngine;
namespace Altaius.Demo02.Presenter
{
    public sealed class PresenterCaptionBacking:MonoBehaviour
    {
        public GameObject Backing;
        public void Fit()
        {
            if(!Backing)return;var text=(RectTransform)transform;var rect=(RectTransform)Backing.transform;rect.SetParent(transform.parent,false);rect.anchorMin=text.anchorMin;rect.anchorMax=text.anchorMax;rect.pivot=text.pivot;rect.anchoredPosition=text.anchoredPosition+new Vector2(-8,8);rect.sizeDelta=text.sizeDelta+new Vector2(16,16);int index=transform.GetSiblingIndex();if(rect.GetSiblingIndex()<index)index--;rect.SetSiblingIndex(index);
        }
        void OnEnable(){if(Backing){Fit();Backing.SetActive(true);}}
        void OnDisable(){if(Backing)Backing.SetActive(false);}
    }
}
