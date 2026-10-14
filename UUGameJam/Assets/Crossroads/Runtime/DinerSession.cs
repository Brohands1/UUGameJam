using System;
using System.Collections.Generic;
using System.Linq;

namespace Crossroads.Diner
{
    public enum ShiftState { Ready, Running, Paused, Results }
    public enum ViewState { FrontCounter, FrontService, Kitchen }
    public enum GuestState { Waiting, Served, Captured, WalkedOut, ClosedUnserved }
    public enum PotState { Idle, Loading, Cooking, ReadyBlocked }
    public enum PayloadType { Tool, Dish, Captured, Ingredient, ReservedIngredient }
    public enum TargetType { Guest, Processor, Pot, IngredientBin, Tray, Waste }
    public enum SoundCue { Arrival, Capture, Invalid, Processed, Cooked, Paid, Walkout }

    [Serializable] public sealed class Guest
    {
        public int id, slot, road, quotedPrice;
        public MonsterDefinition species;
        public RecipeDefinition recipe;
        public float patience, initialPatience, arrivedAt;
        public bool warned;
        public GuestState state;
    }
    [Serializable] public sealed class CapturedMonster
    { public int id, sourceCustomerId; public MonsterDefinition species; }
    [Serializable] public sealed class Dish
    { public int id; public RecipeDefinition recipe; }
    [Serializable] public sealed class RunStatistics
    {
        public int revenue, served, captures, walkouts, closedUnserved, discarded, arrivals, cooked, invalidDrops, inspections;
        public float firstInspection=-1, firstServe=-1, firstCapture=-1;
    }
    public sealed class DragPayload
    {
        public PayloadType type;
        public int sourceIndex, instanceId;
        public DinerDefinition definition;
        public int[] guestIdsAtPickup;
    }
    public struct DropTarget
    {
        public TargetType type;
        public int index, instanceId;
        public DropTarget(TargetType kind, int slot=0, int id=0) { type=kind; index=slot; instanceId=id; }
    }
    public struct ActionResult
    {
        public bool success;
        public string reason;
        public ActionResult(bool ok,string text) { success=ok; reason=text; }
    }

    // Authoritative simulation. No UI object, coroutine, or scene visibility owns business time.
    public sealed class DinerSession
    {
        public readonly DinerConfig Config;
        public ShiftState State { get; private set; } = ShiftState.Ready;
        public ViewState View { get; private set; } = ViewState.FrontCounter;
        public ViewState TransitionFrom { get; private set; }
        public ViewState TransitionTo { get; private set; }
        public bool Transitioning { get; private set; }
        public int TurnDirection { get; private set; }
        public float TransitionProgress { get; private set; }
        public float Elapsed { get; private set; }
        public float Remaining => Math.Max(0,Config.shiftDuration-Elapsed);
        public Guest[] Guests { get; private set; }
        public CapturedMonster[] Captured { get; private set; }
        public Dish[] Tray { get; private set; }
        public int[] Available { get; private set; }
        public int[] Produced { get; private set; }
        public int[] Consumed { get; private set; }
        public CapturedMonster Processing { get; private set; }
        public float ProcessingRemaining { get; private set; }
        public PotState Pot { get; private set; }
        public IngredientDefinition Reserved { get; private set; }
        public RecipeDefinition CookingRecipe { get; private set; }
        public float CookingRemaining { get; private set; }
        public Dish HeldDish { get; private set; }
        public float Cooldown { get; private set; }
        public DragPayload Drag { get; private set; }
        public RunStatistics Stats { get; private set; }
        public readonly List<Guest> GuestHistory = new List<Guest>();
        public bool ShowOrders => View==ViewState.FrontService && !Transitioning && (State==ShiftState.Running || State==ShiftState.Paused);
        public event Action<string,SoundCue> Feedback;
        int nextId=1;
        float arrivalTimer, refillTimer, transitionDuration;
        bool arrivalPending;
        Random random;
        readonly int seed;
        readonly Queue<int> speciesBag=new Queue<int>(), recipeBag=new Queue<int>();

        public DinerSession(DinerConfig config, int? seed=null)
        {
            var error=config.ValidateConfiguration();
            if(error!=null) throw new ArgumentException(error);
            Config=config; this.seed=seed ?? config.randomSeed; Reset();
        }
        void Reset()
        {
            random=new Random(seed);
            Guests=new Guest[Config.waitingSlots]; Captured=new CapturedMonster[Config.capturedSlots]; Tray=new Dish[Config.traySlots];
            Available=Enumerable.Repeat(Config.startingStock,Config.ingredients.Length).ToArray();
            Produced=new int[Available.Length]; Consumed=new int[Available.Length];
            Stats=new RunStatistics(); GuestHistory.Clear(); speciesBag.Clear(); recipeBag.Clear();
            Elapsed=0; Cooldown=0; Processing=null; ProcessingRemaining=0; Pot=PotState.Idle;
            Reserved=null; CookingRecipe=null; CookingRemaining=0; HeldDish=null; Drag=null;
            View=ViewState.FrontCounter; Transitioning=false; TransitionProgress=0;
            arrivalTimer=Config.Phase(0).interval; arrivalPending=false; refillTimer=Config.fullAreaRefillDelay;
        }
        public void StartShift()
        {
            if(State==ShiftState.Running) return;
            Reset(); State=ShiftState.Running;
            if(Config.openingGuests!=null) for(int i=0;i<Math.Min(Guests.Length,Config.openingGuests.Length);i++)
                Spawn(i,Config.openingGuests[i].species,Config.openingGuests[i].recipe);
            Notify("W - Check guests. Orders are visible only up close.",SoundCue.Arrival);
        }
        public void Pause()
        { if(State==ShiftState.Running) { CancelDrag(); State=ShiftState.Paused; } }
        public void Resume() { if(State==ShiftState.Paused) State=ShiftState.Running; }
        public void CancelDrag() { Drag=null; }

        public bool Navigate(char key)
        {
            if(State!=ShiftState.Running || Transitioning) return false;
            ViewState target=View;
            int direction=0;
            if(key=='w' && View==ViewState.FrontCounter) target=ViewState.FrontService;
            if(key=='s' && View==ViewState.FrontService) target=ViewState.FrontCounter;
            if(key=='a' || key=='d') { target=View==ViewState.Kitchen ? ViewState.FrontCounter : ViewState.Kitchen; direction=key=='a' ? -1 : 1; }
            if(target==View) return false;
            TransitionFrom=View; TransitionTo=target; TurnDirection=direction;
            TransitionProgress=0; Transitioning=true;
            transitionDuration=direction==0 ? Config.pushSeconds : Config.turnSeconds+(View==ViewState.FrontService ? Config.pushSeconds : 0);
            return true;
        }
        // Host runs BeginFrame -> player commands -> EndFrame, preserving the boundary order.
        public void BeginFrame(float delta)
        {
            if(State!=ShiftState.Running || delta<=0) return;
            Elapsed=Math.Min(Config.shiftDuration,Elapsed+delta);
            if(Elapsed>=Config.shiftDuration) { Close(); return; }
            if(Transitioning)
            {
                TransitionProgress=Math.Min(1,TransitionProgress+delta/transitionDuration);
                if(TransitionProgress>=1)
                {
                    View=TransitionTo; Transitioning=false;
                    if(View==ViewState.FrontService) { Stats.inspections++; if(Stats.firstInspection<0) Stats.firstInspection=Elapsed; }
                }
            }
            Cooldown=Math.Max(0,Cooldown-delta);
            for(int i=0;i<Guests.Length;i++)
            {
                var g=Guests[i]; if(g==null) continue;
                g.patience=Math.Max(0,g.patience-delta);
                if(g.patience<=0) { g.state=GuestState.WalkedOut; Guests[i]=null; Stats.walkouts++; Notify("A guest walked out.",SoundCue.Walkout); }
                else if(g.patience<10 && !g.warned) { g.warned=true; Notify("Guest leaving soon!",SoundCue.Walkout); }
            }
            if(Processing!=null)
            {
                ProcessingRemaining=Math.Max(0,ProcessingRemaining-delta);
                if(ProcessingRemaining<=0)
                {
                    int i=IngredientIndex(Processing.species.ingredient), count=Processing.species.ingredientYield;
                    Available[i]+=count; Produced[i]+=count; Processing=null;
                    Notify("Processing complete. Ingredients stocked.",SoundCue.Processed);
                }
            }
            if(Pot==PotState.Cooking)
            {
                CookingRemaining=Math.Max(0,CookingRemaining-delta);
                if(CookingRemaining<=0)
                {
                    HeldDish=new Dish { id=nextId++,recipe=CookingRecipe }; Stats.cooked++;
                    Pot=PotState.ReadyBlocked; Notify("Dish ready.",SoundCue.Cooked);
                }
            }
            FlushPot();
        }
        public void EndFrame(float delta)
        {
            if(State!=ShiftState.Running || delta<=0) return;
            int free=Array.FindIndex(Guests,g=>g==null);
            if(arrivalPending)
            {
                if(free<0) { refillTimer=Config.fullAreaRefillDelay; return; }
                refillTimer-=delta;
                if(refillTimer>0) return;
                arrivalPending=false; SpawnRandom(free); arrivalTimer=Config.Phase(Elapsed).interval; return;
            }
            arrivalTimer-=delta;
            if(arrivalTimer>0) return;
            if(free<0) { arrivalPending=true; refillTimer=Config.fullAreaRefillDelay; return; }
            SpawnRandom(free); arrivalTimer=Config.Phase(Elapsed).interval;
        }
        public void Tick(float delta) { BeginFrame(delta); EndFrame(delta); }
        void Close()
        {
            CancelDrag(); Transitioning=false; State=ShiftState.Results;
            foreach(var g in Guests) if(g!=null) { g.state=GuestState.ClosedUnserved; Stats.closedUnserved++; }
            Array.Clear(Guests,0,Guests.Length);
        }
        void SpawnRandom(int slot)
        { Spawn(slot,Config.monsters[TakeBag(speciesBag,Config.monsters.Length)],Config.recipes[TakeBag(recipeBag,Config.recipes.Length)]); Notify("A new guest has arrived.",SoundCue.Arrival); }
        int TakeBag(Queue<int> bag,int count)
        {
            if(bag.Count==0)
            {
                int[] values=Enumerable.Range(0,count).ToArray();
                for(int i=values.Length-1;i>0;i--) { int j=random.Next(i+1); int swap=values[i]; values[i]=values[j]; values[j]=swap; }
                foreach(int value in values) bag.Enqueue(value);
            }
            return bag.Dequeue();
        }
        void Spawn(int slot,MonsterDefinition species,RecipeDefinition recipe)
        {
            float patience=Config.Phase(Elapsed).patience;
            var g=new Guest { id=nextId++,slot=slot,road=random.Next(4),species=species,recipe=recipe,quotedPrice=recipe.price,patience=patience,initialPatience=patience,arrivedAt=Elapsed,state=GuestState.Waiting };
            Guests[slot]=g; GuestHistory.Add(g); Stats.arrivals++;
        }
        public int IngredientIndex(IngredientDefinition ingredient) => Array.IndexOf(Config.ingredients,ingredient);
        public ActionResult BeginDrag(PayloadType type,int source)
        {
            if(State!=ShiftState.Running || Transitioning || Drag!=null) return new ActionResult(false,"Not available.");
            DinerDefinition definition=null; int instance=0;
            switch(type)
            {
                case PayloadType.Tool:
                    if(View!=ViewState.FrontCounter || source<0 || source>=Config.tools.Length) return new ActionResult(false,"Take tools from the counter.");
                    definition=Config.tools[source]; break;
                case PayloadType.Dish:
                    if(View==ViewState.FrontService || source<0 || source>=Tray.Length || Tray[source]==null) return new ActionResult(false,"Take dishes from the counter.");
                    definition=Tray[source].recipe; instance=Tray[source].id; break;
                case PayloadType.Captured:
                    if(View!=ViewState.Kitchen || source<0 || source>=Captured.Length || Captured[source]==null) return new ActionResult(false,"Empty holding slot.");
                    definition=Captured[source].species; instance=Captured[source].id; break;
                case PayloadType.Ingredient:
                    if(View!=ViewState.Kitchen || source<0 || source>=Available.Length || Available[source]<=0) return Fail("Not enough ingredients.");
                    definition=Config.ingredients[source]; break;
                case PayloadType.ReservedIngredient:
                    if(View!=ViewState.Kitchen || Pot!=PotState.Loading || Reserved==null) return new ActionResult(false,"No reserved ingredient.");
                    definition=Reserved; break;
            }
            Drag=new DragPayload { type=type,sourceIndex=source,instanceId=instance,definition=definition,guestIdsAtPickup=Guests.Select(g=>g==null ? 0 : g.id).ToArray() };
            return new ActionResult(true,"Hold left mouse. W / A / D to carry between views.");
        }
        public ActionResult CanDrop(DropTarget target)
        {
            if(Drag==null) return new ActionResult(false,"Nothing held.");
            if(State!=ShiftState.Running) return new ActionResult(false,"Shift is not running.");
            if(Transitioning) return new ActionResult(false,"Wait for the view to settle.");
            if(target.type==TargetType.Guest)
            {
                if(View!=ViewState.FrontService) return new ActionResult(false,"W - Check guests before serving.");
                if(target.index<0 || target.index>=Guests.Length) return new ActionResult(false,"Guest gone.");
                var g=Guests[target.index];
                if(g==null || g.id!=target.instanceId || g.id!=Drag.guestIdsAtPickup[target.index] || g.state!=GuestState.Waiting || g.patience<=0) return new ActionResult(false,"Guest gone. Pick up again.");
                if(Drag.type==PayloadType.Tool)
                {
                    if(g.species.weakness!=Drag.definition) return new ActionResult(false,"Wrong counter-item.");
                    if(Cooldown>0) return new ActionResult(false,"Counter-items cooling down.");
                    if(Array.FindIndex(Captured,c=>c==null)<0) return new ActionResult(false,"Capture queue full.");
                    return new ActionResult(true,"Release to capture.");
                }
                if(Drag.type==PayloadType.Dish)
                {
                    if(!ValidDraggedDish()) return new ActionResult(false,"Dish gone.");
                    if(g.recipe!=Drag.definition) return new ActionResult(false,"Wrong dish.");
                    return new ActionResult(true,"Release to serve.");
                }
            }
            if(target.type==TargetType.Processor && View==ViewState.Kitchen && Drag.type==PayloadType.Captured)
            {
                if(Processing!=null) return new ActionResult(false,"Processing station busy.");
                if(Captured[Drag.sourceIndex]==null || Captured[Drag.sourceIndex].id!=Drag.instanceId) return new ActionResult(false,"Capture gone.");
                return new ActionResult(true,"Release to process.");
            }
            if(target.type==TargetType.Pot && View==ViewState.Kitchen && Drag.type==PayloadType.Ingredient)
            {
                if(Pot==PotState.Cooking || Pot==PotState.ReadyBlocked) return new ActionResult(false,Pot==PotState.ReadyBlocked ? "Tray full." : "Cooking...");
                int index=IngredientIndex((IngredientDefinition)Drag.definition);
                if(Available[index]<1) return new ActionResult(false,"Not enough ingredients.");
                if(Reserved!=null && FindRecipe(Reserved,(IngredientDefinition)Drag.definition)==null) return new ActionResult(false,"Those ingredients do not make a recipe.");
                return new ActionResult(true,Reserved==null ? "Release to reserve one ingredient." : "Release to start cooking.");
            }
            if(target.type==TargetType.IngredientBin && View==ViewState.Kitchen && Drag.type==PayloadType.ReservedIngredient)
            {
                if(Pot!=PotState.Loading || Reserved!=Drag.definition || target.index!=IngredientIndex(Reserved)) return new ActionResult(false,"Return to the matching ingredient bin.");
                return new ActionResult(true,"Release to return ingredient.");
            }
            if((target.type==TargetType.Tray || target.type==TargetType.Waste) && View!=ViewState.FrontService && Drag.type==PayloadType.Dish)
            {
                if(!ValidDraggedDish()) return new ActionResult(false,"Dish gone.");
                if(target.type==TargetType.Tray && (target.index<0 || target.index>=Tray.Length || Tray[target.index]!=null)) return new ActionResult(false,"That tray slot is occupied.");
                return new ActionResult(true,target.type==TargetType.Waste ? "Release to discard dish." : "Release to move dish.");
            }
            return new ActionResult(false,"Invalid target. Item returned.");
        }
        bool ValidDraggedDish() => Drag.sourceIndex>=0 && Drag.sourceIndex<Tray.Length && Tray[Drag.sourceIndex]!=null && Tray[Drag.sourceIndex].id==Drag.instanceId;
        public ActionResult Drop(DropTarget target)
        {
            var check=CanDrop(target);
            if(!check.success) { CancelDrag(); return Fail(check.reason); }
            var payload=Drag;
            if(target.type==TargetType.Guest)
            {
                var g=Guests[target.index];
                if(payload.type==PayloadType.Tool)
                {
                    int slot=Array.FindIndex(Captured,c=>c==null);
                    Captured[slot]=new CapturedMonster { id=nextId++,sourceCustomerId=g.id,species=g.species };
                    g.state=GuestState.Captured; Guests[target.index]=null; Stats.captures++; Cooldown=Config.captureCooldown;
                    if(Stats.firstCapture<0) Stats.firstCapture=Elapsed;
                    Notify("Captured. Take it to the processing table.",SoundCue.Capture);
                }
                else
                {
                    Tray[payload.sourceIndex]=null; g.state=GuestState.Served; Guests[target.index]=null;
                    Stats.served++; Stats.revenue+=g.quotedPrice;
                    if(Stats.firstServe<0) { Stats.firstServe=Elapsed; Notify("Paid! Capture guests to replenish your ingredients.",SoundCue.Paid); }
                    else Notify("Paid +$"+g.quotedPrice+". Thank you for dining.",SoundCue.Paid);
                }
            }
            else if(target.type==TargetType.Processor)
            {
                Processing=Captured[payload.sourceIndex]; Captured[payload.sourceIndex]=null; ProcessingRemaining=Config.processingSeconds;
                Notify("Processing...",SoundCue.Capture);
            }
            else if(target.type==TargetType.Pot)
            {
                var ingredient=(IngredientDefinition)payload.definition;
                int index=IngredientIndex(ingredient);
                if(Reserved==null) { Available[index]--; Reserved=ingredient; Pot=PotState.Loading; }
                else
                {
                    var recipe=FindRecipe(Reserved,ingredient);
                    Available[index]--; Consumed[IngredientIndex(Reserved)]++; Consumed[index]++;
                    Reserved=null; CookingRecipe=recipe; CookingRemaining=recipe.cookSeconds; Pot=PotState.Cooking;
                    Notify("Cooking...",SoundCue.Processed);
                }
            }
            else if(target.type==TargetType.IngredientBin)
            { Available[IngredientIndex(Reserved)]++; Reserved=null; Pot=PotState.Idle; }
            else if(target.type==TargetType.Tray)
            { Tray[target.index]=Tray[payload.sourceIndex]; Tray[payload.sourceIndex]=null; }
            else if(target.type==TargetType.Waste)
            { Tray[payload.sourceIndex]=null; Stats.discarded++; Notify("Dish discarded. Tray space freed.",SoundCue.Invalid); }
            Drag=null; FlushPot(); return new ActionResult(true,check.reason);
        }
        public ActionResult InvalidRelease() { CancelDrag(); return Fail("Invalid target. Item returned."); }
        RecipeDefinition FindRecipe(IngredientDefinition a,IngredientDefinition b) => Array.Find(Config.recipes,r=>r.Matches(a,b));
        void FlushPot()
        {
            if(State!=ShiftState.Running || Pot!=PotState.ReadyBlocked || HeldDish==null) return;
            int free=Array.FindIndex(Tray,d=>d==null); if(free<0) return;
            Tray[free]=HeldDish; HeldDish=null; CookingRecipe=null; Pot=PotState.Idle;
        }
        ActionResult Fail(string reason)
        {
            if(State==ShiftState.Running) { Stats.invalidDrops++; Notify(reason,SoundCue.Invalid); }
            return new ActionResult(false,reason);
        }
        void Notify(string message,SoundCue cue) { Feedback?.Invoke(message,cue); }

        public string CheckInvariants()
        {
            if(Stats.arrivals!=Stats.served+Stats.captures+Stats.walkouts+Stats.closedUnserved+Guests.Count(g=>g!=null)) return "Guest conservation failed.";
            for(int i=0;i<Available.Length;i++)
            {
                int reserved=Reserved==Config.ingredients[i] ? 1 : 0;
                if(Available[i]<0 || Config.startingStock+Produced[i]!=Available[i]+reserved+Consumed[i]) return "Ingredient conservation failed: "+Config.ingredients[i].id;
            }
            if(Stats.cooked!=Stats.served+Stats.discarded+Tray.Count(d=>d!=null)+(HeldDish!=null ? 1 : 0)) return "Dish conservation failed.";
            if(GuestHistory.Where(g=>g.state==GuestState.Served).Sum(g=>g.quotedPrice)!=Stats.revenue) return "Revenue mismatch.";
            if(Tray.Where(d=>d!=null).Select(d=>d.id).Distinct().Count()!=Tray.Count(d=>d!=null)) return "Duplicate dish instance.";
            return null;
        }
    }
}
