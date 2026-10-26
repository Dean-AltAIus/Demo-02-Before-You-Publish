using System;
using System.Collections;
using System.Reflection;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Integration;
using Altaius.Demo02.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
namespace Altaius.Demo02.Authoring
{
    public static partial class Task6RuntimeAcceptance
    {
        public static IEnumerator ArrowInput(string output)
        {
            Bind(output);CampusDiagnostics.GameSize(1280,720);yield return Frames(12);
            ui.Applied.TextScale=1;ui.Applied.ReducedMotion=false;ui.Applied.Silent=false;Apply();ui.CloseSurface(false);view.CloseWindow();EventSystem.current.SetSelectedGameObject(null);
            var motor=ui.Gameplay.Motor;var origin=motor.transform.position;var facing=motor.CharacterVisual.rotation;
            foreach(var key in new[]{Key.UpArrow,Key.DownArrow,Key.LeftArrow,Key.RightArrow})
            {
                motor.GoTo(origin,facing);ui.Gameplay.CameraRig.SnapFollow();yield return Frames(3);
                var forward=motor.ViewCamera.transform.forward;forward.y=0;forward.Normalize();var right=Vector3.Cross(Vector3.up,forward);var expected=key==Key.UpArrow?forward:key==Key.DownArrow?-forward:key==Key.LeftArrow?-right:right;
                var before=motor.transform.position;Check(ui.Gameplay.WorldOwnsInput,"World owns input before "+key);input.Keys=new[]{key};yield return Seconds(.55f);input.Keys=Array.Empty<Key>();yield return Frames(3);var delta=motor.transform.position-before;delta.y=0;
                Check(Vector3.Dot(delta,expected)>.2f,"Arrow moves actual CharacterController in expected direction "+key+" distance "+delta.magnitude);
            }
            motor.GoTo(origin,facing);ui.Gameplay.CameraRig.SnapFollow();yield return Frames(3);
            foreach(var key in new[]{Key.W,Key.A,Key.S,Key.D}){var before=motor.transform.position;input.Keys=new[]{key};yield return Seconds(.25f);input.Keys=Array.Empty<Key>();yield return Frames(2);var delta=motor.transform.position-before;delta.y=0;Check(delta.magnitude<.01f,"Legacy letter does not move world "+key);}
            yield return Shot("arrow-world-hud-1280");input.Keys=new[]{Key.Tab};yield return Frames(2);input.Keys=Array.Empty<Key>();yield return Frames(3);Check(view.ViewId=="routes"&&!ui.Gameplay.WorldOwnsInput,"Tab still opens native direct routes");
            var selected=EventSystem.current.currentSelectedGameObject;input.Keys=new[]{Key.Tab};yield return Frames(2);input.Keys=Array.Empty<Key>();yield return Frames(3);Check(EventSystem.current.currentSelectedGameObject!=selected,"Tab still changes native UI focus");
            var stationary=motor.transform.position;input.Keys=new[]{Key.UpArrow};yield return Seconds(.25f);input.Keys=Array.Empty<Key>();yield return Frames(3);var uiDelta=motor.transform.position-stationary;uiDelta.y=0;Check(uiDelta.magnitude<.01f,"Arrow UI navigation cannot move world");
            yield return Route("studio");W("Inspect.D02.rubric.v1.naming");W("Primary");yield return Route("experiment");W("Work.W_LIST");W("Criterion.naming");yield return Until(()=>!contacts.Busy&&contacts.QueuedCount==0,30,"native normal contact before focused input");
            const string own="wasd remains literal text";yield return Type("Mark.Rationale",own);var field=view.Rows.Find("Mark.Rationale").Input;field.caretPosition=own.Length;field.ForceLabelUpdate();
            stationary=motor.transform.position;input.Keys=new[]{Key.LeftArrow};yield return Frames(2);input.Keys=Array.Empty<Key>();yield return Frames(2);
            field.ProcessEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.LeftArrow});field.ForceLabelUpdate();yield return Frames(3);var typedDelta=motor.transform.position-stationary;typedDelta.y=0;
            Check(typedDelta.magnitude<.01f&&!ui.Gameplay.WorldOwnsInput,"Focused text owns arrows and blocks world movement");Check(field.text==own&&field.caretPosition<own.Length,"Native caret navigation preserves literal WASD draft");yield return Shot("arrow-focused-input-1280");
            ui.OpenSettings("world");typeof(PresentationView).GetMethod("ShowSettingsTab",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,new object[]{2});yield return Shot("arrow-controls-1280");
            ui.CloseSurface(false);view.CloseWindow();ui.OpenCoach();ui.RequestHelp(HelpIntent.Controls);yield return Frames(15);yield return Shot("arrow-help-1280");
            Result="PASS arrow world movement and native focus ownership";Log(Result);
        }
    }
}
