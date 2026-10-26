using System;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    /// <summary>Saved reusable paper layer, sized by the live text layout without changing bindings.</summary>
    [DisallowMultipleComponent]
    public sealed class NativeRowPaperCard : MonoBehaviour
    {
        public NativeRow Row;
        public NativeFrame Card;
        public VerticalLayoutGroup Layout;
        public Vector4 PlainPadding;
        public string[] ControlPrefixes={"Review.Pair","Review.Earlier","Review.Gate","Review.Original","Hold.Current","Receipt.Status","Receipt.Outcome","Debrief.Outcome"};
        ulong revision=ulong.MaxValue;
        void LateUpdate()
        {
            if(!Row||!Card||revision==Row.BindingGeneration)return;revision=Row.BindingGeneration;
            bool visible=Array.Exists(ControlPrefixes,p=>(Row.ControlId??"").StartsWith(p,StringComparison.Ordinal));
            Card.gameObject.SetActive(visible);
            if(Layout)Layout.padding=visible?new RectOffset(8,8,6,6):new RectOffset((int)PlainPadding.x,(int)PlainPadding.y,(int)PlainPadding.z,(int)PlainPadding.w);
        }
    }
}
