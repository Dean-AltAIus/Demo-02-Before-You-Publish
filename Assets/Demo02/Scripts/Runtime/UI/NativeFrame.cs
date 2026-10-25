using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    public enum NativeFrameKind { Navy, Paper, Button, QuietButton, Input, Tab }
    /// <summary>Editable vector geometry. No reference screenshot, baked lettering or generated screen texture.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NativeFrame : MaskableGraphic
    {
        public NativeFrameKind Kind;
        public bool GoldCorners=true;
        public float Radius=12;
        public Color Navy=new Color(.025f,.11f,.23f);
        public Color Paper=new Color(1f,.963f,.86f);
        public Color Gold=new Color(.88f,.59f,.16f);
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var r=GetPixelAdjustedRect();
            // Soft layered offset shadow supports the dimensional supplied frame language.
            for(int i=3;i>=1;i--) Rounded(mesh,new Rect(r.x-i,r.y-2-i,r.width+2*i,r.height+2*i),Radius+i,new Color(0,.025f,.06f,.045f),new Color(0,.025f,.06f,.045f));
            Rounded(mesh,r,Radius,Navy,Navy);
            var edge=Inset(r,2);Rounded(mesh,edge,Radius-1,Gold,new Color(1f,.82f,.38f));
            var inner=Inset(r,4);var blue=Kind==NativeFrameKind.Navy || Kind==NativeFrameKind.Button || Kind==NativeFrameKind.Tab;
            var top=blue?new Color(.025f,.39f,.65f):new Color(1f,.98f,.91f);
            var bottom=blue?new Color(.015f,.14f,.32f):new Color(.93f,.86f,.72f);
            if(Kind==NativeFrameKind.Navy){top=new Color(.035f,.26f,.43f);bottom=Navy;}
            if(Kind==NativeFrameKind.Input){top=new Color(1,.99f,.95f);bottom=new Color(.99f,.965f,.90f);}
            Rounded(mesh,inner,Radius-2,bottom,top);
            if(Kind==NativeFrameKind.Paper)Rounded(mesh,Inset(r,7),Radius-3,Paper,new Color(1f,.979f,.91f));
            if(GoldCorners)
            {
                Corner(mesh,new Vector2(r.xMin+4,r.yMax-4),1,-1);
                Corner(mesh,new Vector2(r.xMax-4,r.yMax-4),-1,-1);
                Corner(mesh,new Vector2(r.xMin+4,r.yMin+4),1,1);
                Corner(mesh,new Vector2(r.xMax-4,r.yMin+4),-1,1);
            }
        }
        void Corner(VertexHelper m,Vector2 p,float dx,float dy)
        {
            var light=new Color(1f,.83f,.39f);var dark=new Color(.63f,.34f,.055f);
            Quad(m,p+new Vector2(dx*2,dy*2),p+new Vector2(dx*24,dy*9),Gold,light);
            Quad(m,p+new Vector2(dx*2,dy*2),p+new Vector2(dx*9,dy*24),dark,light);
        }
        static Rect Inset(Rect r,float p)=>new Rect(r.x+p,r.y+p,Mathf.Max(0,r.width-2*p),Mathf.Max(0,r.height-2*p));
        void Quad(VertexHelper m,Vector2 a,Vector2 b,Color low,Color high)
        {Rounded(m,Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y)),2,low,high);}
        void Rounded(VertexHelper m,Rect r,float radius,Color low,Color high)
        {
            low*=color;high*=color;
            radius=Mathf.Clamp(radius,0,Mathf.Min(r.width,r.height)/2);int start=m.currentVertCount;
            m.AddVert(r.center,Color.Lerp(low,high,.5f),Vector2.zero);
            for(int c=0;c<4;c++)
            {
                var centre=new Vector2(c==0||c==3?r.xMax-radius:r.xMin+radius,c<2?r.yMax-radius:r.yMin+radius);
                for(int j=0;j<=5;j++)
                {float angle=(c*90+j*18)*Mathf.Deg2Rad;var p=centre+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;float t=r.height>0?(p.y-r.yMin)/r.height:0;m.AddVert(p,Color.Lerp(low,high,t),Vector2.zero);}
            }
            int count=m.currentVertCount-start-1;for(int i=0;i<count;i++)m.AddTriangle(start,start+1+i,start+1+(i+1)%count);
        }
    }
}
