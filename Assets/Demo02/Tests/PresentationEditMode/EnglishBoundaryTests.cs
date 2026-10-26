using System;
using System.Linq;
using System.Reflection;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Presentation;
using Altaius.Demo02.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Altaius.Demo02.Tests
{
    public sealed class EnglishBoundaryTests
    {
        Scene preview; PresentationView ui; WorkbenchView view; ReviewNativeEvents events;
        LocalPresentationPreferences preferences;
        const string Own = "Not approved \u2013 20 kg \u2014  not 2 kg.  {task_version} $1 <b>literal</b>\nArabic: \u0644\u0627";
        [SetUp] public void Setup()
        {
            preferences=LocalPresentationPreferences.Load();
            preview=EditorSceneManager.OpenPreviewScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            ui=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PresentationView>(true)).Single();view=ui.Workbench;
            var go=new GameObject("Isolated native English events",typeof(ReviewNativeEvents));SceneManager.MoveGameObjectToScene(go,preview);
            events=go.GetComponent<ReviewNativeEvents>();events.BeginNative();EventSystem.current=events;
            ui.Host.BeginSession(SessionMode.Independent);Call(ui,"Start");Call(view,"Start");ui.CloseSurface(false);
        }
        static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
        [TearDown] public void Cleanup(){if(events){events.SetSelectedGameObject(null);events.EndNative();}EditorSceneManager.ClosePreviewScene(preview);preferences.Save();}
        [Test] public void MarkDraftRemainsLiteralAfterNativeReopenScaleAndLaterEdit()
        {
            var s=ui.Host.Session;s.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);ui.Gameplay.DirectRoute("experiment");
            view.OpenWork("W_LIST");view.OpenMark("W_LIST","naming");view.SetFieldText("Mark.Marks","7");view.SetFieldText("Mark.Rationale",Own);
            view.OpenWork("W_LIST");view.OpenMark("W_LIST","naming");view.SetTextScale(2);
            var field=view.Rows.Find("Mark.Rationale").Input;
            Assert.That(field.text,Is.EqualTo(Own));Assert.That(field.textComponent.supportRichText,Is.False);
            field.text+=" !";Assert.That(ui.Host.Workbench.Mark.Rationale,Is.EqualTo(Own+" !"));
            view.SaveAnnotation();Assert.That(s.Snapshot.AnnotationHistory.Last().Content.Rationale,Is.EqualTo(Own+" !"));
        }
        [Test] public void TaskAndRubricDraftRemainLiteralThroughPreviewCancelAndReopen()
        {
            var s=ui.Host.Session;foreach(var id in new[]{"D02.task.v1.Q1","D02.task.v1.Q2","D02.rubric.v1.naming","D02.rubric.v1.listing"})s.Inspect(id,InspectionLocation.Studio);
            ui.Gameplay.DirectRoute("studio");view.OpenTask();view.SetFieldText("Task.Q1.Text",Own);view.SetFieldText("Task.Context",Own);
            s.Inspect("S_A",InspectionLocation.Archive);s.Inspect("S_B",InspectionLocation.Archive);ui.Host.Workbench.Task.SourceIds.UnionWith(new[]{"S_A","S_B"});
            view.ActivateControl("Primary");Assert.That(view.ViewId,Is.EqualTo("preview"));view.ActivateControl("Secondary");view.OpenTask();
            Assert.That(view.Rows.Find("Task.Q1.Text").Input.text,Is.EqualTo(Own));Assert.That(view.Rows.Find("Task.Context").Input.text,Is.EqualTo(Own));
            view.OpenRubric();view.SetFieldText("Rubric.0.Text",Own);view.ActivateControl("Primary");view.ActivateControl("Secondary");view.OpenRubric();
            Assert.That(view.Rows.Find("Rubric.0.Text").Input.text,Is.EqualTo(Own));
        }
        [Test] public void EmptyNumericMarkUsesNumericPromptWithoutPrefill()
        {
            var s=ui.Host.Session;s.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);ui.Gameplay.DirectRoute("experiment");view.OpenWork("W_LIST");view.OpenMark("W_LIST","naming");
            var field=view.Rows.Find("Mark.Marks").Input;Assert.That(field.text,Is.Empty);
            Assert.That(((Text)field.placeholder).text,Does.Contain("whole number"));Assert.That(view.Primary.interactable,Is.False);
        }
    }
}
