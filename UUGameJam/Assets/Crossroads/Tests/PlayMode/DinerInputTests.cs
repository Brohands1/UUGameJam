using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Crossroads.Diner.Tests
{
    public sealed class DinerInputTests
    {
        Mouse mouse;
        Keyboard keyboard;
        DinerGame game;
        Vector2 cursor;
        bool held;
        DinerSession S => game.Session;
        DinerPresentation UI => game.presentation;
        [UnitySetUp] public IEnumerator Setup()
        {
            SceneManager.LoadScene("CrossroadsDiner"); yield return null; yield return null;
            game=Object.FindFirstObjectByType<DinerGame>(); Assert.IsNotNull(game);
            mouse=InputSystem.AddDevice<Mouse>(); keyboard=InputSystem.AddDevice<Keyboard>();
            mouse.MakeCurrent(); keyboard.MakeCurrent();
            yield return Click(UI.hitAreas.First(h=>h.command=="start").Rect);
            Assert.AreEqual(ShiftState.Running,S.State);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(mouse!=null) InputSystem.RemoveDevice(mouse);
            if(keyboard!=null) InputSystem.RemoveDevice(keyboard);
            if(game!=null) { Assert.IsNull(S.CheckInvariants()); S.Pause(); }
            yield return null;
        }
        Vector2 Center(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        IEnumerator Pointer(RectTransform rect,bool down)
        {
            cursor=Center(rect); held=down;
            InputSystem.QueueStateEvent(mouse,new MouseState { position=cursor }.WithButton(MouseButton.Left,down));
            yield return null; yield return null;
        }
        IEnumerator Click(RectTransform rect) { yield return Pointer(rect,true); yield return Pointer(rect,false); }
        IEnumerator KeyPress(Key key,float wait=.66f)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
            if(wait>0) yield return new WaitForSecondsRealtime(wait);
        }
        IEnumerator Drag(RectTransform source,RectTransform target)
        { yield return Pointer(source,true); Assert.IsNotNull(S.Drag); yield return Pointer(target,true); yield return Pointer(target,false); }

        [UnityTest] public IEnumerator MouseAndKeyboard_CompleteCaptureProcessCookServeLoop()
        {
            Assert.IsFalse(UI.guests[0].orderRoot.activeSelf);
            var initial=S.Guests[0]; yield return KeyPress(Key.W); Assert.IsTrue(UI.guests[0].orderRoot.activeSelf); Assert.AreSame(initial,S.Guests[0]);
            yield return KeyPress(Key.S); Assert.IsFalse(UI.guests[0].orderRoot.activeSelf);
            yield return Pointer(UI.tools[0].hit.Rect,true); Assert.IsNotNull(S.Drag);
            var heldTool=S.Drag; yield return KeyPress(Key.W); Assert.AreSame(heldTool,S.Drag); Assert.IsTrue(UI.dragRoot.gameObject.activeSelf);
            yield return Pointer(UI.guests[0].target.Rect,false); Assert.AreEqual(1,S.Stats.captures); Assert.IsNull(S.Guests[0]);
            yield return KeyPress(Key.A); Assert.AreEqual(ViewState.Kitchen,S.View); Assert.IsFalse(UI.guests[2].orderRoot.activeSelf);
            Assert.IsTrue(UI.recipeIcons.All(i=>i.gameObject.activeInHierarchy));
            yield return Drag(UI.captures[0].hit.Rect,UI.processor.hit.Rect); Assert.IsNotNull(S.Processing);
            yield return new WaitForSecondsRealtime(3.2f); Assert.AreEqual(4,S.Available[0]);
            yield return Drag(UI.bins[0].hit.Rect,UI.pot.hit.Rect); Assert.AreEqual(PotState.Loading,S.Pot); Assert.AreEqual(3,S.Available[0]);
            yield return Drag(UI.bins[1].hit.Rect,UI.pot.hit.Rect); Assert.AreEqual(PotState.Cooking,S.Pot);
            yield return new WaitForSecondsRealtime(4.2f); Assert.IsNotNull(S.Tray[0]); Assert.AreEqual("BloodStew",S.Tray[0].recipe.id);
            yield return KeyPress(Key.D); Assert.AreEqual(ViewState.FrontCounter,S.View);
            yield return Pointer(UI.frontTray[0].hit.Rect,true); int heldDish=S.Drag.instanceId;
            yield return KeyPress(Key.W); Assert.AreEqual(heldDish,S.Drag.instanceId);
            yield return Pointer(UI.guests[2].target.Rect,false); Assert.AreEqual(12,S.Stats.revenue); Assert.AreEqual(1,S.Stats.served); Assert.IsNull(S.Tray[0]);
            Assert.IsNull(S.CheckInvariants());
        }
        [UnityTest] public IEnumerator InputCancellationTransitionAndPausePreserveSources()
        {
            yield return Pointer(UI.tools[0].hit.Rect,true); Assert.IsNotNull(S.Drag);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); yield return null; yield return null;
            Assert.IsTrue(S.Transitioning);
            InputSystem.QueueStateEvent(mouse,new MouseState { position=cursor }); yield return null; yield return null;
            Assert.IsNull(S.Drag); Assert.AreEqual(0,S.Stats.captures);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return new WaitForSecondsRealtime(.3f);
            yield return KeyPress(Key.D); Assert.AreEqual(ViewState.Kitchen,S.View);
            yield return Drag(UI.bins[0].hit.Rect,UI.pot.hit.Rect); Assert.IsNotNull(S.Reserved);
            yield return Pointer(UI.bins[1].hit.Rect,true); Assert.IsNotNull(S.Drag);
            yield return KeyPress(Key.Escape,.1f); Assert.AreEqual(ShiftState.Paused,S.State); Assert.IsNull(S.Drag);
            float time=S.Elapsed, patience=S.Guests[1].patience; yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(time,S.Elapsed); Assert.AreEqual(patience,S.Guests[1].patience); Assert.IsNotNull(S.Reserved);
            yield return Pointer(UI.bins[1].hit.Rect,false); yield return KeyPress(Key.Escape,.1f);
            Assert.AreEqual(ShiftState.Running,S.State);
            yield return Drag(UI.reserved.hit.Rect,UI.bins[0].hit.Rect); Assert.IsNull(S.Reserved); Assert.AreEqual(2,S.Available[0]);
        }
        [UnityTest] public IEnumerator InspectionMenusRestartAndHiddenOrders()
        {
            Assert.IsFalse(UI.guests.Any(g=>g.orderRoot.activeInHierarchy));
            yield return KeyPress(Key.W); Assert.IsTrue(UI.guests[0].orderRoot.activeInHierarchy);
            yield return KeyPress(Key.S); Assert.IsFalse(UI.guests.Any(g=>g.orderRoot.activeInHierarchy));
            yield return KeyPress(Key.D); Assert.IsFalse(UI.guests.Any(g=>g.orderRoot.activeInHierarchy));
            S.Tick(300); yield return null; Assert.IsTrue(UI.resultsPanel.activeInHierarchy);
            yield return Click(UI.hitAreas.First(h=>h.command=="restart").Rect);
            Assert.AreEqual(ShiftState.Running,S.State); Assert.AreEqual(0,S.Stats.revenue); Assert.AreEqual(3,S.Stats.arrivals); Assert.IsNull(S.Drag);
            Assert.IsFalse(UI.guests.Any(g=>g.orderRoot.activeInHierarchy));
        }
    }
}
