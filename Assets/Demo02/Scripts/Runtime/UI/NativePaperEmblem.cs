using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    public enum PaperEmblemKind { Folio, Review, Sources, Record }
    /// <summary>Editable native still life. Current status comes from a read-only projection, with no baked copy.</summary>
    public sealed class NativePaperEmblem : MaskableGraphic
    {
        public PaperEmblemKind Kind;
        public ReviewArtState ReviewState;
        public void BindReview(ReviewArtState state)
        {if(Kind!=PaperEmblemKind.Review||ReviewState!=state){Kind=PaperEmblemKind.Review;ReviewState=state;SetVerticesDirty();}}
        VertexHelper mesh; Rect r;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;r=GetPixelAdjustedRect();
            for(int i=0;i<3;i++)
            {float y=.1f+i*.09f;Quad(.08f,y,.82f,.10f,new Color(.025f,.15f,.3f),new Color(.045f,.34f,.50f));Quad(.14f,y+.015f,.71f,.045f,new Color(.87f,.81f,.66f),new Color(1,.96f,.83f));Quad(.06f,y+.065f,.86f,.025f,new Color(.77f,.49f,.10f),new Color(1,.78f,.3f));}
            Quad(.17f,.32f,.68f,.59f,new Color(.025f,.09f,.18f),new Color(.06f,.27f,.42f));
            Quad(.195f,.34f,.63f,.55f,new Color(.86f,.78f,.62f),new Color(1,.965f,.86f));
            Quad(.23f,.365f,.56f,.50f,new Color(.99f,.94f,.82f),new Color(1,.98f,.90f));
            Quad(.35f,.84f,.31f,.09f,new Color(.62f,.39f,.1f),new Color(1,.78f,.35f));
            for(int i=0;i<5;i++)Quad(.29f,.72f-i*.068f,i==4?.28f:.44f,.012f,new Color(.13f,.29f,.38f,.3f),new Color(.13f,.29f,.38f,.3f));
            if(Kind==PaperEmblemKind.Review)
            {
                var ink=new Color(.025f,.09f,.18f);var gold=new Color(1,.77f,.26f);var cream=new Color(1,.97f,.86f);
                if(ReviewState==ReviewArtState.Stale)
                {
                    Quad(.03f,.38f,.28f,.29f,ink,new Color(.08f,.25f,.37f));Quad(.06f,.40f,.22f,.24f,cream,Color.white);
                    Quad(.15f,.48f,.28f,.29f,ink,new Color(.08f,.25f,.37f));Quad(.18f,.50f,.22f,.24f,cream,Color.white);
                    Line(.30f,.52f,.40f,.62f,.025f,ink);Line(.30f,.62f,.40f,.52f,.025f,ink);
                }
                if(ReviewState==ReviewArtState.Held||ReviewState==ReviewArtState.UnverifiedHold||ReviewState==ReviewArtState.HoldRequested)
                {
                    Disc(.70f,.36f,.19f,ink);Disc(.70f,.36f,.165f,gold);
                    Quad(.63f,.25f,.045f,.22f,ink,ink);Quad(.72f,.25f,.045f,.22f,ink,ink);
                    if(ReviewState!=ReviewArtState.Held){Disc(.85f,.52f,.075f,cream);Line(.85f,.49f,.85f,.56f,.02f,ink);}
                }
                else
                {
                    Quad(.54f,.20f,.32f,.28f,new Color(.69f,.41f,.065f),gold);Quad(.57f,.23f,.26f,.20f,new Color(.96f,.61f,.15f),new Color(1,.86f,.45f));
                    bool open=ReviewState==ReviewArtState.Released;
                    Quad(.58f,.46f,.045f,.12f,new Color(.77f,.55f,.19f),cream);
                    Quad(open?.86f:.77f,.46f,.045f,.12f,new Color(.77f,.55f,.19f),cream);
                    Quad(.58f,.56f,open?.325f:.235f,.04f,new Color(.77f,.55f,.19f),new Color(1,.87f,.5f));
                    Quad(.68f,.29f,.04f,.10f,ink,ink);
                    if(ReviewState==ReviewArtState.Pending||ReviewState==ReviewArtState.Scheduled)
                    {Disc(.31f,.48f,.15f,ink);Disc(.31f,.48f,.125f,cream);Line(.31f,.48f,.31f,.57f,.018f,ink);Line(.31f,.48f,.38f,.44f,.018f,ink);}
                    if(ReviewState==ReviewArtState.Returned)
                    {Line(.43f,.59f,.20f,.59f,.035f,ink);Line(.20f,.59f,.29f,.68f,.035f,ink);Line(.20f,.59f,.29f,.50f,.035f,ink);}
                    if(ReviewState==ReviewArtState.Approved||open)
                    {Disc(.32f,.52f,.13f,ink);Line(.24f,.52f,.30f,.46f,.025f,gold);Line(.30f,.46f,.41f,.60f,.025f,gold);}
                    if(ReviewState==ReviewArtState.Denied||ReviewState==ReviewArtState.Unavailable)
                    {Disc(.31f,.53f,.13f,ink);Line(.23f,.45f,.39f,.61f,.025f,cream);}
                    if(ReviewState==ReviewArtState.Unknown)
                    {Disc(.31f,.53f,.13f,ink);Line(.26f,.59f,.36f,.59f,.025f,cream);Line(.36f,.59f,.31f,.50f,.025f,cream);Disc(.31f,.45f,.018f,cream);}
                }
            }
        }
        void Line(float x,float y,float xx,float yy,float width,Color tint)
        {
            var a=new Vector2(r.xMin+x*r.width,r.yMin+y*r.height);var b=new Vector2(r.xMin+xx*r.width,r.yMin+yy*r.height);
            var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*r.width*.5f;int i=mesh.currentVertCount;
            foreach(var p in new[]{a-n,a+n,b+n,b-n})mesh.AddVert(p,tint*color,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
        }
        void Disc(float x,float y,float radius,Color tint)
        {
            int i=mesh.currentVertCount;mesh.AddVert(new Vector2(r.xMin+x*r.width,r.yMin+y*r.height),tint*color,Vector2.zero);
            for(int k=0;k<=24;k++){float a=k*Mathf.PI*2/24;mesh.AddVert(new Vector2(r.xMin+(x+Mathf.Cos(a)*radius)*r.width,r.yMin+y*r.height+Mathf.Sin(a)*radius*r.width),tint*color,Vector2.zero);if(k>0)mesh.AddTriangle(i,i+k,i+k+1);}
        }
        void Quad(float x,float y,float w,float h,Color bottom,Color top)
        {int i=mesh.currentVertCount;mesh.AddVert(new Vector2(r.xMin+x*r.width,r.yMin+y*r.height),bottom*color,Vector2.zero);mesh.AddVert(new Vector2(r.xMin+(x+w)*r.width,r.yMin+y*r.height),bottom*color,Vector2.zero);mesh.AddVert(new Vector2(r.xMin+(x+w)*r.width,r.yMin+(y+h)*r.height),top*color,Vector2.zero);mesh.AddVert(new Vector2(r.xMin+x*r.width,r.yMin+(y+h)*r.height),top*color,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);}
    }
}
