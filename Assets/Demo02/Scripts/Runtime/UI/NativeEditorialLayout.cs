using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    /// <summary>Pairs related editable metadata while retaining native controls, order and large-text scrolling.</summary>
    [DisallowMultipleComponent]
    public sealed class NativeEditorialLayout : LayoutGroup
    {
        public float Spacing=8, ColumnGap=12;
        bool Pair(int i)
        {
            if(i+1>=rectChildren.Count || rectTransform.rect.width<650)return false;
            var a=rectChildren[i].GetComponent<NativeRow>();var b=rectChildren[i+1].GetComponent<NativeRow>();
            if(!a||!b||!a.Caption||a.Caption.fontSize>24)return false;
            string id=a.ControlId??"",next=b.ControlId??"";
            if(id.StartsWith("Preview.Before."))return next=="Preview.After."+id.Substring(15);
            if(id=="Task.Attach.S_A")return next=="Task.Attach.S_B";
            if(id.StartsWith("Task.")&&id.EndsWith(".Marks"))return next==id.Substring(0,id.Length-6)+".Outcomes";
            if(!id.StartsWith("Rubric."))return false;
            if(id.EndsWith(".Id"))return next==id.Substring(0,id.Length-3)+".Question";
            return id.EndsWith(".Outcomes")&&next==id.Substring(0,id.Length-9)+".Marks";
        }
        public override void CalculateLayoutInputHorizontal(){base.CalculateLayoutInputHorizontal();SetLayoutInputForAxis(0,0,-1,0);}
        public override void SetLayoutHorizontal()
        {
            float width=rectTransform.rect.width-padding.horizontal;
            for(int i=0;i<rectChildren.Count;i++)
            {
                bool pair=Pair(i);float childWidth=pair?(width-ColumnGap)*.5f:width;
                SetChildAlongAxis(rectChildren[i],0,padding.left,childWidth);
                if(pair){i++;SetChildAlongAxis(rectChildren[i],0,padding.left+childWidth+ColumnGap,childWidth);}
            }
        }
        public override void CalculateLayoutInputVertical(){SetLayoutInputForAxis(Height(false),Height(false),-1,1);}
        public override void SetLayoutVertical(){Height(true);}
        float Height(bool place)
        {
            float y=padding.top;
            for(int i=0;i<rectChildren.Count;i++)
            {
                bool pair=Pair(i);float ownHeight=Mathf.Max(LayoutUtility.GetMinHeight(rectChildren[i]),LayoutUtility.GetPreferredHeight(rectChildren[i]));
                float otherHeight=pair?Mathf.Max(LayoutUtility.GetMinHeight(rectChildren[i+1]),LayoutUtility.GetPreferredHeight(rectChildren[i+1])):0;
                float height=Mathf.Max(ownHeight,otherHeight);
                // Each row retains its ContentSizeFitter height. Giving a shorter
                // row the shared height lets its centred pivot shift the top.
                if(place)SetChildAlongAxis(rectChildren[i],1,y,ownHeight);
                if(pair){i++;if(place)SetChildAlongAxis(rectChildren[i],1,y,otherHeight);}
                y+=height+Spacing;
            }
            return y+padding.bottom-(rectChildren.Count>0?Spacing:0);
        }
    }
}
