using UnityEngine;
namespace Altaius.Demo02.Gameplay
{
    public sealed class WorldMarkingLane : MonoBehaviour
    {
        public TextMesh CriterionLabel;
        public TextMesh MarkLabel;
        public GameObject PassageProxy;
        public Vector2 LabelArea = new Vector2(.51f,.065f);
        public void FitLabels()
        {
            Fit(CriterionLabel); Fit(MarkLabel);
        }
        void Fit(TextMesh label)
        {
            if(!label||!label.font)return;
            label.font.RequestCharactersInTexture(label.text,label.fontSize,label.fontStyle);
            float width=0,line=0;int lines=1;
            foreach(char c in label.text){if(c=='\n'){width=Mathf.Max(width,line);line=0;lines++;}else if(label.font.GetCharacterInfo(c,out var info,label.fontSize,label.fontStyle))line+=info.advance;}
            width=Mathf.Max(width,line);
            float height=lines*(float)label.font.lineHeight*label.fontSize/Mathf.Max(1,label.font.fontSize);
            label.characterSize=Mathf.Min(.04f,LabelArea.x*10/Mathf.Max(1,width),LabelArea.y*10/Mathf.Max(1,height));
        }
    }
}
