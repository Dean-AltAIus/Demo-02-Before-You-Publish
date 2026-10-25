using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    public enum NativeIconKind { Book, Document, Conversation, Coach, Clock, Lock, Settings, Return, Send, Reset, Pause, Timeline, Check, Unavailable, Person, Source }
    /// <summary>One consistent geometric icon vocabulary, each symbol has a live text label beside it.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NativeIcon : MaskableGraphic
    {
        public NativeIconKind Kind;
        public float Stroke=2.5f;
        VertexHelper mesh;Rect rect;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;rect=GetPixelAdjustedRect();
            switch(Kind)
            {
                case NativeIconKind.Book: Box(.08f,.2f,.38f,.65f);Box(.54f,.2f,.38f,.65f);Line(.5f,.16f,.5f,.87f);break;
                case NativeIconKind.Document:case NativeIconKind.Source: Box(.22f,.08f,.57f,.83f);Line(.34f,.68f,.68f,.68f);Line(.34f,.50f,.68f,.50f);Line(.34f,.32f,.6f,.32f);if(Kind==NativeIconKind.Source)Circle(.75f,.2f,.16f);break;
                case NativeIconKind.Conversation:Box(.1f,.3f,.8f,.58f);Line(.27f,.3f,.27f,.1f);Line(.27f,.1f,.48f,.3f);Line(.25f,.64f,.75f,.64f);break;
                case NativeIconKind.Coach:Circle(.5f,.5f,.36f);Circle(.38f,.57f,.035f);Circle(.62f,.57f,.035f);Line(.36f,.34f,.64f,.34f);break;
                case NativeIconKind.Clock:Circle(.5f,.5f,.39f);Line(.5f,.5f,.5f,.77f);Line(.5f,.5f,.7f,.38f);break;
                case NativeIconKind.Lock:Box(.17f,.1f,.66f,.48f);Line(.3f,.58f,.3f,.78f);Line(.3f,.78f,.5f,.91f);Line(.5f,.91f,.7f,.78f);Line(.7f,.78f,.7f,.58f);Circle(.5f,.35f,.05f);break;
                case NativeIconKind.Settings:Circle(.5f,.5f,.28f);Circle(.5f,.5f,.1f);for(int i=0;i<8;i++){var a=i*Mathf.PI/4;Line(.5f+Mathf.Cos(a)*.28f,.5f+Mathf.Sin(a)*.28f,.5f+Mathf.Cos(a)*.43f,.5f+Mathf.Sin(a)*.43f);}break;
                case NativeIconKind.Return:Line(.82f,.5f,.18f,.5f);Line(.18f,.5f,.44f,.78f);Line(.18f,.5f,.44f,.22f);break;
                case NativeIconKind.Send:Line(.12f,.83f,.88f,.5f);Line(.88f,.5f,.12f,.16f);Line(.12f,.16f,.3f,.5f);Line(.3f,.5f,.12f,.83f);Line(.3f,.5f,.65f,.5f);break;
                case NativeIconKind.Reset:Circle(.5f,.5f,.34f);Line(.8f,.81f,.8f,.55f);Line(.8f,.55f,.56f,.6f);break;
                case NativeIconKind.Pause:Box(.24f,.16f,.14f,.68f);Box(.62f,.16f,.14f,.68f);break;
                case NativeIconKind.Timeline:Line(.22f,.1f,.22f,.9f);for(int i=0;i<3;i++){float y=.25f+i*.25f;Circle(.22f,y,.07f);Line(.4f,y,.86f,y);}break;
                case NativeIconKind.Check:Line(.16f,.47f,.4f,.2f);Line(.4f,.2f,.86f,.8f);break;
                case NativeIconKind.Unavailable:Circle(.5f,.5f,.39f);Line(.22f,.22f,.78f,.78f);break;
                case NativeIconKind.Person:Circle(.5f,.73f,.17f);Line(.19f,.15f,.27f,.42f);Line(.27f,.42f,.73f,.42f);Line(.73f,.42f,.81f,.15f);Line(.81f,.15f,.19f,.15f);break;
            }
        }
        Vector2 P(float x,float y)=>new Vector2(rect.xMin+x*rect.width,rect.yMin+y*rect.height);
        void Line(float x,float y,float ex,float ey)
        {
            var a=P(x,y);var b=P(ex,ey);var n=(b-a).normalized;var p=new Vector2(-n.y,n.x)*Stroke/2;int i=mesh.currentVertCount;
            mesh.AddVert(a-p,color,Vector2.zero);mesh.AddVert(a+p,color,Vector2.zero);mesh.AddVert(b+p,color,Vector2.zero);mesh.AddVert(b-p,color,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
        }
        void Box(float x,float y,float w,float h){Line(x,y,x+w,y);Line(x+w,y,x+w,y+h);Line(x+w,y+h,x,y+h);Line(x,y+h,x,y);}
        void Circle(float x,float y,float radius){for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);}}
    }
}
