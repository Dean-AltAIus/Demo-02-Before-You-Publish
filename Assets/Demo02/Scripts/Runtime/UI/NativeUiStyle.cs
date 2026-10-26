using UnityEngine;
namespace Altaius.Demo02.UI
{
    [CreateAssetMenu(menuName="Altaius/Demo 02/Compact game UI style")]
    public sealed class NativeUiStyle:ScriptableObject
    {
        [Header("Default geometry in 1280 x 720 reference units")]
        public Vector2 SessionSize=new Vector2(282,46),LeenaSize=new Vector2(200,62);
        public float PanelWidth=430,WorkspaceWidth=450,PanelHeight=500,ShortPanelHeight=390;
        public float ControlHeight=32,InputHeight=34,MultilineHeight=76,RowSpacing=8;
        public int RowPadding=8;
        [Header("Editable native typography")]
        public int BodyFont=14,SecondaryFont=12,ControlFont=13,HeadingFont=20,TitleFont=24;
        [Header("Restrained native frames")]
        public float FrameRadius=6;
    }
}
