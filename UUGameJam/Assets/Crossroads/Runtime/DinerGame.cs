using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Crossroads.Diner
{
    public sealed class DinerGame : MonoBehaviour
    {
        public DinerConfig config;
        public DinerPresentation presentation;
        public Camera viewCamera;
        public DinerSession Session { get; private set; }
        public float Volume { get; private set; }
        AudioSource effects, ambience;
        readonly AudioClip[] fallbackTones=new AudioClip[7];
        float lastInvariantCheck;
        bool appFocusPause;
        ShiftState previousState;

        void Awake()
        {
            Application.runInBackground=true;
            if(config==null || presentation==null) { Debug.LogError("Crossroads: missing configuration/presentation."); enabled=false; return; }
            try { Session=new DinerSession(config); }
            catch(Exception exception) { Debug.LogException(exception); enabled=false; return; }
            if(presentation.layoutRoot==null) presentation.Build();
            presentation.EnsurePauseTutorial();
            effects=gameObject.AddComponent<AudioSource>(); effects.playOnAwake=false; effects.spatialBlend=0;
            ambience=gameObject.AddComponent<AudioSource>(); ambience.playOnAwake=false; ambience.loop=true; ambience.spatialBlend=0;
            Volume=PlayerPrefs.GetFloat("Crossroads.Volume",config.theme.defaultVolume);
            if(config.theme.ambience!=null) { ambience.clip=config.theme.ambience; ambience.volume=Volume*.32f; ambience.Play(); }
            Session.Feedback+=OnFeedback;
            previousState=Session.State;
            presentation.Refresh(Session,Vector2.zero,Volume); presentation.AnimateView(Session);
        }
        void Update()
        {
            if(Session==null) return;
            float dt=Time.unscaledDeltaTime;
            Session.BeginFrame(dt);
            HandleKeyboard();
            presentation.AnimateView(Session);
            var mouse=Mouse.current; Vector2 pointer=mouse!=null ? mouse.position.ReadValue() : Vector2.zero;
            presentation.Refresh(Session,pointer,Volume);
            if(mouse!=null)
            {
                if(mouse.rightButton.wasPressedThisFrame) Session.CancelDrag();
                if(mouse.leftButton.wasPressedThisFrame)
                {
                    var button=presentation.HitAt(pointer,h=>!string.IsNullOrEmpty(h.command));
                    if(button!=null) Command(button.command);
                    else if(Session.State==ShiftState.Running && !Session.Transitioning)
                    {
                        var source=presentation.HitAt(pointer,h=>h.isSource);
                        if(source!=null) Session.BeginDrag(source.payloadType,source.index);
                    }
                }
                if(mouse.leftButton.wasReleasedThisFrame && Session.Drag!=null)
                {
                    if(Session.Transitioning) Session.CancelDrag();
                    else
                    {
                        var target=presentation.HitAt(pointer,h=>h.isTarget);
                        if(target!=null) Session.Drop(target.Target(Session)); else Session.InvalidRelease();
                    }
                }
            }
            Session.EndFrame(dt);
            presentation.Refresh(Session,pointer,Volume);
            UpdateHover(pointer);
            if(viewCamera!=null)
            {
                // Real camera pose mirrors the 2D view transition, allowing a future world-art replacement.
                float baseYaw=Session.View==ViewState.Kitchen ? 180 : 0;
                if(Session.Transitioning && Session.TurnDirection!=0)
                {
                    float retreat=Session.TransitionFrom==ViewState.FrontService ? config.pushSeconds/(config.pushSeconds+config.turnSeconds) : 0;
                    float t=Mathf.Clamp01((Session.TransitionProgress-retreat)/(1-retreat));
                    baseYaw=(Session.TransitionFrom==ViewState.Kitchen ? 180 : 0)+Session.TurnDirection*180*Mathf.SmoothStep(0,1,t);
                }
                viewCamera.transform.rotation=Quaternion.Euler(0,baseYaw,0);
                viewCamera.transform.position=new Vector3(0,0,Session.View==ViewState.FrontService ? -8 : -10);
            }
            if(previousState!=Session.State)
            {
                if(Session.State==ShiftState.Results)
                {
                    PlayerPrefs.SetInt("Crossroads.BestRevenue",Mathf.Max(PlayerPrefs.GetInt("Crossroads.BestRevenue",0),Session.Stats.revenue));
                    PlayerPrefs.Save();
                    Debug.Log("Crossroads shift result: "+JsonUtility.ToJson(Session.Stats));
                }
                previousState=Session.State;
            }
            if(Debug.isDebugBuild && Time.unscaledTime-lastInvariantCheck>1)
            {
                lastInvariantCheck=Time.unscaledTime; var error=Session.CheckInvariants(); if(error!=null) Debug.LogError("Crossroads invariant: "+error);
            }
        }
        void HandleKeyboard()
        {
            var keyboard=Keyboard.current; if(keyboard==null) return;
            if(keyboard.escapeKey.wasPressedThisFrame)
            {
                if(presentation.TutorialOpen) presentation.CloseTutorial();
                else if(Session.State==ShiftState.Paused) Session.Resume();
                else Session.Pause();
                return;
            }
            if(Session.State!=ShiftState.Running) return;
            if(keyboard.aKey.wasPressedThisFrame) Session.Navigate('a');
            else if(keyboard.dKey.wasPressedThisFrame) Session.Navigate('d');
            else if(keyboard.wKey.wasPressedThisFrame) Session.Navigate('w');
            else if(keyboard.sKey.wasPressedThisFrame) Session.Navigate('s');
        }
        public void Command(string command)
        {
            if(presentation.TutorialOpen && command!="tutorial_back") return;
            switch(command)
            {
                case "start": if(Session.State==ShiftState.Ready) Session.StartShift(); break;
                case "restart": if(Session.State==ShiftState.Results) Session.StartShift(); break;
                case "pause": Session.Pause(); break;
                case "resume": Session.Resume(); break;
                case "tutorial": if(Session.State==ShiftState.Paused) presentation.OpenTutorial(); break;
                case "tutorial_back": presentation.CloseTutorial(); break;
                case "quieter": SetVolume(Volume-.1f); break;
                case "louder": SetVolume(Volume+.1f); break;
            }
        }
        void SetVolume(float value)
        { Volume=Mathf.Clamp01(value); PlayerPrefs.SetFloat("Crossroads.Volume",Volume); if(ambience!=null) ambience.volume=Volume*.32f; }
        void UpdateHover(Vector2 pointer)
        {
            foreach(var h in presentation.hitAreas) if(h!=null) h.Highlight(false,false);
            if(Session.State!=ShiftState.Running || Session.Transitioning) return;
            if(Session.Drag!=null)
            {
                var target=presentation.HitAt(pointer,h=>h.isTarget);
                if(target!=null)
                {
                    var result=Session.CanDrop(target.Target(Session)); target.Highlight(true,result.success);
                    presentation.helpText.text=result.reason+"   |   Right click - Cancel";
                }
            }
            else
            {
                var source=presentation.HitAt(pointer,h=>h.isSource);
                if(source!=null && !string.IsNullOrEmpty(source.tooltip)) presentation.helpText.text=source.tooltip;
            }
        }
        void OnFeedback(string text,SoundCue cue)
        {
            presentation.noticeText.text=text;
            var theme=config.theme;
            AudioClip clip=cue==SoundCue.Arrival ? theme.arrival : cue==SoundCue.Capture ? theme.capture : cue==SoundCue.Invalid ? theme.invalid : cue==SoundCue.Processed ? theme.processed : cue==SoundCue.Cooked ? theme.cooked : cue==SoundCue.Paid ? theme.paid : theme.walkout;
            if(clip==null)
            {
                int i=(int)cue;
                if(fallbackTones[i]==null) fallbackTones[i]=Tone(i);
                clip=fallbackTones[i];
            }
            if(effects!=null && clip!=null) effects.PlayOneShot(clip,Volume*.3f);
        }
        AudioClip Tone(int cue)
        {
            const int rate=22050; int count=rate/5; var samples=new float[count];
            float[] notes={523.25f,196f,130.8f,392f,659.25f,783.99f,164.8f};
            for(int i=0;i<count;i++)
            {
                float t=(float)i/rate, envelope=Mathf.Sin(Mathf.PI*i/count)*Mathf.Exp(-14*t);
                samples[i]=Mathf.Sin(2*Mathf.PI*notes[cue]*t)*envelope*.22f;
            }
            var clip=AudioClip.Create("Crossroads placeholder cue "+cue,count,1,rate,false); clip.SetData(samples,0); return clip;
        }
        void OnApplicationFocus(bool focus)
        {
            // Do not advance a player's shift while they alt-tab. Background simulation inside a view is unaffected.
            if(!focus && Session!=null && Session.State==ShiftState.Running && !Application.isEditor) { Session.Pause(); appFocusPause=true; }
            if(focus && appFocusPause) appFocusPause=false;
        }
        void OnDestroy()
        {
            if(Session!=null) Session.Feedback-=OnFeedback;
            foreach(var clip in fallbackTones) if(clip!=null) Destroy(clip);
        }
    }
}
