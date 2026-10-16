using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Crossroads.Diner
{
    [Serializable] public sealed class GuestWidget
    {
        public RectTransform figure;
        public DinerVisual portrait, dish;
        public GameObject orderRoot, detailsRoot;
        public Text slot, recipe, ingredients, price, patience, details;
        public Image patienceFill;
        public DinerHitArea target;
    }
    [Serializable] public sealed class ItemWidget
    { public DinerVisual visual; public Text label, sublabel; public DinerHitArea hit; }

    public sealed class DinerPresentation : MonoBehaviour
    {
        public DinerConfig config;
        public RectTransform front, kitchen, dragRoot;
        public GameObject counterItems, serviceHint, readyPanel, pausePanel, resultsPanel;
        public GameObject tutorialPanel;
        public bool TutorialOpen { get; private set; }
        public GuestWidget[] guests;
        public ItemWidget[] tools, frontTray, kitchenTray, captures, bins;
        public ItemWidget processor, pot, reserved;
        public DinerVisual dragIcon;
        public DinerVisual[] recipeIcons;
        public Text timeText, revenueText, viewText, stockText, noticeText, helpText, dragLabel, kitchenStatus, resultRevenue, resultStats, resultTelemetry, volumeText;
        [SerializeField] Text targetStatus, resultOutcome, resultGoal;
        public Image timeFill, processorFill, potFill;
        public List<DinerHitArea> hitAreas=new List<DinerHitArea>();
        public RectTransform layoutRoot;
        DinerTheme Theme => config.theme;
        Font Font => Theme.bodyFont!=null ? Theme.bodyFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        readonly Color wood=new Color(.23f,.18f,.15f);
        readonly Color panel=new Color(.105f,.17f,.185f);
        readonly Color muted=new Color(.62f,.69f,.65f);
        readonly Color rim=new Color(.34f,.39f,.33f);

        // Also called by the editor installer: all layout objects are serialized in the scene/prefab.
        public void Build()
        {
            if(layoutRoot!=null) return;
            hitAreas.Clear();
            var canvas=gameObject.GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=gameObject.GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1600,900); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var surround=Box(transform,"OuterBackground",0,0,1600,900,Theme.ink);
            surround.anchorMin=Vector2.zero; surround.anchorMax=Vector2.one; surround.offsetMin=Vector2.zero; surround.offsetMax=Vector2.zero;
            layoutRoot=Rect(transform,"Layout 1600 x 900",0,0,1600,900);
            layoutRoot.anchorMin=layoutRoot.anchorMax=new Vector2(.5f,.5f); layoutRoot.pivot=new Vector2(.5f,.5f); layoutRoot.anchoredPosition=Vector2.zero;
            front=Rect(layoutRoot,"Front room",0,0,1600,900);
            kitchen=Rect(layoutRoot,"Kitchen room",0,0,1600,900);
            BuildFront(); BuildKitchen(); BuildHud(); BuildMenus();
            dragRoot=Box(layoutRoot,"Held item",0,0,148,146,new Color(.08f,.13f,.14f,.95f));
            dragRoot.pivot=new Vector2(.5f,.5f);
            dragIcon=Visual(dragRoot,"Held art",32,7,84,88);
            dragLabel=Label(dragRoot,"Held name","",6,100,136,40,16,Theme.cream,TextAnchor.MiddleCenter);
            dragRoot.gameObject.SetActive(false);
            EnsureRulePresentation();
        }
        // Update existing layouts additively so custom artwork and positions remain intact.
        public void EnsureRulePresentation()
        {
            targetStatus=revenueText.transform.parent.Find("Revenue caption").GetComponent<Text>();
            resultOutcome=resultsPanel.transform.Find("Title").GetComponent<Text>();
            resultGoal=resultsPanel.transform.Find("Revenue caption").GetComponent<Text>();
            revenueText.fontSize=27;
            string goal="Earn $"+config.revenueTarget+" by closing.";
            string capture="Each capture costs other guests "+config.capturePatiencePenalty.ToString("0.#")+"s patience.";
            readyPanel.transform.Find("Fine print").GetComponent<Text>().text=goal+"  /  "+capture;
            pausePanel.transform.Find("Pause status").GetComponent<Text>().text="All clocks are frozen. "+goal;
            tutorialPanel.transform.Find("Pause notice").GetComponent<Text>().text=goal+"  "+capture;
            tutorialPanel.transform.Find("Pause notice").GetComponent<Text>().fontSize=20;
        }
        public RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform; rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(w,h); return rect;
        }
        RectTransform Box(Transform parent,string name,float x,float y,float w,float h,Color color,Sprite sprite=null)
        {
            var r=Rect(parent,name,x,y,w,h); var image=r.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false;
            if(sprite!=null) { image.sprite=sprite; image.type=Image.Type.Sliced; } return r;
        }
        Text Label(Transform parent,string name,string text,float x,float y,float w,float h,int size,Color color,TextAnchor alignment=TextAnchor.MiddleLeft,bool bold=false)
        {
            var r=Rect(parent,name,x,y,w,h); var t=r.gameObject.AddComponent<Text>(); t.font=Font; t.fontSize=size; t.color=color; t.text=text; t.alignment=alignment; t.fontStyle=bold ? FontStyle.Bold : FontStyle.Normal; t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate;
            return t;
        }
        void Title(Transform root,string title,string subtitle,float x,float y,float width)
        {
            var t=Label(root,title,title,x,y,width,44,32,Theme.cream,TextAnchor.MiddleLeft,true); if(Theme.titleFont!=null) t.font=Theme.titleFont;
            Label(root,title+" subtitle",subtitle,x,y+44,width,32,17,muted);
        }
        DinerArtGraphic Art(Transform root,string name,ArtKind kind,float x,float y,float w,float h,Color color)
        { var r=Rect(root,name,x,y,w,h); var a=r.gameObject.AddComponent<DinerArtGraphic>(); a.kind=kind; a.color=color; a.raycastTarget=false; return a; }
        DinerVisual Visual(Transform root,string name,float x,float y,float w,float h)
        {
            var r=Rect(root,name,x,y,w,h); var v=r.gameObject.AddComponent<DinerVisual>();
            v.fallback=Art(r,"Vector placeholder",ArtKind.Ghost,0,0,w,h,Theme.mint);
            var image=Box(r,"Sprite replacement",0,0,w,h,Color.white).GetComponent<Image>(); v.spriteImage=image; image.gameObject.SetActive(false);
            return v;
        }
        DinerHitArea Hit(RectTransform rect,bool source,PayloadType payload,int index,bool target=false,TargetType kind=TargetType.Guest,string tooltip=null)
        {
            var h=rect.gameObject.AddComponent<DinerHitArea>(); h.isSource=source; h.payloadType=payload; h.index=index; h.isTarget=target; h.targetType=kind; h.tooltip=tooltip;
            h.frame=rect.GetComponent<Image>(); if(h.frame!=null) h.restingColor=h.frame.color; hitAreas.Add(h); return h;
        }
        DinerHitArea Button(Transform parent,string text,string command,float x,float y,float w,float h,bool primary=false)
        {
            var r=Box(parent,text,x,y,w,h,primary ? Theme.gold : panel,Theme.panelSprite);
            Label(r,"Label",text,8,0,w-16,h,26,primary ? Theme.ink : Theme.cream,TextAnchor.MiddleCenter,true);
            var area=Hit(r,false,PayloadType.Tool,0); area.command=command; return area;
        }
        Image Progress(Transform root,float x,float y,float w,Color color)
        {
            var outer=Box(root,"Progress track",x,y,w,7,Theme.ink);
            var fill=Box(outer,"Progress fill",0,0,w,7,color).GetComponent<Image>();
            return fill;
        }
        static void Fill(Image image,float value) { image.rectTransform.localScale=new Vector3(Mathf.Clamp01(value),1,1); }
        void Backdrop(Transform parent,bool isKitchen)
        {
            Box(parent,"Wall",0,0,1600,900,isKitchen ? new Color(.12f,.18f,.18f) : new Color(.13f,.2f,.22f));
            for(int i=0;i<16;i++) Box(parent,"Wall timber "+i,i*108,100,3,590,new Color(.25f,.31f,.29f,.3f));
            Box(parent,"Cornice",0,100,1600,12,wood); Box(parent,"Brass cornice",0,112,1600,2,Theme.gold*.7f);
            if(!isKitchen)
            {
                Art(parent,"Four roads",ArtKind.Crossroads,210,150,1180,425,Color.white);
                Box(parent,"Window lintel",196,137,1208,16,wood);
                Box(parent,"Window left",196,145,14,434,wood); Box(parent,"Window right",1390,145,14,434,wood);
                Box(parent,"Window sill",190,562,1220,24,wood);
                Label(parent,"Road sign","NORTH  /  OLD WOODS                         EAST  /  MOON PASS                         SOUTH  /  MARSH                         WEST  /  HOLLOW",237,154,1126,26,12,Theme.cream,TextAnchor.MiddleCenter);
            }
            else
            {
                for(int i=0;i<20;i++) for(int j=0;j<5;j++) Box(parent,"Tile",i*84+(j%2)*42,182+j*69,80,65,new Color(.16f,.23f,.22f));
            }
            var sprite=isKitchen ? Theme.kitchenBackground : Theme.frontBackground;
            if(sprite!=null) { var bg=Box(parent,"Replacement background",0,115,1600,712,Color.white); var im=bg.GetComponent<Image>(); im.sprite=sprite; im.type=Image.Type.Simple; }
            Art(parent,"Left lantern",ArtKind.Lantern,36,135,133,212,Theme.gold);
            Art(parent,"Right lantern",ArtKind.Lantern,1430,135,133,212,Theme.gold);
            var prefab=isKitchen ? Theme.kitchenDecorationPrefab : Theme.frontDecorationPrefab;
            if(prefab!=null) Instantiate(prefab,parent,false);
        }
        void BuildDiscard(Transform parent,string name,float x,float y,float width,float height)
        {
            var waste=Box(parent,name,x,y,width,height,new Color(.24f,.18f,.17f));
            bool hasIcon=Theme.trashIcon!=null;
            if(hasIcon)
            {
                var image=Box(waste,"Trash can",8,10,88,height-20,Color.white).GetComponent<Image>();
                image.sprite=Theme.trashIcon;
                image.type=Image.Type.Simple;
                image.preserveAspect=true;
            }
            float textX=hasIcon ? 102 : 8;
            float textWidth=width-textX-8;
            Label(waste,"Discard title","DISCARD",textX,18,textWidth,36,22,Theme.cream,TextAnchor.MiddleCenter,true);
            Label(waste,"Discard hint","Drop dish",textX,57,textWidth,height-63,20,Theme.cream,TextAnchor.UpperCenter);
            Hit(waste,false,PayloadType.Dish,0,true,TargetType.Waste);
        }

        void BuildFront()
        {
            Backdrop(front,false);
            Label(front,"Room plaque","THE CROSSROADS",580,119,440,42,25,Theme.cream,TextAnchor.MiddleCenter,true);
            guests=new GuestWidget[config.waitingSlots];
            float areaWidth=1120, cell=areaWidth/config.waitingSlots;
            for(int i=0;i<guests.Length;i++)
            {
                float x=240+i*cell;
                var g=new GuestWidget(); guests[i]=g;
                var target=Box(front,"Guest "+(i+1),x,336,cell-20,247,new Color(.1f,.17f,.18f,.35f));
                g.target=Hit(target,false,PayloadType.Tool,i,true,TargetType.Guest,"W - Check guests.");
                g.figure=Rect(target,"Figure",(cell-20-150)/2,0,150,195); g.portrait=Visual(g.figure,"Portrait",0,0,150,195);
                g.slot=Label(target,"Seat", "0"+(i+1)+"  /  EMPTY",0,207,cell-20,28,15,Theme.cream,TextAnchor.MiddleCenter,true);
                var bubble=Box(front,"Order "+(i+1),x-5,187,cell-10,143,Theme.cream,Theme.panelSprite); g.orderRoot=bubble.gameObject;
                g.dish=Visual(bubble,"Recipe icon",12,14,75,75);
                g.recipe=Label(bubble,"Recipe","",92,12,cell-116,55,18,Theme.ink,TextAnchor.MiddleLeft,true);
                g.price=Label(bubble,"Price","",92,66,cell-116,28,22,new Color(.46f,.3f,.15f),TextAnchor.MiddleLeft,true);
                g.ingredients=Label(bubble,"Ingredients","",12,106,cell-34,27,20,Theme.ink,TextAnchor.MiddleCenter);
                var details=Box(front,"Details "+(i+1),x-5,586,cell-10,119,panel,Theme.panelSprite); g.detailsRoot=details.gameObject;
                g.patience=Label(details,"Patience","",13,7,cell-35,26,17,Theme.gold,TextAnchor.MiddleLeft,true);
                g.patienceFill=Progress(details,13,36,cell-36,Theme.mint);
                g.details=Label(details,"Weakness and yield","",13,52,cell-36,60,18,Theme.cream);
                bubble.gameObject.SetActive(false); details.gameObject.SetActive(false);
            }
            var counter=Box(front,"Counter work surface",0,612,1600,214,wood); counterItems=counter.gameObject;
            Box(counter,"Counter brass edge",0,0,1600,5,Theme.gold);
            Label(counter,"Tool heading","COUNTER-ITEMS  /  REUSABLE",52,18,560,25,15,Theme.gold,TextAnchor.MiddleLeft,true);
            tools=new ItemWidget[config.tools.Length];
            float tw=510f/config.tools.Length;
            for(int i=0;i<tools.Length;i++)
            {
                var r=Box(counter,"Tool "+i,48+i*tw,53,tw-12,134,panel,Theme.slotSprite);
                var item=new ItemWidget { visual=Visual(r,"Tool art",(tw-12-66)/2,5,66,78),label=Label(r,"Name",config.tools[i].displayName,4,88,tw-20,36,20,Theme.cream,TextAnchor.MiddleCenter,true) };
                item.hit=Hit(r,true,PayloadType.Tool,i,false,TargetType.Guest,"Hold left mouse, then W to use on a guest."); tools[i]=item;
            }
            Label(counter,"Tray heading","PASS  /  FINISHED DISHES",637,18,500,25,15,Theme.gold,TextAnchor.MiddleLeft,true);
            frontTray=BuildTray(counter,633,53,550,false);
            BuildDiscard(counter,"Discard",1282,66,230,114);
            serviceHint=Label(front,"Service instruction","HOLD + RELEASE ON A GUEST     /     S - Return to the counter     /     A or D - Turn to kitchen",210,737,1180,43,18,Theme.gold,TextAnchor.MiddleCenter).gameObject;
            serviceHint.SetActive(false);
        }
        ItemWidget[] BuildTray(Transform root,float x,float y,float width,bool inKitchen)
        {
            var values=new ItemWidget[config.traySlots]; float cell=width/values.Length;
            for(int i=0;i<values.Length;i++)
            {
                var r=Box(root,"Tray slot "+(i+1),x+i*cell,y,cell-14,134,panel,Theme.slotSprite);
                var v=new ItemWidget { visual=Visual(r,"Dish",10,15,82,87),label=Label(r,"Dish name","EMPTY",101,22,cell-125,62,20,Theme.cream,TextAnchor.MiddleLeft,true),sublabel=Label(r,"Slot label","PASS 0"+(i+1),101,89,cell-125,27,18,muted) };
                v.hit=Hit(r,true,PayloadType.Dish,i,true,TargetType.Tray,inKitchen ? "Shared with the front counter." : "Hold a dish, keep holding, then press W."); values[i]=v;
            }
            return values;
        }
        void BuildKitchen()
        {
            Backdrop(kitchen,true);
            Title(kitchen,"The night kitchen","Process the captured. Cook for the living. Keep the pass moving.",58,140,1300);
            var holding=Box(kitchen,"Holding",48,245,360,292,panel,Theme.panelSprite);
            Label(holding,"Heading","01  /  HOLDING",20,12,310,30,18,Theme.gold,TextAnchor.MiddleLeft,true);
            captures=new ItemWidget[config.capturedSlots]; float cw=324f/captures.Length;
            for(int i=0;i<captures.Length;i++)
            {
                var r=Box(holding,"Captured slot "+i,18+i*cw,55,cw-12,215,new Color(.14f,.23f,.23f),Theme.slotSprite);
                var v=new ItemWidget { visual=Visual(r,"Monster",(cw-12-110)/2,10,110,138),label=Label(r,"Name","EMPTY",4,150,cw-20,27,16,Theme.cream,TextAnchor.MiddleCenter,true),sublabel=Label(r,"Hint","Drag to table",3,184,cw-18,22,13,muted,TextAnchor.MiddleCenter) };
                v.hit=Hit(r,true,PayloadType.Captured,i); captures[i]=v;
            }
            var processing=Box(kitchen,"Processing table",430,245,320,292,panel,Theme.panelSprite);
            Label(processing,"Heading","02  /  PROCESS",19,12,280,30,18,Theme.gold,TextAnchor.MiddleLeft,true);
            if(Theme.processorIcon!=null) { var im=Box(processing,"Station skin",68,48,180,138,Color.white).GetComponent<Image>(); im.sprite=Theme.processorIcon; im.preserveAspect=true; }
            else Art(processing,"Cleaver",ArtKind.Cleaver,85,48,145,138,Theme.mint);
            processor=new ItemWidget { visual=Visual(processing,"Processing guest",107,49,99,131),label=Label(processing,"State","IDLE",18,184,284,30,19,Theme.cream,TextAnchor.MiddleCenter,true),sublabel=Label(processing,"Description","One monster -> two ingredients",8,249,304,25,14,muted,TextAnchor.MiddleCenter) };
            processorFill=Progress(processing,22,225,276,Theme.mint); processor.hit=Hit(processing,false,PayloadType.Captured,0,true,TargetType.Processor);
            var stove=Box(kitchen,"Stove",772,245,336,292,panel,Theme.panelSprite);
            Label(stove,"Heading","03  /  SIMMER",18,12,300,30,18,Theme.gold,TextAnchor.MiddleLeft,true);
            if(Theme.potIcon!=null) { var im=Box(stove,"Stove skin",102,46,136,123,Color.white).GetComponent<Image>(); im.sprite=Theme.potIcon; im.preserveAspect=true; }
            else Art(stove,"Cauldron",ArtKind.Pot,95,44,145,128,Theme.mint);
            pot=new ItemWidget { visual=Visual(stove,"Held dish",127,45,82,88),label=Label(stove,"State","ADD TWO INGREDIENTS",12,180,312,31,17,Theme.cream,TextAnchor.MiddleCenter,true),sublabel=Label(stove,"Description","A valid pair starts automatically",7,252,322,23,13,muted,TextAnchor.MiddleCenter) };
            potFill=Progress(stove,22,225,292,Theme.gold); pot.hit=Hit(stove,false,PayloadType.Ingredient,0,true,TargetType.Pot);
            var loading=Box(stove,"Reserved ingredient",24,72,58,75,new Color(.2f,.29f,.27f));
            reserved=new ItemWidget { visual=Visual(loading,"Ingredient",5,3,48,51),label=Label(loading,"Slot one","1",0,51,58,22,13,Theme.cream,TextAnchor.MiddleCenter) };
            reserved.hit=Hit(loading,true,PayloadType.ReservedIngredient,0,true,TargetType.Pot,"Drag the reserved ingredient back to its matching bin.");
            var slotTwo=Box(stove,"Second ingredient slot",256,72,58,75,new Color(.2f,.29f,.27f));
            Label(slotTwo,"Slot two","+\n2",0,4,58,65,24,Theme.cream,TextAnchor.MiddleCenter);
            Hit(slotTwo,false,PayloadType.Ingredient,0,true,TargetType.Pot);
            var book=Box(kitchen,"Public recipe book",1130,245,422,292,Theme.cream,Theme.panelSprite);
            Label(book,"Heading","THE HOUSE MENU",23,12,377,36,26,Theme.ink,TextAnchor.MiddleLeft,true);
            Label(book,"Public reference","Recipes and ingredients",23,51,377,28,20,new Color(.39f,.41f,.32f));
            float rh=202f/config.recipes.Length;
            recipeIcons=new DinerVisual[config.recipes.Length];
            for(int i=0;i<config.recipes.Length;i++)
            {
                var recipe=config.recipes[i]; recipeIcons[i]=Visual(book,"Recipe "+recipe.id,24,83+i*rh,57,55);
                Label(book,"Name "+recipe.id,recipe.displayName+"  $"+recipe.price,97,80+i*rh,303,29,22,Theme.ink,TextAnchor.MiddleLeft,true);
                Label(book,"Pair "+recipe.id,recipe.first.displayName+" + "+recipe.second.displayName+"   /   "+recipe.cookSeconds+"s",97,108+i*rh,303,29,20,new Color(.25f,.28f,.22f));
            }
            var bottom=Box(kitchen,"Kitchen worktop",0,564,1600,262,wood); Box(bottom,"Brass edge",0,0,1600,5,Theme.gold);
            Label(bottom,"Stock heading","PANTRY  /  DRAG ONE PORTION",51,18,550,28,15,Theme.gold,TextAnchor.MiddleLeft,true);
            bins=new ItemWidget[config.ingredients.Length]; float bw=560f/bins.Length;
            for(int i=0;i<bins.Length;i++)
            {
                var r=Box(bottom,"Ingredient bin "+i,48+i*bw,62,bw-13,159,panel,Theme.slotSprite);
                var v=new ItemWidget { visual=Visual(r,"Ingredient",15,14,63,83),label=Label(r,"Available","2",91,18,bw-110,66,34,Theme.cream,TextAnchor.MiddleCenter,true),sublabel=Label(r,"Name",config.ingredients[i].displayName,8,108,bw-29,32,18,Theme.cream,TextAnchor.MiddleCenter,true) };
                v.hit=Hit(r,true,PayloadType.Ingredient,i,true,TargetType.IngredientBin,"Drag one portion to the pot. Return reserved portions here."); bins[i]=v;
            }
            Label(bottom,"Pass heading","SHARED PASS  /  SERVE AT THE FRONT",660,18,570,28,15,Theme.gold,TextAnchor.MiddleLeft,true);
            kitchenTray=BuildTray(bottom,656,65,596,true);
            BuildDiscard(bottom,"Waste",1292,66,222,134);
            kitchen.gameObject.SetActive(false);
        }
        void BuildHud()
        {
            var header=Box(layoutRoot,"Persistent HUD",0,0,1600,105,Theme.ink);
            Label(header,"Brand","CROSSROADS",45,15,400,39,33,Theme.cream,TextAnchor.MiddleLeft,true);
            Label(header,"Subbrand","D I N E R    /    THE NIGHT SHIFT",47,57,400,25,13,Theme.gold);
            viewText=Label(header,"Current view","01  /  FRONT COUNTER",480,25,370,31,20,Theme.cream,TextAnchor.MiddleLeft,true);
            stockText=Label(header,"Stock","",480,57,490,28,20,Theme.cream);
            timeText=Label(header,"Clock","05:00",1015,10,171,49,36,Theme.cream,TextAnchor.MiddleCenter,true);
            timeFill=Progress(header,1024,68,154,Theme.gold);
            revenueText=Label(header,"Revenue","$0",1230,9,163,49,37,Theme.gold,TextAnchor.MiddleCenter,true);
            Label(header,"Revenue caption","REVENUE",1230,60,163,24,12,muted,TextAnchor.MiddleCenter);
            Button(header,"II  ESC","pause",1430,26,115,48);
            var footer=Box(layoutRoot,"Persistent hints",0,825,1600,75,Theme.ink);
            helpText=Label(footer,"Controls","",43,5,1080,29,18,Theme.cream);
            noticeText=Label(footer,"Feedback","Open the diner when you are ready.",43,37,1100,30,20,Theme.gold);
            kitchenStatus=Label(footer,"Station summary","",1140,5,414,64,18,Theme.cream,TextAnchor.MiddleRight);
        }
        void BuildMenus()
        {
            var ready=Box(layoutRoot,"Ready overlay",0,0,1600,900,new Color(.04f,.08f,.09f,.96f)); readyPanel=ready.gameObject;
            Label(ready,"Edition","A SUPERNATURAL RESTAURANT",360,82,880,38,22,Theme.gold,TextAnchor.MiddleCenter);
            Label(ready,"Title","Crossroads Diner",280,132,1040,98,72,Theme.cream,TextAnchor.MiddleCenter,true);
            Label(ready,"Tagline","Serve some. Hunt some. Turn monsters into meals.",250,238,1100,48,24,muted,TextAnchor.MiddleCenter);
            string[] headings={"01   CHECK & CHOOSE","02   CARRY & CAPTURE","03   PROCESS & COOK"};
            string[] bodies={"W: Read guest orders.\nS: Return to the counter.\nServe the requested dish.","Hold left mouse to pick up.\nKeep holding and press W.\nRelease over a guest.","A / D: Turn to the kitchen.\nDrag captures to the table.\nAdd two ingredients to cook."};
            ArtKind[] arts={ArtKind.Ghost,ArtKind.Garlic,ArtKind.Pot};
            for(int i=0;i<3;i++)
            {
                var card=Box(ready,"Instructions "+i,178+i*426,337,392,332,panel,Theme.panelSprite);
                Art(card,"Illustration",arts[i],154,16,82,78,Theme.mint);
                Label(card,"Heading",headings[i],15,111,362,42,22,Theme.gold,TextAnchor.MiddleCenter,true);
                Label(card,"Description",bodies[i],20,166,352,150,24,Theme.cream,TextAnchor.UpperCenter);
            }
            Button(ready,"START SHIFT","start",591,692,418,70,true);
            Label(ready,"Fine print","5 MINUTES  /  4 WAITING SEATS  /  EVERY GUEST IS A CHOICE",360,783,880,36,20,Theme.cream,TextAnchor.MiddleCenter);
            Label(ready,"Cancellation","Right click cancels a held item. Esc pauses and cancels dragging.",360,831,880,34,20,Theme.cream,TextAnchor.MiddleCenter);
            var pause=Box(layoutRoot,"Pause overlay",0,0,1600,900,new Color(.03f,.06f,.07f,.94f)); pausePanel=pause.gameObject;
            Label(pause,"Title","On a short break",400,140,800,80,55,Theme.cream,TextAnchor.MiddleCenter,true);
            Label(pause,"Pause status","All clocks are paused. Ingredients stay in the pot.",350,230,900,46,26,Theme.cream,TextAnchor.MiddleCenter);
            Label(pause,"Controls","W   Check guests       S   Return to counter\nA / D   Turn left / right       Right click   Cancel drag\nHold left mouse + a view key to carry an item",360,310,880,138,28,Theme.cream,TextAnchor.MiddleCenter);
            Button(pause,"-","quieter",550,487,70,50); Button(pause,"+","louder",980,487,70,50);
            volumeText=Label(pause,"Volume","VOLUME 55%",640,487,320,50,23,Theme.gold,TextAnchor.MiddleCenter);
            Button(pause,"RESUME SHIFT","resume",570,599,460,66,true);
            pause.SetAsLastSibling(); pause.gameObject.SetActive(false);
            var results=Box(layoutRoot,"Results overlay",0,0,1600,900,new Color(.04f,.08f,.09f,.97f)); resultsPanel=results.gameObject;
            Label(results,"Eyebrow","THE LANTERNS ARE OUT",400,74,800,38,17,Theme.gold,TextAnchor.MiddleCenter);
            Label(results,"Title","Shift complete",400,126,800,73,53,Theme.cream,TextAnchor.MiddleCenter,true);
            resultRevenue=Label(results,"Revenue","$0",500,211,600,108,85,Theme.gold,TextAnchor.MiddleCenter,true);
            Label(results,"Revenue caption","TOTAL REVENUE",550,322,500,36,22,Theme.cream,TextAnchor.MiddleCenter);
            resultStats=Label(results,"Statistics","",310,387,980,182,29,Theme.cream,TextAnchor.MiddleCenter);
            resultTelemetry=Label(results,"Playtest metrics","",320,594,960,72,22,Theme.cream,TextAnchor.MiddleCenter);
            Button(results,"PLAY AGAIN","restart",570,697,460,70,true);
            Label(results,"Coda","At the crossroads, every guest is a choice.",370,802,860,40,22,Theme.gold,TextAnchor.MiddleCenter);
            results.gameObject.SetActive(false);
            EnsurePauseTutorial();
        }

        // Additive migration also supports scenes whose presentation was created before the tutorial.
        public void EnsurePauseTutorial()
        {
            if(tutorialPanel!=null) return;
            Button(pausePanel.transform,"HOW TO PLAY","tutorial",570,690,460,60);
            var tutorial=Box(layoutRoot,"Pause tutorial",0,0,1600,900,new Color(.03f,.06f,.07f,.98f));
            tutorialPanel=tutorial.gameObject;
            Label(tutorial,"Title","How to play",300,80,1000,77,54,Theme.cream,TextAnchor.MiddleCenter,true);
            Label(tutorial,"Pause notice","Your shift is paused. Take your time.",300,169,1000,44,26,Theme.gold,TextAnchor.MiddleCenter);

            var controls=Box(tutorial,"Controls reference",70,239,470,453,panel,Theme.panelSprite);
            Label(controls,"Heading","01  /  MOVE & CARRY",24,18,422,42,24,Theme.gold,TextAnchor.MiddleLeft,true);
            Label(controls,"Keys","W: Inspect guest orders\nS: Return to the counter\nA / D: Turn left / right\n\nHold left mouse to pick up.\nKeep holding + a move key\nto carry the item.\nRelease over a valid target.\nRight click: Return the item\nEsc: Back to pause",24,84,422,332,24,Theme.cream,TextAnchor.UpperLeft);

            var loop=Box(tutorial,"Loop reference",565,239,470,453,panel,Theme.panelSprite);
            Label(loop,"Heading","02  /  RUN THE DINER",24,18,422,42,24,Theme.gold,TextAnchor.MiddleLeft,true);
            Label(loop,"Steps","1. W: Read orders and prices.\n    S: Return to pick up items.\n\n2. Carry an item with W.\n    Drop it onto a guest.\n\n3. Captures go to the table.\n    Two ingredients go in the pot.\n\n4. Serve the finished dish.",24,84,422,332,24,Theme.cream,TextAnchor.UpperLeft);

            var reference=Box(tutorial,"Public recipe reference",1060,239,470,453,Theme.cream,Theme.panelSprite);
            Label(reference,"Heading","03  /  POCKET REFERENCE",24,18,422,42,24,Theme.ink,TextAnchor.MiddleLeft,true);
            string recipes=string.Join("\n\n",config.recipes.Select(r=>r.displayName+"\n"+r.first.displayName+" + "+r.second.displayName));
            Label(reference,"Weaknesses",string.Join("\n",config.monsters.Select(m=>m.displayName+" : "+m.weakness.displayName)),24,76,422,114,22,Theme.ink,TextAnchor.UpperLeft);
            Label(reference,"Recipes",recipes,24,206,422,220,22,Theme.ink,TextAnchor.UpperLeft);

            Label(tutorial,"Tips","Orders are visible only up close. Return a reserved ingredient to its bin.\nFull pass? Serve or discard a dish; the pot keeps its finished dish warm.",190,711,1220,62,22,Theme.cream,TextAnchor.MiddleCenter);
            Button(tutorial,"BACK TO PAUSE","tutorial_back",570,795,460,62,true);
            tutorial.gameObject.SetActive(false);
        }
        public void OpenTutorial() { EnsurePauseTutorial(); TutorialOpen=true; }
        public void CloseTutorial() { TutorialOpen=false; }

        public DinerHitArea HitAt(Vector2 position,Func<DinerHitArea,bool> filter)
        {
            for(int i=hitAreas.Count-1;i>=0;i--)
            {
                var h=hitAreas[i];
                if(h==null || (TutorialOpen && !h.transform.IsChildOf(tutorialPanel.transform))) continue;
                if(filter(h) && h.Contains(position)) return h;
            }
            return null;
        }
        public void Refresh(DinerSession s,Vector2 pointer,float volume)
        {
            if(s.State!=ShiftState.Paused) TutorialOpen=false;
            readyPanel.SetActive(s.State==ShiftState.Ready); pausePanel.SetActive(s.State==ShiftState.Paused && !TutorialOpen); resultsPanel.SetActive(s.State==ShiftState.Results);
            if(tutorialPanel!=null) tutorialPanel.SetActive(s.State==ShiftState.Paused && TutorialOpen);
            volumeText.text="VOLUME "+Mathf.RoundToInt(volume*100)+"%";
            timeText.text=TimeSpan.FromSeconds(Math.Ceiling(s.Remaining)).ToString(@"mm\:ss"); Fill(timeFill,s.Remaining/config.shiftDuration);
            timeText.color=s.Remaining<30 ? Theme.danger : Theme.cream; revenueText.text="$"+s.Stats.revenue+" / "+config.revenueTarget;
            revenueText.color=s.TargetReached ? Theme.mint : Theme.gold;
            if(targetStatus!=null) targetStatus.text=s.TargetReached ? "TARGET REACHED" : "REVENUE / TARGET";
            viewText.text=s.View==ViewState.Kitchen ? "03  /  KITCHEN" : s.View==ViewState.FrontService ? "02  /  GUEST SERVICE" : "01  /  FRONT COUNTER";
            stockText.text=string.Join("    ",config.ingredients.Select((d,i)=>d.displayName+" "+s.Available[i]));
            helpText.text=s.View==ViewState.Kitchen ? "A / D - Turn to counter     |     Drag captures to table; ingredients to pot" : s.View==ViewState.FrontService ? "S - Back to counter     |     A / D - Turn to kitchen     |     Release on a guest" : "W - Check guests     |     Hold an item + W to carry it     |     A / D - Turn to kitchen";
            kitchenStatus.text="HOLDING "+s.Captured.Count(c=>c!=null)+"/"+s.Captured.Length+"   /   PASS "+s.Tray.Count(d=>d!=null)+"/"+s.Tray.Length+"\n"+(s.Processing!=null ? "Processing " +s.ProcessingRemaining.ToString("0.0")+"s" : "Table idle")+"   /   "+(s.Pot==PotState.ReadyBlocked ? "Tray full" : s.Pot.ToString());
            bool service=s.View==ViewState.FrontService && !s.Transitioning;
            counterItems.SetActive(!service); serviceHint.SetActive(service);
            for(int i=0;i<guests.Length;i++)
            {
                var w=guests[i]; var g=s.Guests[i]; w.portrait.Show(g?.species);
                w.slot.text="0"+(i+1)+(g!=null ? "  /  WAITING" : "  /  EMPTY");
                bool reveal=s.ShowOrders && g!=null; w.orderRoot.SetActive(reveal); w.detailsRoot.SetActive(reveal);
                if(g!=null)
                {
                    float arrival=Mathf.Clamp01((s.Elapsed-g.arrivedAt)/.6f);
                    float bounce=s.State==ShiftState.Running ? Mathf.Sin(Time.unscaledTime*1.8f+g.id)*3 : 0;
                    w.figure.anchoredPosition=new Vector2((w.target.Rect.rect.width-150)/2+(1-arrival)*(g.road%2==0 ? -80 : 80),-bounce);
                    if(reveal)
                    {
                        w.dish.Show(g.recipe); w.recipe.text=g.recipe.displayName; w.price.text="$"+g.quotedPrice;
                        w.ingredients.text=g.recipe.first.displayName+" + "+g.recipe.second.displayName;
                        w.patience.text=g.patience<10 ? "LEAVING IN "+Mathf.CeilToInt(g.patience)+"s" : "PATIENCE  "+Mathf.CeilToInt(g.patience)+"s";
                        w.patience.color=g.patience<10 ? Theme.danger : Theme.gold; Fill(w.patienceFill,g.patience/g.initialPatience);
                        w.patienceFill.color=g.patience<10 ? Theme.danger : Theme.mint;
                        w.details.text=g.species.displayName+"  /  "+g.species.weakness.displayName+"\nYield: "+g.species.ingredient.displayName+" x"+g.species.ingredientYield;
                    }
                }
            }
            for(int i=0;i<tools.Length;i++) tools[i].visual.Show(config.tools[i]);
            if(recipeIcons!=null) for(int i=0;i<recipeIcons.Length;i++) recipeIcons[i].Show(config.recipes[i]);
            RefreshTray(frontTray,s); RefreshTray(kitchenTray,s);
            for(int i=0;i<captures.Length;i++) { captures[i].visual.Show(s.Captured[i]?.species); captures[i].label.text=s.Captured[i]!=null ? s.Captured[i].species.displayName : "EMPTY"; }
            for(int i=0;i<bins.Length;i++) { bins[i].visual.Show(config.ingredients[i]); bins[i].label.text=s.Available[i].ToString(); }
            processor.visual.Show(s.Processing?.species); processor.label.text=s.Processing!=null ? "PROCESSING  "+s.ProcessingRemaining.ToString("0.0")+"s" : "DROP A CAPTURE HERE";
            Fill(processorFill,s.Processing!=null ? 1-s.ProcessingRemaining/config.processingSeconds : 0);
            pot.visual.Show(s.HeldDish?.recipe); reserved.visual.Show(s.Reserved);
            pot.label.text=s.Pot==PotState.Cooking ? "COOKING  "+s.CookingRemaining.ToString("0.0")+"s" : s.Pot==PotState.ReadyBlocked ? "TRAY FULL - KEPT WARM" : s.Pot==PotState.Loading ? "ADD A MATCHING INGREDIENT" : "ADD TWO INGREDIENTS";
            Fill(potFill,s.Pot==PotState.Cooking ? 1-s.CookingRemaining/s.CookingRecipe.cookSeconds : s.Pot==PotState.ReadyBlocked ? 1 : 0);
            reserved.label.text=s.Reserved!=null ? "RETURN" : "1";
            dragRoot.gameObject.SetActive(s.Drag!=null);
            if(s.Drag!=null)
            {
                dragIcon.Show(s.Drag.definition); dragLabel.text=s.Drag.definition.displayName;
                Vector2 local; RectTransformUtility.ScreenPointToLocalPointInRectangle(layoutRoot,pointer,null,out local);
                dragRoot.anchoredPosition=new Vector2(local.x+800+36,local.y-450-28);
            }
            if(s.State==ShiftState.Results)
            {
                var stats=s.Stats; resultRevenue.text="$"+stats.revenue;
                if(resultOutcome!=null) { resultOutcome.text=s.ShiftSucceeded ? "Target reached!" : "Target missed"; resultOutcome.color=s.ShiftSucceeded ? Theme.mint : Theme.danger; }
                if(resultGoal!=null) resultGoal.text="TARGET $"+config.revenueTarget+(s.ShiftSucceeded ? "  /  SHIFT SUCCESSFUL" : "  /  SHORT BY $"+s.RevenueShortfall);
                resultStats.text="Orders served   "+stats.served+"               Captures   "+stats.captures+"\n\nWalkouts   "+stats.walkouts+"             Unserved at closing   "+stats.closedUnserved+"\n\nDishes discarded   "+stats.discarded;
                resultTelemetry.text="Inspections "+stats.inspections+"   /   Invalid drops "+stats.invalidDrops+"   /   Seed "+config.randomSeed+"\n"+(stats.firstServe>=0 ? "First service: "+stats.firstServe.ToString("0.0")+"s" : "No dishes served this shift.");
            }
        }
        void RefreshTray(ItemWidget[] widgets,DinerSession s)
        {
            for(int i=0;i<widgets.Length;i++)
            {
                var d=s.Tray[i]; var w=widgets[i]; w.visual.Show(d?.recipe); w.label.text=d!=null ? d.recipe.displayName : "EMPTY";
                w.sublabel.text=s.Drag!=null && s.Drag.type==PayloadType.Dish && s.Drag.sourceIndex==i ? "HELD / RESERVED" : "PASS 0"+(i+1);
            }
        }
        public void AnimateView(DinerSession s)
        {
            float from=s.TransitionFrom==ViewState.FrontService ? 1.105f : 1;
            float to=s.TransitionTo==ViewState.FrontService ? 1.105f : 1;
            front.localEulerAngles=kitchen.localEulerAngles=Vector3.zero;
            front.anchoredPosition=kitchen.anchoredPosition=Vector2.zero;
            if(!s.Transitioning)
            {
                front.gameObject.SetActive(s.View!=ViewState.Kitchen); kitchen.gameObject.SetActive(s.View==ViewState.Kitchen);
                float zoom=s.View==ViewState.FrontService ? 1.105f : 1;
                front.localScale=Vector3.one*zoom;
                front.anchoredPosition=s.View==ViewState.FrontService ? new Vector2(-84,28) : Vector2.zero;
                kitchen.localScale=Vector3.one; return;
            }
            float progress=s.TransitionProgress;
            if(s.TurnDirection==0)
            {
                front.gameObject.SetActive(true); kitchen.gameObject.SetActive(false);
                float t=Mathf.SmoothStep(0,1,progress); float zoom=Mathf.Lerp(from,to,t);
                front.localScale=Vector3.one*zoom;
                front.anchoredPosition=new Vector2(-800*(zoom-1),267*(zoom-1)); return;
            }
            // Service first retreats, then performs the requested directional turn.
            float retreat=s.TransitionFrom==ViewState.FrontService ? config.pushSeconds/(config.pushSeconds+config.turnSeconds) : 0;
            if(progress<retreat)
            {
                front.gameObject.SetActive(true); kitchen.gameObject.SetActive(false);
                float zoom=Mathf.Lerp(1.105f,1,Mathf.SmoothStep(0,1,progress/retreat));
                front.localScale=Vector3.one*zoom; front.anchoredPosition=new Vector2(-800*(zoom-1),267*(zoom-1)); return;
            }
            float turn=(progress-retreat)/(1-retreat); float ease=Mathf.SmoothStep(0,1,turn);
            front.localScale=kitchen.localScale=Vector3.one;
            front.gameObject.SetActive(true); kitchen.gameObject.SetActive(true);
            RectTransform outgoing=s.TransitionFrom==ViewState.Kitchen ? kitchen : front, incoming=s.TransitionTo==ViewState.Kitchen ? kitchen : front;
            outgoing.anchoredPosition=new Vector2(-s.TurnDirection*1600*ease,0);
            incoming.anchoredPosition=new Vector2(s.TurnDirection*1600*(1-ease),0);
        }
    }
}
