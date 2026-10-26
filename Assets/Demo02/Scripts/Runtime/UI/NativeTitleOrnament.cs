using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    public enum TitleOrnamentKind { ViewBorder, Cartouche, BookLaurel }
    /// <summary>Editable native geometry; no reference-image controls or baked title lettering.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NativeTitleOrnament : MaskableGraphic
    {
        public TitleOrnamentKind Kind;
        static readonly Color Navy=new Color(.025f,.10f,.19f),Gold=new Color(.86f,.62f,.24f),Cream=new Color(1,.968f,.875f);
        VertexHelper mesh;Rect r;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;r=GetPixelAdjustedRect();
            if(Kind==TitleOrnamentKind.ViewBorder)
            {
                Border(r,7,Navy);Border(Inset(r,7),2,Gold);Border(Inset(r,10),1,new Color(1,.9f,.63f,.8f));
                foreach(float x in new[]{r.xMin+5,r.xMax-5})foreach(float y in new[]{r.yMin+5,r.yMax-5})
                {float dx=x<r.center.x?1:-1,dy=y<r.center.y?1:-1;Line(new Vector2(x,y),new Vector2(x+dx*30,y),5,Gold);Line(new Vector2(x,y),new Vector2(x,y+dy*30),5,Gold);}
            }
            else if(Kind==TitleOrnamentKind.Cartouche)
            {
                Polygon(Inset(r,0),Navy);Polygon(Inset(r,3),Gold);Polygon(Inset(r,5),Navy);Polygon(Inset(r,7),Cream);
                Line(new Vector2(r.xMin+30,r.yMin+17),new Vector2(r.xMax-30,r.yMin+17),1.2f,Gold);
                Diamond(new Vector2(r.center.x,r.yMin+17),4,Gold);
            }
            else
            {
                var mid=r.center;float unit=r.height;
                for(int side=-1;side<=1;side+=2)
                {
                    Line(mid+new Vector2(side*unit*.31f,-unit*.27f),mid+new Vector2(side*unit*.7f,unit*.18f),1.5f,Gold);
                    for(int i=0;i<5;i++)
                    {var p=mid+new Vector2(side*unit*(.33f+i*.075f),unit*(-.22f+i*.09f));Leaf(p,new Vector2(side*.6f,.8f)*unit*.11f,Gold);Leaf(p,new Vector2(-side*.8f,.4f)*unit*.095f,Gold);}
                }
                var b=new Rect(mid.x-unit*.29f,mid.y-unit*.23f,unit*.58f,unit*.55f);Quad(b,Navy);Border(b,1.5f,Gold);
                Line(new Vector2(mid.x,b.yMin),new Vector2(mid.x,b.yMax),1.2f,Gold);
                for(int i=0;i<3;i++){float y=b.yMin+unit*(.12f+i*.12f);Line(new Vector2(b.xMin+unit*.055f,y),new Vector2(mid.x-unit*.045f,y-unit*.03f),.8f,Cream);Line(new Vector2(mid.x+unit*.045f,y-unit*.03f),new Vector2(b.xMax-unit*.055f,y),.8f,Cream);}
            }
        }
        static Rect Inset(Rect a,float n)=>new Rect(a.x+n,a.y+n,a.width-2*n,a.height-2*n);
        void Polygon(Rect a,Color c)
        {
            float k=11;var points=new[]{new Vector2(a.xMin+k,a.yMin),new Vector2(a.xMax-k,a.yMin),new Vector2(a.xMax,a.yMin+k),new Vector2(a.xMax,a.yMax-k),new Vector2(a.xMax-k,a.yMax),new Vector2(a.center.x+36,a.yMax),new Vector2(a.center.x+25,a.yMax+8),new Vector2(a.center.x-25,a.yMax+8),new Vector2(a.center.x-36,a.yMax),new Vector2(a.xMin+k,a.yMax),new Vector2(a.xMin,a.yMax-k),new Vector2(a.xMin,a.yMin+k)};
            int n=mesh.currentVertCount;mesh.AddVert(a.center,c*color,Vector2.zero);foreach(var p in points)mesh.AddVert(p,c*color,Vector2.zero);for(int i=0;i<points.Length;i++)mesh.AddTriangle(n,n+1+i,n+1+(i+1)%points.Length);
        }
        void Quad(Rect a,Color c){int n=mesh.currentVertCount;foreach(var p in new[]{new Vector2(a.xMin,a.yMin),new Vector2(a.xMin,a.yMax),new Vector2(a.xMax,a.yMax),new Vector2(a.xMax,a.yMin)})mesh.AddVert(p,c*color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);}
        void Border(Rect a,float w,Color c){Quad(new Rect(a.xMin,a.yMin,a.width,w),c);Quad(new Rect(a.xMin,a.yMax-w,a.width,w),c);Quad(new Rect(a.xMin,a.yMin,w,a.height),c);Quad(new Rect(a.xMax-w,a.yMin,w,a.height),c);}
        void Line(Vector2 a,Vector2 b,float w,Color c){var d=(b-a).normalized;var p=new Vector2(-d.y,d.x)*w*.5f;int n=mesh.currentVertCount;foreach(var v in new[]{a-p,a+p,b+p,b-p})mesh.AddVert(v,c*color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);}
        void Diamond(Vector2 p,float s,Color c)=>Leaf(p,new Vector2(s,0),c);
        void Leaf(Vector2 p,Vector2 v,Color c){var side=new Vector2(-v.y,v.x)*.35f;int n=mesh.currentVertCount;foreach(var a in new[]{p,p+v*.55f+side,p+v,p+v*.55f-side})mesh.AddVert(a,c*color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);}
    }
}
