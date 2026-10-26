using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    public enum NativeFrameKind { Navy, Paper, Button, QuietButton, Input, Tab, Card }
    /// <summary>Editable vector geometry. No reference screenshot, baked lettering or generated screen texture.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NativeFrame : MaskableGraphic
    {
        public NativeFrameKind Kind;
        public bool GoldCorners=true;
        public float Radius=6;
        public Color Navy=new Color(.025f,.11f,.23f);
        public Color Paper=new Color(1f,.963f,.86f);
        public Color Gold=new Color(.88f,.59f,.16f);
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var r=GetPixelAdjustedRect();
            if(Kind==NativeFrameKind.Card)
            {
                Rounded(mesh,new Rect(r.x+1,r.y-3,r.width,r.height),5,new Color(.15f,.12f,.08f,.09f),new Color(.15f,.12f,.08f,.09f));
                Rounded(mesh,r,5,new Color(.74f,.68f,.55f),new Color(.88f,.83f,.72f));
                Rounded(mesh,Inset(r,1),4,new Color(.97f,.94f,.86f),new Color(1,.99f,.94f));
                Quad(mesh,new Vector2(r.xMin+9,r.yMax-2),new Vector2(r.xMax-9,r.yMax-1),Color.white,Color.white);
                return;
            }
            // Soft layered offset shadow supports the dimensional supplied frame language.
            for(int i=2;i>=1;i--) Rounded(mesh,new Rect(r.x-i,r.y-2-i,r.width+2*i,r.height+2*i),Radius+i,new Color(0,.025f,.06f,.045f),new Color(0,.025f,.06f,.045f));
            Rounded(mesh,r,Radius,Navy,Navy);
            var edge=Inset(r,.65f);Rounded(mesh,edge,Radius-1,Gold,new Color(.88f,.72f,.42f));
            var inner=Inset(r,1.2f);var blue=Kind==NativeFrameKind.Navy || Kind==NativeFrameKind.Button || Kind==NativeFrameKind.Tab;
            var top=blue?new Color(.045f,.23f,.35f):new Color(1f,.98f,.91f);
            var bottom=blue?new Color(.015f,.095f,.18f):new Color(.93f,.86f,.72f);
            if(Kind==NativeFrameKind.Navy){top=new Color(.035f,.19f,.29f);bottom=Navy;}
            if(Kind==NativeFrameKind.Input){top=new Color(1,.99f,.95f);bottom=new Color(.99f,.965f,.90f);}
            Rounded(mesh,inner,Radius-1,bottom,top);
            if(Kind==NativeFrameKind.Paper)
            {
                Rounded(mesh,Inset(r,2),Radius-1,Paper,new Color(1f,.979f,.91f));
                // Subtle native fibres and an inset ruled edge, kept behind live content.
                for(int i=0;i<28;i++){float x=r.xMin+12+(r.width-24)*((i*.6180339f)%1);float y=r.yMin+12+(r.height-24)*((i*.381966f)%1);Quad(mesh,new Vector2(x,y),new Vector2(x+Mathf.Min(12,r.width*.025f),y+.35f),new Color(.55f,.40f,.22f,.045f),new Color(.55f,.40f,.22f,.045f));}
                Quad(mesh,new Vector2(r.xMin+12,r.yMax-10),new Vector2(r.xMax-12,r.yMax-9),new Color(1,1,.94f,.8f),new Color(1,1,.94f,.8f));
            }
            if(blue)
            {
                Quad(mesh,new Vector2(r.xMin+Radius,r.yMax-5),new Vector2(r.xMax-Radius,r.yMax-4),new Color(.52f,.66f,.69f,.28f),new Color(.52f,.66f,.69f,.28f));
                Quad(mesh,new Vector2(r.xMin+Radius,r.yMin+5),new Vector2(r.xMax-Radius,r.yMin+6),new Color(.01f,.055f,.12f,.65f),new Color(.01f,.055f,.12f,.65f));
            }
            if(GoldCorners&&Kind==NativeFrameKind.Navy)
            {
                Corner(mesh,new Vector2(r.xMin+4,r.yMax-4),1,-1);
                Corner(mesh,new Vector2(r.xMax-4,r.yMax-4),-1,-1);
                Corner(mesh,new Vector2(r.xMin+4,r.yMin+4),1,1);
                Corner(mesh,new Vector2(r.xMax-4,r.yMin+4),-1,1);
            }
        }
        void Corner(VertexHelper m,Vector2 p,float dx,float dy)
        {
            var light=new Color(.9f,.74f,.45f);var dark=new Color(.43f,.30f,.13f);
            Quad(m,p+new Vector2(dx*2,dy*2),p+new Vector2(dx*19,dy*7),dark,Gold);
            Quad(m,p+new Vector2(dx*2,dy*2),p+new Vector2(dx*7,dy*19),dark,Gold);
            Quad(m,p+new Vector2(dx*3,dy*3),p+new Vector2(dx*17,dy*4.5f),Gold,light);
            Quad(m,p+new Vector2(dx*3,dy*3),p+new Vector2(dx*4.5f,dy*17),Gold,light);
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
