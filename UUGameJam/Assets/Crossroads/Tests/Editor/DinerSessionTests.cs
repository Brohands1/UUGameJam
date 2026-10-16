using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Crossroads.Diner.Tests
{
    public sealed class DinerSessionTests
    {
        DinerConfig config;
        DinerSession s;
        [SetUp] public void Setup()
        {
            var original=AssetDatabase.LoadAssetAtPath<DinerConfig>(Editor.DinerInstaller.ConfigPath);
            Assert.IsNotNull(original,"Install the Crossroads catalog first.");
            config=UnityEngine.Object.Instantiate(original); s=new DinerSession(config); s.StartShift();
        }
        [TearDown] public void Cleanup()
        { Assert.IsNull(s.CheckInvariants()); UnityEngine.Object.DestroyImmediate(config); }
        void Go(ViewState target)
        {
            if(s.View==target && !s.Transitioning) return;
            if(s.View==ViewState.FrontService && target==ViewState.FrontCounter) s.Navigate('s');
            else if(target==ViewState.FrontService)
            {
                if(s.View==ViewState.Kitchen) { s.Navigate('d'); s.Tick(.4f); }
                s.Navigate('w');
            }
            else s.Navigate('d');
            s.Tick(.61f); Assert.AreEqual(target,s.View);
        }
        ActionResult Add(int index)
        { Assert.IsTrue(s.BeginDrag(PayloadType.Ingredient,index).success); return s.Drop(new DropTarget(TargetType.Pot)); }
        void Cook(int a,int b)
        { Go(ViewState.Kitchen); Assert.IsTrue(Add(a).success); Assert.IsTrue(Add(b).success); s.Tick(4.01f); }
        void Capture(int slot)
        {
            Go(ViewState.FrontCounter); var guest=s.Guests[slot]; Assert.IsNotNull(guest);
            int tool=Array.IndexOf(config.tools,guest.species.weakness);
            Assert.IsTrue(s.BeginDrag(PayloadType.Tool,tool).success); Go(ViewState.FrontService);
            Assert.IsTrue(s.Drop(new DropTarget(TargetType.Guest,slot,guest.id)).success);
        }
        [Test] public void T01_OpeningDefaultsAndReadyClock()
        {
            var ready=new DinerSession(config); ready.Tick(30); Assert.AreEqual(0,ready.Elapsed);
            Assert.AreEqual(3,s.Guests.Count(g=>g!=null)); CollectionAssert.AreEqual(new[]{2,2,2},s.Available);
            Assert.AreEqual("Vampire",s.Guests[0].species.id); Assert.AreEqual("SpectralStew",s.Guests[0].recipe.id);
            Assert.AreEqual("HauntedSoup",s.Guests[1].recipe.id); Assert.AreEqual("BloodStew",s.Guests[2].recipe.id);
            Assert.AreEqual(300,config.shiftDuration); Assert.AreEqual(4,s.Guests.Length); Assert.AreEqual(2,s.Tray.Length);
        }
        [Test] public void V01_V03_OrdersOnlyAppearAfterInspectionWithoutReset()
        {
            var guest=s.Guests[0]; int id=guest.id; var recipe=guest.recipe;
            Assert.IsFalse(s.ShowOrders); s.Navigate('w'); Assert.IsFalse(s.ShowOrders);
            s.Tick(.21f); Assert.IsTrue(s.ShowOrders); s.Tick(2);
            s.Navigate('s'); Assert.IsFalse(s.ShowOrders); s.Tick(.21f);
            Go(ViewState.FrontService); Assert.AreEqual(id,s.Guests[0].id); Assert.AreSame(recipe,s.Guests[0].recipe);
            Assert.Less(s.Guests[0].patience,48); Assert.AreEqual(2,s.Stats.inspections);
        }
        [Test] public void V04_V05_DirectionEndpointsAndIgnoredRepeatedNavigation()
        {
            s.Navigate('a'); Assert.AreEqual(-1,s.TurnDirection); Assert.IsFalse(s.Navigate('d')); s.Tick(.4f); Assert.AreEqual(ViewState.Kitchen,s.View);
            Assert.IsFalse(s.Navigate('w')); Assert.IsFalse(s.Navigate('s'));
            s.Navigate('d'); Assert.AreEqual(1,s.TurnDirection); s.Tick(.4f); Assert.AreEqual(ViewState.FrontCounter,s.View);
            Go(ViewState.FrontService); s.Navigate('a'); s.Tick(.25f); Assert.IsTrue(s.Transitioning); s.Tick(.4f); Assert.AreEqual(ViewState.Kitchen,s.View);
            Assert.IsFalse(s.ShowOrders);
        }
        [Test] public void T04_V07_CaptureCrossViewDoesNotConsumeToolOrProduceEarly()
        {
            Assert.IsTrue(s.BeginDrag(PayloadType.Tool,0).success); var payload=s.Drag;
            Go(ViewState.Kitchen); Assert.AreSame(payload,s.Drag); Go(ViewState.FrontService);
            int id=s.Guests[0].id; Assert.IsTrue(s.Drop(new DropTarget(TargetType.Guest,0,id)).success);
            Assert.IsNull(s.Guests[0]); Assert.AreEqual(1,s.Stats.captures); Assert.AreEqual(1,s.Captured.Count(c=>c!=null));
            Assert.AreEqual(2,s.Available[0]); Assert.AreEqual(GuestState.Captured,s.GuestHistory.First(g=>g.id==id).state);
        }
        [Test] public void V08_ReleaseDuringTransitionReturnsItem()
        {
            Cook(0,1); Go(ViewState.FrontCounter); int id=s.Tray[0].id;
            s.BeginDrag(PayloadType.Dish,0); s.Navigate('w');
            Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,2,s.Guests[2].id)).success);
            Assert.IsNull(s.Drag); Assert.AreEqual(id,s.Tray[0].id); Assert.AreEqual(0,s.Stats.revenue);
        }
        [Test] public void V09_NoRemoteCaptureFromCounterOrKitchen()
        {
            s.BeginDrag(PayloadType.Tool,0); Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,0,s.Guests[0].id)).success);
            s.BeginDrag(PayloadType.Tool,0); Go(ViewState.Kitchen); Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,0,s.Guests[0].id)).success);
            Assert.AreEqual(0,s.Stats.captures);
        }
        [Test] public void T05_V11_WrongToolIsAtomic()
        {
            s.BeginDrag(PayloadType.Tool,1); Go(ViewState.FrontService); var guest=s.Guests[0];
            var patience=s.Guests.Select(g=>g?.patience ?? 0).ToArray();
            Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,0,guest.id)).success); Assert.AreSame(guest,s.Guests[0]);
            CollectionAssert.AreEqual(patience,s.Guests.Select(g=>g?.patience ?? 0).ToArray());
            Assert.AreEqual(0,s.Stats.captures); Assert.AreEqual(0,s.Captured.Count(c=>c!=null));
        }
        [Test] public void T06_CaptureQueueFullKeepsOrder()
        {
            Capture(0); Capture(1); Go(ViewState.FrontCounter); var guest=s.Guests[2];
            s.BeginDrag(PayloadType.Tool,2); Go(ViewState.FrontService);
            float patience=guest.patience;
            var result=s.Drop(new DropTarget(TargetType.Guest,2,guest.id)); Assert.IsFalse(result.success); StringAssert.Contains("full",result.reason);
            Assert.AreEqual(patience,guest.patience);
            Assert.AreSame(guest,s.Guests[2]); Assert.AreEqual(2,s.Stats.captures);
        }
        [Test] public void T07_RepeatedCaptureCannotDuplicate()
        {
            int id=s.Guests[0].id; Capture(0); Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,0,id)).success);
            Assert.AreEqual(1,s.Stats.captures); Assert.AreEqual(1,s.Captured.Count(c=>c!=null));
        }
        [Test] public void T08_T16_ProcessingCompletesOnceInHiddenView()
        {
            Capture(0); Go(ViewState.Kitchen); s.BeginDrag(PayloadType.Captured,0); Assert.IsTrue(s.Drop(new DropTarget(TargetType.Processor)).success);
            Assert.AreEqual(0,s.Captured.Count(c=>c!=null)); Assert.IsNotNull(s.Processing); Assert.AreEqual(2,s.Available[0]);
            Go(ViewState.FrontCounter); s.Tick(3.01f); Assert.AreEqual(4,s.Available[0]); Assert.IsNull(s.Processing); s.Tick(5); Assert.AreEqual(4,s.Available[0]);
        }
        [Test] public void V12_ReserveAndReturnConservesInventory()
        {
            Go(ViewState.Kitchen); Add(0); Assert.AreEqual(1,s.Available[0]); Assert.AreEqual(PotState.Loading,s.Pot);
            s.BeginDrag(PayloadType.ReservedIngredient,0); Assert.IsFalse(s.Drop(new DropTarget(TargetType.IngredientBin,1)).success); Assert.IsNotNull(s.Reserved);
            s.BeginDrag(PayloadType.ReservedIngredient,0); Assert.IsTrue(s.Drop(new DropTarget(TargetType.IngredientBin,0)).success);
            Assert.AreEqual(2,s.Available[0]); Assert.AreEqual(PotState.Idle,s.Pot);
        }
        [Test] public void V13_InvalidPairDoesNotConsumeSecondPortion()
        {
            Go(ViewState.Kitchen); Add(0); Assert.IsFalse(Add(0).success); Assert.AreEqual(1,s.Available[0]); Assert.AreEqual(0,s.Consumed[0]);
            Assert.IsTrue(Add(1).success); Assert.AreEqual(PotState.Cooking,s.Pot); Assert.AreEqual(1,s.Consumed[0]); Assert.AreEqual(1,s.Consumed[1]);
            Assert.IsNull(s.Reserved);
        }
        [Test] public void T09_T10_NoStockAndBusyPotCannotConsume()
        {
            Cook(0,1); Cook(0,1); Assert.AreEqual(0,s.Available[0]); Assert.IsFalse(s.BeginDrag(PayloadType.Ingredient,0).success);
            Assert.AreEqual(0,s.Available[1]); Assert.IsTrue(s.BeginDrag(PayloadType.Ingredient,2).success); s.CancelDrag();
            Assert.AreEqual(2,s.Available[2]);
        }
        [Test] public void T11_T12_V14_HeldTrayRemainsReservedAndBlockedPotRecovers()
        {
            Cook(0,1); Cook(2,1); Cook(0,2); Assert.AreEqual(PotState.ReadyBlocked,s.Pot); int held=s.HeldDish.id;
            s.BeginDrag(PayloadType.Dish,0); int original=s.Tray[0].id; s.Tick(1); Assert.AreEqual(held,s.HeldDish.id); Assert.AreEqual(original,s.Tray[0].id);
            s.CancelDrag(); Assert.AreEqual(original,s.Tray[0].id);
            s.BeginDrag(PayloadType.Dish,0); Assert.IsTrue(s.Drop(new DropTarget(TargetType.Waste)).success);
            Assert.AreEqual(held,s.Tray[0].id); Assert.IsNull(s.HeldDish); Assert.AreEqual(PotState.Idle,s.Pot); Assert.AreEqual(1,s.Stats.discarded);
        }
        [Test] public void T13_WrongDishKeptAndV06_CrossViewServePaysOnce()
        {
            Cook(0,1); Go(ViewState.FrontCounter); int dish=s.Tray[0].id; s.BeginDrag(PayloadType.Dish,0); Go(ViewState.FrontService);
            Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,0,s.Guests[0].id)).success); Assert.AreEqual(dish,s.Tray[0].id);
            Go(ViewState.FrontCounter); s.BeginDrag(PayloadType.Dish,0); Go(ViewState.FrontService); int id=s.Guests[2].id;
            Assert.IsTrue(s.Drop(new DropTarget(TargetType.Guest,2,id)).success); Assert.AreEqual(12,s.Stats.revenue);
            Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,2,id)).success); Assert.AreEqual(12,s.Stats.revenue);
        }
        [Test] public void V10_ReplacementGuestCannotInheritOldDragTarget()
        {
            int old=s.Guests[0].id; s.BeginDrag(PayloadType.Tool,0); Go(ViewState.FrontService); s.Tick(51);
            var fresh=s.Guests[0]; Assert.IsNotNull(fresh); Assert.AreNotEqual(old,fresh.id);
            Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,0,fresh.id)).success); Assert.AreSame(fresh,s.Guests[0]);
            Assert.AreEqual(0,s.Stats.captures);
        }
        [Test] public void T14_ExpiredGuestDoesNotConsumeDish()
        {
            Cook(0,1); Go(ViewState.FrontCounter); s.BeginDrag(PayloadType.Dish,0); int dish=s.Tray[0].id, guest=s.Guests[2].id;
            Go(ViewState.FrontService); s.Tick(51); Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,2,guest)).success);
            Assert.AreEqual(dish,s.Tray[0].id); Assert.AreEqual(0,s.Stats.served);
        }
        [Test] public void V15_T18_PauseCancelsDragButKeepsLoadingAndAllTimers()
        {
            Go(ViewState.Kitchen); Add(0); s.BeginDrag(PayloadType.Ingredient,1); float time=s.Elapsed, patience=s.Guests[0].patience;
            s.Pause(); Assert.IsNull(s.Drag); s.Tick(10); Assert.AreEqual(time,s.Elapsed); Assert.AreEqual(patience,s.Guests[0].patience); Assert.IsNotNull(s.Reserved);
            s.Resume(); Add(1); s.Pause(); float cooking=s.CookingRemaining; s.Tick(10); Assert.AreEqual(cooking,s.CookingRemaining);
        }
        [Test] public void T19_ClosingBoundaryFreezesResultsAndRejectsTransaction()
        {
            Cook(0,1); Go(ViewState.FrontCounter); s.BeginDrag(PayloadType.Dish,0); Go(ViewState.FrontService);
            int guest=s.Guests[2].id; int waiting=s.Guests.Count(g=>g!=null); s.Tick(300-s.Elapsed);
            Assert.AreEqual(ShiftState.Results,s.State); Assert.AreEqual(waiting,s.Stats.closedUnserved); Assert.IsNull(s.Drag);
            var json=JsonUtility.ToJson(s.Stats); Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,2,guest)).success); s.Tick(10); Assert.AreEqual(json,JsonUtility.ToJson(s.Stats));
        }
        [Test] public void T02_T03_IndependentArrivalsAndSinglePendingRefill()
        {
            s.Tick(10.01f); Assert.AreEqual(4,s.Guests.Count(g=>g!=null)); s.Tick(10.01f); Assert.AreEqual(4,s.Stats.arrivals);
            Capture(0); Assert.IsNull(s.Guests[0]); s.Tick(.5f); Assert.IsNull(s.Guests[0]); s.Tick(.51f); Assert.IsNotNull(s.Guests[0]);
            Assert.AreEqual(5,s.Stats.arrivals); Assert.AreEqual(0,s.Stats.served);
        }
        [Test] public void V16_KitchenDoesNotRevealOrders()
        { Go(ViewState.FrontService); Assert.IsTrue(s.ShowOrders); s.Navigate('d'); Assert.IsFalse(s.ShowOrders); s.Tick(.6f); Assert.IsFalse(s.ShowOrders); }
        [Test] public void DishMovePreservesInstanceAndNeverPays()
        {
            Cook(0,1); int id=s.Tray[0].id; s.BeginDrag(PayloadType.Dish,0); Assert.IsTrue(s.Drop(new DropTarget(TargetType.Tray,1)).success);
            Assert.IsNull(s.Tray[0]); Assert.AreEqual(id,s.Tray[1].id); Assert.AreEqual(0,s.Stats.revenue);
        }
        [Test] public void PhaseAppliesOnlyToNewGuests()
        {
            config.phases=new[]{new ArrivalPhase(0,2,50),new ArrivalPhase(3,2,40)};
            s=new DinerSession(config); s.StartShift(); s.Tick(2.01f); var old=s.Guests[3]; Assert.AreEqual(50,old.initialPatience);
            s.Tick(2); Assert.AreEqual(50,old.initialPatience); Assert.Greater(old.patience,47);
        }
        [Test] public void T20_RestartClearsReservationsTransactionsAndHistory()
        {
            for(int round=0;round<3;round++)
            {
                Go(ViewState.Kitchen); Add(0); s.BeginDrag(PayloadType.Ingredient,1); s.Tick(300);
                s.StartShift(); Assert.AreEqual(0,s.Elapsed); Assert.AreEqual(3,s.Stats.arrivals); Assert.AreEqual(0,s.Stats.revenue);
                Assert.IsNull(s.Drag); Assert.IsNull(s.Reserved); Assert.AreEqual(PotState.Idle,s.Pot); Assert.AreEqual(3,s.GuestHistory.Count);
                CollectionAssert.AreEqual(new[]{2,2,2},s.Available);
            }
        }
        [TestCase(99,false,1)]
        [TestCase(100,true,0)]
        [TestCase(114,true,0)]
        public void RevenueTargetIsEvaluatedAtClosingAndResets(int payment,bool success,int shortfall)
        {
            Assert.AreEqual(100,config.revenueTarget);
            Cook(0,1); Go(ViewState.FrontCounter);
            var guest=s.Guests[2]; guest.quotedPrice=payment;
            s.BeginDrag(PayloadType.Dish,0); Go(ViewState.FrontService);
            var other=s.Guests[0]; float patience=other.patience;
            Assert.IsTrue(s.Drop(new DropTarget(TargetType.Guest,2,guest.id)).success);
            Assert.AreEqual(patience,other.patience,"Serving must not frighten other guests.");
            Assert.AreEqual(success,s.TargetReached); Assert.IsFalse(s.ShiftSucceeded);
            Assert.AreEqual(ShiftState.Running,s.State,"Reaching the target must not end the shift early.");
            s.Tick(s.Remaining);
            Assert.AreEqual(success,s.ShiftSucceeded); Assert.AreEqual(shortfall,s.RevenueShortfall);
            Assert.IsNull(s.CheckInvariants());
            s.StartShift(); Assert.IsFalse(s.TargetReached); Assert.IsFalse(s.ShiftSucceeded); Assert.AreEqual(100,s.RevenueShortfall);
        }
        [TestCase(5f)]
        [TestCase(0f)]
        [TestCase(8f)]
        public void SuccessfulCaptureAppliesConfiguredPenaltyExactlyOnce(float penalty)
        {
            config.capturePatiencePenalty=penalty;
            s.BeginDrag(PayloadType.Tool,0); Go(ViewState.FrontService);
            var target=s.Guests[0]; var witnesses=s.Guests.Where(g=>g!=null && g!=target).ToArray();
            var before=witnesses.Select(g=>g.patience).ToArray(); float targetBefore=target.patience;
            Assert.IsTrue(s.CanDrop(new DropTarget(TargetType.Guest,0,target.id)).success);
            Assert.IsTrue(s.Drop(new DropTarget(TargetType.Guest,0,target.id)).success);
            Assert.AreEqual(targetBefore,target.patience); Assert.AreEqual(GuestState.Captured,target.state);
            for(int i=0;i<witnesses.Length;i++) Assert.AreEqual(before[i]-penalty,witnesses[i].patience,.001f);
            Assert.IsFalse(s.Drop(new DropTarget(TargetType.Guest,0,target.id)).success);
            for(int i=0;i<witnesses.Length;i++) Assert.AreEqual(before[i]-penalty,witnesses[i].patience,.001f);
            s.Pause(); s.Tick(30);
            for(int i=0;i<witnesses.Length;i++) Assert.AreEqual(before[i]-penalty,witnesses[i].patience,.001f);
        }
        [Test] public void CaptureCanCauseImmediateWalkoutAndLowPatienceWarning()
        {
            config.capturePatiencePenalty=5;
            s.BeginDrag(PayloadType.Tool,0); Go(ViewState.FrontService);
            var leaving=s.Guests[1]; var warning=s.Guests[2];
            leaving.patience=4; warning.patience=12;
            Assert.IsTrue(s.Drop(new DropTarget(TargetType.Guest,0,s.Guests[0].id)).success);
            Assert.AreEqual(0,leaving.patience); Assert.AreEqual(GuestState.WalkedOut,leaving.state);
            Assert.IsNull(s.Guests[1]); Assert.AreEqual(1,s.Stats.walkouts);
            Assert.AreEqual(7,warning.patience); Assert.IsTrue(warning.warned);
            s.Tick(.1f); Assert.AreEqual(1,s.Stats.walkouts);
            s.Tick(10); var newcomer=s.Guests.First(g=>g!=null);
            Assert.AreEqual(config.Phase(s.Elapsed).patience,newcomer.patience,"New arrivals must not inherit past capture penalties.");
        }
        [Test] public void CapturePenaltiesAccumulateForRemainingWitnesses()
        {
            Capture(0); Go(ViewState.FrontCounter);
            s.BeginDrag(PayloadType.Tool,1); Go(ViewState.FrontService);
            var witness=s.Guests[2]; float patience=witness.patience;
            Assert.IsTrue(s.Drop(new DropTarget(TargetType.Guest,1,s.Guests[1].id)).success);
            Assert.AreEqual(patience-config.capturePatiencePenalty,witness.patience,.001f);
            Assert.AreEqual(2,s.Stats.captures);
        }
        [Test] public void T21_SeededFuzzMaintainsAllConservationLawsForThreeFullShifts()
        {
            var rng=new System.Random(713);
            for(int round=0;round<3;round++)
            {
                s.StartShift();
                for(int step=0;step<3001 && s.State==ShiftState.Running;step++)
                {
                    s.BeginFrame(.1f);
                    if(rng.Next(7)==0) s.Navigate("wasd"[rng.Next(4)]);
                    if(s.Drag==null)
                    {
                        var kind=(PayloadType)rng.Next(5); int count=kind==PayloadType.Tool ? 3 : kind==PayloadType.Ingredient ? 3 : 2;
                        s.BeginDrag(kind,rng.Next(count));
                    }
                    else if(rng.Next(4)==0) s.CancelDrag();
                    else
                    {
                        var kind=(TargetType)rng.Next(6); int slot=rng.Next(4); int id=s.Guests[slot]?.id ?? 0;
                        s.Drop(new DropTarget(kind,slot,id));
                    }
                    s.EndFrame(.1f); Assert.IsNull(s.CheckInvariants(),"Round "+round+", step "+step);
                }
                Assert.AreEqual(ShiftState.Results,s.State);
            }
        }
    }
}
