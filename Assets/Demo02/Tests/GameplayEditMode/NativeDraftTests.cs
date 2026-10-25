using Altaius.Demo02.Domain;
using Altaius.Demo02.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Altaius.Demo02.Gameplay.Tests
{
    // Exposes the protected native activation tick; it does not replace InputField editing/cancellation.
    public sealed class NativeFieldProbe : InputField {public void NativeTick()=>base.LateUpdate();}
    public sealed class NativeEventsProbe : EventSystem
    {public void BeginNative()=>base.OnEnable();public void EndNative()=>base.OnDisable();}
    public sealed class NativeEscapeWorkspace : MonoBehaviour, ICampusWorkspace
    {
        public GameObject Row;public bool IsOpen{get;set;}=true;public int BackCount;
        public void Back(){BackCount++;IsOpen=false;Row.SetActive(false);}
        public void Pause(){} public void OpenRoutes(){} public void OpenStation(CampusStation station){}
    }
    public class NativeDraftTests
    {
        Scene preview;GameObject root;EventSystem previous;NativeEventsProbe events;NativeRow row;NativeFieldProbe field;
        WorkbenchModel model;MarkDraft draft;CampusGameplayFacade facade;NativeEscapeWorkspace workspace;
        [SetUp] public void SetUp()
        {
            previous=EventSystem.current;preview=EditorSceneManager.NewPreviewScene();root=new GameObject("Native draft test",typeof(Canvas));SceneManager.MoveGameObjectToScene(root,preview);
            var eventObject=new GameObject("Isolated native events",typeof(NativeEventsProbe));eventObject.transform.SetParent(root.transform);events=eventObject.GetComponent<NativeEventsProbe>();events.BeginNative();EventSystem.current=events;
            var rowObject=new GameObject("Native draft row",typeof(RectTransform),typeof(NativeRow));rowObject.transform.SetParent(root.transform);row=rowObject.GetComponent<NativeRow>();row.Kind=NativeRowKind.Input;
            row.Caption=Label(rowObject.transform,"Caption");
            var inputObject=new GameObject("Input",typeof(RectTransform),typeof(Image),typeof(NativeFieldProbe));inputObject.transform.SetParent(rowObject.transform);field=inputObject.GetComponent<NativeFieldProbe>();field.textComponent=Label(inputObject.transform,"Text");field.targetGraphic=inputObject.GetComponent<Image>();field.caretBlinkRate=0;row.Input=field;
            var helper=new GameObject("Inactive facade fixture");helper.SetActive(false);helper.transform.SetParent(root.transform);facade=helper.AddComponent<CampusGameplayFacade>();workspace=helper.AddComponent<NativeEscapeWorkspace>();workspace.Row=rowObject;facade.WorkspaceBehaviour=workspace;
            var session=OfflineSession.Create(SessionMode.Independent);session.Inspect("W_LIST",InspectionLocation.ExperimentTable);session.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);
            model=new WorkbenchModel(session);draft=model.OpenMark("W_LIST","naming");draft.Rationale="Original rationale";Bind();
        }
        static Text Label(Transform parent,string name)
        {var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent);var t=g.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.rectTransform.sizeDelta=new Vector2(800,120);return t;}
        void Bind()=>row.BindInput("Own rationale",draft.Rationale,v=>draft.Rationale=v,1,true);
        void Activate(){events.SetSelectedGameObject(field.gameObject);field.ActivateInputField();field.NativeTick();Assert.That(field.isFocused,Is.True);}
        void Type(string text){foreach(char c in text)field.ProcessEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.None,character=c});field.ForceLabelUpdate();}
        void Cancel(){field.ProcessEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape});field.DeactivateInputField();}
        [TearDown] public void TearDown()
        {if(events){events.SetSelectedGameObject(null);events.EndNative();}EditorSceneManager.ClosePreviewScene(preview);if(previous)EventSystem.current=previous;}

        [TestCase(true)] [TestCase(false)]
        public void NativeEscapeRetainsLatestDraftInEitherEventOrder(bool nativeFirst)
        {
            Activate();const string typed="My current rationale, not the original.";Type(typed);Assert.That(draft.Rationale,Is.EqualTo(typed));
            if(nativeFirst){Cancel();facade.HandleEscape();}else{facade.HandleEscape();Cancel();}
            Assert.That(workspace.BackCount,Is.EqualTo(1));Assert.That(draft.Rationale,Is.EqualTo(typed));
            row.gameObject.SetActive(true);draft=model.OpenMark("W_LIST","naming");Bind();
            Assert.That(field.text,Is.EqualTo(typed));Assert.That(field.textComponent.text,Is.EqualTo(typed));
            Assert.That(model.Session.Snapshot.AnnotationHistory,Is.Empty);
        }

        [Test] public void EditsAfterCancellationRemainEditableAndAnotherEscapeRetainsThem()
        {
            Activate();Type("First draft");Cancel();
            // Presenter/property editing can occur before the next native activation tick.
            field.text="Second draft";field.text="Third draft";Assert.That(draft.Rationale,Is.EqualTo("Third draft"));
            Activate();Type("Fourth draft");Cancel();Assert.That(draft.Rationale,Is.EqualTo("Fourth draft"));
            Assert.That(field.text,Is.EqualTo("Fourth draft"));
        }

        [Test] public void SavedLiteralRowsRenderMarkupAsCharacters()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Prefabs/UI/TextRow.prefab");var literal=Object.Instantiate(prefab,root.transform).GetComponent<NativeRow>();
            Assert.That(literal.Caption.supportRichText,Is.False);Assert.That(literal.Body.supportRichText,Is.False);
            literal.BindText("<b>Question</b>","<b>Explain</b>",1);
            Assert.That(literal.Caption.text,Is.EqualTo("<b>Question</b>"));Assert.That(literal.Body.text,Is.EqualTo("<b>Explain</b>"));
            var settings=literal.Body.GetGenerationSettings(new Vector2(800,120));var generator=new TextGenerator();
            Assert.That(generator.GetPreferredWidth(literal.Body.text,settings),Is.GreaterThan(generator.GetPreferredWidth("Explain",settings)));
        }
    }
}
