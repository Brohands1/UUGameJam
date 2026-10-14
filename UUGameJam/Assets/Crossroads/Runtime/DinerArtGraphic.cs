using UnityEngine;
using UnityEngine.UI;

namespace Crossroads.Diner
{
    // Editable, resolution-independent placeholder artwork. Replace with sprites/prefabs in the catalog.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DinerArtGraphic : MaskableGraphic
    {
        public ArtKind kind;
        VertexHelper mesh;
        readonly Color outline=new Color(.08f,.12f,.15f);
        readonly Color ivory=new Color(.96f,.89f,.7f);
        readonly Color dark=new Color(.13f,.2f,.22f);
        Vector2 Point(float x,float y) { var r=rectTransform.rect; return new Vector2(r.xMin+x*r.width,r.yMin+y*r.height); }
        void Polygon(Color c,params Vector2[] points)
        {
            int start=mesh.currentVertCount;
            foreach(var p in points) mesh.AddVert(Point(p.x,p.y),c,Vector2.zero);
            for(int i=1;i<points.Length-1;i++) mesh.AddTriangle(start,start+i,start+i+1);
        }
        Vector2 P(float x,float y) => new Vector2(x,y);
        void Ellipse(float x,float y,float rx,float ry,Color c)
        {
            int start=mesh.currentVertCount; mesh.AddVert(Point(x,y),c,Vector2.zero);
            const int n=40;
            for(int i=0;i<=n;i++) { float a=i*2*Mathf.PI/n; mesh.AddVert(Point(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),c,Vector2.zero); }
            for(int i=0;i<n;i++) mesh.AddTriangle(start,start+i+1,start+i+2);
        }
        void Box(float x,float y,float w,float h,Color c) => Polygon(c,P(x,y),P(x+w,y),P(x+w,y+h),P(x,y+h));
        void Line(float x,float y,float xx,float yy,float thick,Color c)
        {
            var a=P(x,y); var b=P(xx,yy); var d=(b-a).normalized; var n=new Vector2(-d.y,d.x)*thick*.5f;
            Polygon(c,a+n,b+n,b-n,a-n);
        }
        void Eye(float x,float y) { Ellipse(x,y,.039f,.045f,ivory); Ellipse(x+.008f,y,.013f,.028f,outline); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            mesh=vh; vh.Clear(); var accent=color;
            switch(kind)
            {
                case ArtKind.Vampire:
                    Ellipse(.5f,.08f,.35f,.055f,new Color(0,0,0,.24f));
                    Polygon(outline,P(.14f,.15f),P(.28f,.68f),P(.72f,.68f),P(.88f,.15f));
                    Polygon(accent,P(.2f,.18f),P(.32f,.66f),P(.68f,.66f),P(.8f,.18f));
                    Polygon(dark,P(.25f,.75f),P(.5f,.56f),P(.75f,.75f),P(.67f,.36f),P(.34f,.36f));
                    Polygon(ivory,P(.42f,.56f),P(.58f,.56f),P(.61f,.21f),P(.4f,.21f));
                    Ellipse(.5f,.73f,.205f,.235f,outline); Ellipse(.5f,.71f,.172f,.202f,new Color(.63f,.74f,.67f));
                    Polygon(outline,P(.32f,.87f),P(.41f,.93f),P(.51f,.83f),P(.61f,.93f),P(.69f,.87f),P(.68f,.97f),P(.32f,.96f));
                    Eye(.43f,.74f); Eye(.57f,.74f);
                    Line(.43f,.62f,.58f,.62f,.026f,outline);
                    Polygon(ivory,P(.43f,.63f),P(.47f,.63f),P(.45f,.575f)); Polygon(ivory,P(.54f,.63f),P(.58f,.63f),P(.56f,.575f));
                    Polygon(outline,P(.43f,.49f),P(.5f,.45f),P(.57f,.49f),P(.57f,.39f),P(.5f,.44f),P(.43f,.39f));
                    break;
                case ArtKind.Werewolf:
                    Ellipse(.5f,.07f,.35f,.055f,new Color(0,0,0,.24f));
                    Polygon(outline,P(.14f,.13f),P(.24f,.42f),P(.35f,.58f),P(.68f,.58f),P(.8f,.37f),P(.87f,.13f));
                    Polygon(new Color(.3f,.37f,.3f),P(.2f,.14f),P(.29f,.48f),P(.7f,.48f),P(.81f,.14f));
                    Polygon(outline,P(.19f,.53f),P(.28f,.7f),P(.26f,.99f),P(.44f,.88f),P(.63f,.88f),P(.8f,.99f),P(.77f,.7f),P(.86f,.53f),P(.73f,.47f),P(.63f,.4f),P(.37f,.4f));
                    Polygon(accent,P(.24f,.55f),P(.33f,.7f),P(.31f,.91f),P(.44f,.82f),P(.62f,.82f),P(.74f,.91f),P(.72f,.7f),P(.8f,.55f),P(.68f,.5f),P(.61f,.45f),P(.4f,.45f));
                    Polygon(new Color(.79f,.55f,.49f),P(.35f,.83f),P(.35f,.71f),P(.44f,.78f)); Polygon(new Color(.79f,.55f,.49f),P(.64f,.78f),P(.7f,.85f),P(.69f,.72f));
                    Eye(.41f,.67f); Eye(.63f,.67f); Ellipse(.52f,.55f,.15f,.1f,ivory); Ellipse(.52f,.58f,.052f,.038f,outline);
                    Line(.52f,.55f,.52f,.51f,.018f,outline); Line(.46f,.5f,.58f,.5f,.012f,outline);
                    Box(.45f,.2f,.1f,.16f,new Color(.66f,.39f,.31f)); break;
                case ArtKind.Ghost:
                    Ellipse(.5f,.07f,.3f,.045f,new Color(0,0,0,.18f));
                    Ellipse(.5f,.67f,.29f,.29f,outline);
                    Polygon(outline,P(.21f,.68f),P(.11f,.15f),P(.3f,.22f),P(.43f,.12f),P(.58f,.21f),P(.74f,.13f),P(.89f,.23f),P(.79f,.68f));
                    Ellipse(.5f,.67f,.257f,.257f,accent);
                    Polygon(accent,P(.246f,.68f),P(.16f,.2f),P(.3f,.27f),P(.44f,.17f),P(.58f,.26f),P(.74f,.18f),P(.84f,.26f),P(.754f,.68f));
                    Ellipse(.41f,.65f,.052f,.078f,outline); Ellipse(.6f,.65f,.052f,.078f,outline); Ellipse(.51f,.49f,.032f,.045f,outline);
                    Ellipse(.395f,.68f,.012f,.021f,ivory); Ellipse(.585f,.68f,.012f,.021f,ivory);
                    Line(.3f,.43f,.28f,.3f,.018f,new Color(.42f,.65f,.63f)); break;
                case ArtKind.Garlic:
                    Polygon(outline,P(.4f,.58f),P(.49f,.98f),P(.59f,.98f),P(.62f,.6f));
                    Polygon(new Color(.52f,.66f,.43f),P(.45f,.59f),P(.51f,.93f),P(.55f,.93f),P(.56f,.6f));
                    Ellipse(.5f,.37f,.32f,.32f,outline); Ellipse(.5f,.38f,.28f,.28f,ivory);
                    Ellipse(.38f,.36f,.1f,.22f,new Color(.79f,.72f,.6f)); Ellipse(.39f,.38f,.072f,.2f,ivory);
                    Ellipse(.6f,.36f,.09f,.22f,new Color(.79f,.72f,.6f)); Ellipse(.6f,.38f,.066f,.2f,ivory);
                    Line(.48f,.1f,.45f,.02f,.016f,ivory); Line(.53f,.1f,.56f,.02f,.016f,ivory); break;
                case ArtKind.Chains:
                    for(int i=0;i<7;i++) { float a=i*Mathf.PI/3.5f; float x=.5f+.27f*Mathf.Cos(a),y=.5f+.32f*Mathf.Sin(a); Ellipse(x,y,.13f,.15f,outline); Ellipse(x,y,.095f,.12f,new Color(.68f,.79f,.8f)); Ellipse(x,y,.052f,.077f,dark); }
                    break;
                case ArtKind.Salt:
                    Box(.28f,.1f,.44f,.68f,outline); Box(.32f,.14f,.36f,.59f,new Color(.56f,.74f,.75f)); Box(.34f,.2f,.32f,.36f,ivory);
                    Box(.26f,.77f,.48f,.13f,outline); Box(.3f,.81f,.4f,.055f,new Color(.47f,.61f,.65f));
                    Polygon(dark,P(.5f,.52f),P(.56f,.38f),P(.5f,.27f),P(.44f,.38f)); break;
                case ArtKind.Blood:
                    Polygon(outline,P(.5f,.97f),P(.22f,.48f),P(.78f,.48f)); Ellipse(.5f,.35f,.3f,.3f,outline);
                    Polygon(accent,P(.5f,.88f),P(.255f,.44f),P(.745f,.44f)); Ellipse(.5f,.35f,.26f,.26f,accent);
                    Ellipse(.4f,.42f,.05f,.105f,new Color(1,.7f,.61f,.65f)); break;
                case ArtKind.Meat:
                    Line(.6f,.35f,.85f,.18f,.13f,ivory); Ellipse(.84f,.16f,.075f,.075f,ivory); Ellipse(.91f,.24f,.075f,.075f,ivory);
                    Ellipse(.43f,.56f,.34f,.29f,outline); Ellipse(.43f,.56f,.3f,.25f,new Color(.9f,.7f,.61f)); Ellipse(.43f,.56f,.25f,.2f,accent);
                    Line(.32f,.66f,.48f,.51f,.045f,new Color(.98f,.82f,.68f)); break;
                case ArtKind.Ecto:
                    Polygon(outline,P(.4f,.62f),P(.31f,.2f),P(.72f,.2f),P(.61f,.62f),P(.59f,.85f),P(.42f,.85f));
                    Polygon(accent,P(.45f,.6f),P(.36f,.24f),P(.66f,.24f),P(.56f,.6f),P(.55f,.8f),P(.46f,.8f));
                    Box(.39f,.83f,.22f,.07f,ivory); Ellipse(.5f,.37f,.12f,.12f,new Color(.73f,.96f,.8f));
                    Ellipse(.47f,.39f,.015f,.025f,outline); Ellipse(.55f,.39f,.015f,.025f,outline); break;
                case ArtKind.Stew: case ArtKind.Soup: case ArtKind.SpectralStew:
                    Ellipse(.5f,.18f,.41f,.08f,outline); Ellipse(.5f,.19f,.35f,.045f,ivory);
                    Polygon(outline,P(.11f,.59f),P(.23f,.25f),P(.77f,.25f),P(.89f,.59f));
                    Polygon(new Color(.57f,.69f,.66f),P(.17f,.55f),P(.27f,.3f),P(.73f,.3f),P(.83f,.55f));
                    Ellipse(.5f,.58f,.4f,.15f,outline); Ellipse(.5f,.59f,.35f,.11f,ivory); Ellipse(.5f,.6f,.3f,.08f,accent);
                    Ellipse(.4f,.61f,.06f,.035f,new Color(.46f,.69f,.38f)); Ellipse(.59f,.6f,.045f,.027f,new Color(.93f,.72f,.45f));
                    if(kind==ArtKind.Stew)
                    {
                        Box(.36f,.59f,.09f,.07f,new Color(.8f,.49f,.34f)); Box(.53f,.57f,.1f,.07f,new Color(.8f,.49f,.34f));
                        Line(.61f,.65f,.73f,.76f,.04f,ivory); Ellipse(.72f,.77f,.036f,.035f,ivory);
                    }
                    else if(kind==ArtKind.SpectralStew)
                    {
                        Ellipse(.5f,.82f,.085f,.1f,ivory); Polygon(ivory,P(.415f,.82f),P(.4f,.7f),P(.47f,.74f),P(.52f,.69f),P(.6f,.73f),P(.585f,.82f));
                        Ellipse(.47f,.83f,.012f,.023f,outline); Ellipse(.53f,.83f,.012f,.023f,outline);
                    }
                    else
                    {
                        Ellipse(.51f,.83f,.09f,.09f,ivory); Ellipse(.55f,.86f,.079f,.08f,dark);
                        Polygon(ivory,P(.29f,.82f),P(.32f,.88f),P(.35f,.82f),P(.32f,.76f));
                    }
                    Line(.26f,.79f,.29f,.93f,.02f,new Color(.87f,.9f,.81f,.6f)); Line(.75f,.8f,.72f,.95f,.02f,new Color(.87f,.9f,.81f,.6f)); break;
                case ArtKind.Pot:
                    Ellipse(.5f,.21f,.3f,.15f,outline); Box(.2f,.23f,.6f,.39f,outline);
                    Ellipse(.5f,.27f,.25f,.1f,dark); Box(.25f,.3f,.5f,.3f,dark);
                    Ellipse(.5f,.62f,.32f,.12f,outline); Ellipse(.5f,.63f,.26f,.072f,accent);
                    Line(.17f,.58f,.07f,.48f,.065f,outline); Line(.83f,.58f,.93f,.48f,.065f,outline);
                    for(int i=0;i<3;i++) { float x=.35f+i*.16f; Line(x,.77f,x-.02f,.9f,.018f,ivory); }
                    Polygon(new Color(.9f,.53f,.23f),P(.3f,.07f),P(.34f,.19f),P(.38f,.06f)); Polygon(new Color(.9f,.53f,.23f),P(.48f,.07f),P(.52f,.2f),P(.56f,.06f)); break;
                case ArtKind.Cleaver:
                    Box(.11f,.1f,.78f,.23f,new Color(.43f,.27f,.19f)); Box(.14f,.31f,.72f,.05f,ivory);
                    Polygon(outline,P(.42f,.44f),P(.59f,.44f),P(.82f,.85f),P(.66f,.94f));
                    Polygon(new Color(.62f,.76f,.74f),P(.45f,.47f),P(.57f,.47f),P(.78f,.84f),P(.67f,.89f));
                    Line(.43f,.48f,.29f,.24f,.095f,dark); break;
                case ArtKind.Lantern:
                    Ellipse(.5f,.48f,.4f,.45f,new Color(.92f,.64f,.28f,.05f)); Ellipse(.5f,.48f,.3f,.36f,new Color(.92f,.64f,.28f,.07f));
                    Box(.3f,.24f,.4f,.49f,outline); Box(.35f,.29f,.3f,.37f,new Color(.95f,.73f,.37f));
                    Line(.5f,.28f,.5f,.7f,.028f,outline); Box(.26f,.2f,.48f,.07f,outline); Box(.26f,.7f,.48f,.07f,outline); Line(.5f,.8f,.5f,1,.035f,outline); break;
                case ArtKind.Crossroads:
                    Box(0,0,1,1,new Color(.12f,.23f,.25f)); Ellipse(.79f,.8f,.09f,.14f,new Color(.8f,.81f,.63f));
                    Polygon(new Color(.24f,.31f,.28f),P(0,.3f),P(.18f,.51f),P(.35f,.32f),P(.58f,.49f),P(.79f,.31f),P(1,.53f),P(1,0),P(0,0));
                    Polygon(new Color(.46f,.43f,.33f),P(.03f,0),P(.28f,0),P(.53f,.4f),P(.85f,.66f),P(.79f,.66f),P(.45f,.35f));
                    Polygon(new Color(.46f,.43f,.33f),P(.75f,0),P(.98f,0),P(.55f,.38f),P(.21f,.68f),P(.16f,.68f),P(.46f,.3f));
                    for(int i=0;i<11;i++) { float x=i*.1f; if(i==2 || i==8) continue; float h=.13f+(i%3)*.045f; Polygon(dark,P(x-.055f,.4f),P(x,.4f+h),P(x+.055f,.4f)); }
                    Line(.58f,.25f,.58f,.65f,.018f,outline); Box(.52f,.58f,.19f,.045f,ivory); Box(.45f,.5f,.17f,.045f,ivory); break;
            }
        }
    }
}
