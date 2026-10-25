using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.Authoring
{
    /// <summary>One bounded additive authoring batch. Never rebuilds or replaces the campus, cast, camera or folios.</summary>
    public static class PresentationAuthoring
    {
        const string Root="Assets/Demo02/Prefabs/UI/Presentation/";
        static Font Regular=>AssetDatabase.LoadAssetAtPath<Font>("Assets/Demo02/Art/UI/Fonts/SourceSerif4-Regular.ttf");
        static Font Heading=>AssetDatabase.LoadAssetAtPath<Font>("Assets/Demo02/Art/UI/Fonts/SourceSerif4-Semibold.ttf");
        static Color Ink=>NativeUiPalette.Ink;
        static readonly Color Cream=new Color(1,.965f,.87f),Navy=new Color(.025f,.11f,.23f);
        static NativeRow textRow,actionRow,inputRow;
        [MenuItem("Altaius/Demo02/Add saved presentation surfaces (once)")]
        public static void AddSavedSurfaces()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before saved UI authoring.");
            var view=UnityEngine.Object.FindFirstObjectByType<WorkbenchView>();if(!view)throw new InvalidOperationException("Open the existing authored Demo 02 campus.");
            if(UnityEngine.Object.FindFirstObjectByType<PresentationView>()||File.Exists(Root+"PresentationShell.prefab"))throw new InvalidOperationException("Presentation authoring already exists. Existing overrides are preserved. Use a named bounded refinement instead.");
            if(!Regular||!Heading)throw new InvalidOperationException("Import the licensed static Source Serif fonts first.");
            Directory.CreateDirectory(Root);Directory.CreateDirectory("Assets/Demo02/Settings/Presentation");
            textRow=CreateRow(NativeRowKind.Text);actionRow=CreateRow(NativeRowKind.Action);inputRow=CreateRow(NativeRowKind.Input);
            var root=Rect("Saved presentation shell",view.ConversationExtensionSlot);Stretch((RectTransform)view.ConversationExtensionSlot);Stretch((RectTransform)root.transform);
            var presentation=root.AddComponent<PresentationView>();presentation.Host=view.Host;presentation.Gameplay=view.Gameplay;presentation.Workbench=view;
            var overlay=Rect("System and conversation layer",root.transform);Stretch((RectTransform)overlay.transform);overlay.AddComponent<Image>().color=new Color(.015f,.05f,.1f,.35f);presentation.Overlay=overlay;
            var surfaces=new List<PresentationSurface>();
            surfaces.Add(Surface(overlay.transform,"title","Before You Publish",NativeIconKind.Book,false,false,null,false,true));
            surfaces.Add(Surface(overlay.transform,"brief","Before you begin",NativeIconKind.Book,false,false,null));
            surfaces.Add(Surface(overlay.transform,"loading","Preparing local practice",NativeIconKind.Clock,false));
            surfaces.Add(Surface(overlay.transform,"onramp","Optional controls",NativeIconKind.Book,true));
            surfaces.Add(Surface(overlay.transform,"conversation","Colleague",NativeIconKind.Conversation,true,false,new[]{"Colleague","Module reviewer","Experiment reviewer"},true));
            surfaces.Add(Surface(overlay.transform,"coach","Leena | AI coach",NativeIconKind.Coach,true,false,new[]{"Controls","Recorded attempt","Requested strategy"},true));
            surfaces.Add(Surface(overlay.transform,"review","Review and publication",NativeIconKind.Lock,true,true));
            surfaces.Add(Surface(overlay.transform,"reviewRequest","Request review time",NativeIconKind.Clock,true,true));
            surfaces.Add(Surface(overlay.transform,"hold","Hold proposal",NativeIconKind.Document,true,true));
            surfaces.Add(Surface(overlay.transform,"confirmation","Confirm this action",NativeIconKind.Document,false));
            surfaces.Add(Surface(overlay.transform,"receipt","Scoped response receipt",NativeIconKind.Document,true,true));
            surfaces.Add(Surface(overlay.transform,"pause","Paused",NativeIconKind.Pause,false));
            surfaces.Add(Surface(overlay.transform,"debrief","Practice record",NativeIconKind.Book,true,true));
            surfaces.Add(Surface(overlay.transform,"timeline","Read-only timeline",NativeIconKind.Timeline,true));
            surfaces.Add(Surface(overlay.transform,"replay","Read-only replay",NativeIconKind.Timeline,true));
            var settings=Surface(overlay.transform,"settings","Settings | Audio",NativeIconKind.Settings,true,false,new[]{"Audio","Display","Input","Accessibility","Language"});surfaces.Add(settings);AddSettingsControls(settings);
            presentation.Surfaces=surfaces.ToArray();
            var talk=MakeButton("Talk to a role",root.transform,"Talk to a role",NativeIconKind.Conversation);var tr=(RectTransform)talk.transform;tr.anchorMin=tr.anchorMax=new Vector2(1,0);tr.pivot=Vector2.one;tr.anchoredPosition=new Vector2(-18,134);tr.sizeDelta=new Vector2(218,48);presentation.TalkLauncher=talk;
            var config=ScriptableObject.CreateInstance<DisconnectedIntegrationSettings>();AssetDatabase.CreateAsset(config,"Assets/Demo02/Settings/Presentation/DisabledIntegration.asset");presentation.Integration=config;
            // Save complete editable screen assets before any runtime binding. No initializer creates a screen.
            SeedVisibleTitle(surfaces[0]);
            foreach(var surface in surfaces)
            {surface.ApplyScale(1);surface.gameObject.SetActive(surface.SurfaceId=="title");PrefabUtility.SaveAsPrefabAssetAndConnect(surface.gameObject,Root+surface.SurfaceId+".prefab",InteractionMode.AutomatedAction);}
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,Root+"PresentationShell.prefab",InteractionMode.AutomatedAction);
            // Scene references are explicit overrides on the saved shell, not missing prefab-scene links.
            presentation=root.GetComponent<PresentationView>();presentation.Host=view.Host;presentation.Gameplay=view.Gameplay;presentation.Workbench=view;
            StyleExistingCanvas(view);
            var coach=view.LeenaDock.GetComponent<Button>();if(!coach)coach=view.LeenaDock.gameObject.AddComponent<Button>();
            coach.targetGraphic=AddFrame(view.LeenaDock.gameObject,NativeFrameKind.Navy,true);SetButtonColours(coach);presentation.CoachLauncher=coach;
            view.LeenaDock.SetAsLastSibling();view.Gameplay.WorkspaceBehaviour=presentation;
            foreach(var surface in presentation.Surfaces)surface.Focus.AdditionalFocusTargets=new Selectable[]{coach};
            PrefabUtility.RecordPrefabInstancePropertyModifications(presentation);PrefabUtility.RecordPrefabInstancePropertyModifications(view.Gameplay);
            EditorUtility.SetDirty(presentation);EditorUtility.SetDirty(view.Gameplay);EditorSceneManager.MarkSceneDirty(view.gameObject.scene);EditorSceneManager.SaveScene(view.gameObject.scene);AssetDatabase.SaveAssets();
        }
        static void SeedVisibleTitle(PresentationSurface title)
        {
            title.Rows.Begin();var row=title.Rows.Take(NativeRowKind.Text,"Title.AuthoredPreview");row.BindText("Fictional practice","Inspect, mark, revise and seek exact-pair review or an owned hold.",1);
            title.Status.text="Authored offline. English available. Arabic and voice unavailable.";
            title.Primary.GetComponentInChildren<Text>().text="Begin practice";title.Secondary.GetComponentInChildren<Text>().text="Settings";title.Tertiary.GetComponentInChildren<Text>().text="Exit";title.Close.gameObject.SetActive(false);title.ApplyScale(1);
        }
        static NativeRow CreateRow(NativeRowKind kind)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Prefabs/UI/"+kind+"Row.prefab");
            var g=UnityEngine.Object.Instantiate(source);g.name=kind+" source-faithful native row";var row=g.GetComponent<NativeRow>();
            StyleText(g);var image=g.GetComponent<Image>();if(image)image.color=Color.clear;
            if(kind==NativeRowKind.Action)StyleButton(row.Action);
            if(kind==NativeRowKind.Input){AddFrame(row.Input.gameObject,NativeFrameKind.Input,false);row.Input.selectionColor=new Color(.16f,.6f,.9f,.4f);row.Input.caretColor=Ink;row.Input.customCaretColor=true;}
            var result=PrefabUtility.SaveAsPrefabAsset(g,Root+kind+"Row.prefab").GetComponent<NativeRow>();UnityEngine.Object.DestroyImmediate(g);return result;
        }
        static PresentationSurface Surface(Transform parent,string id,string heading,NativeIconKind icon,bool wide,bool sidebar=false,string[] tabs=null,bool composer=false,bool title=false)
        {
            var g=Rect("Native "+id+" surface",parent);Stretch((RectTransform)g.transform);var s=g.AddComponent<PresentationSurface>();s.SurfaceId=id;s.Wide=wide;s.TitleComposition=title;
            var frame=Rect("Navy and gold dimensional frame",g.transform);s.Frame=(RectTransform)frame.transform;frame.AddComponent<NativeFrame>().Kind=NativeFrameKind.Navy;
            var paper=Rect("Cream paper reading surface",frame.transform);Stretch((RectTransform)paper.transform,12,12,12,title?12:68);paper.AddComponent<NativeFrame>().Kind=NativeFrameKind.Paper;s.Paper=(RectTransform)paper.transform;
            var symbol=Rect("Meaningful "+icon+" icon",frame.transform);var glyph=symbol.AddComponent<NativeIcon>();glyph.Kind=icon;glyph.color=NativeUiPalette.Gold;Place((RectTransform)symbol.transform,24,20,42,42);s.Icon=glyph;
            s.Heading=Label("Live serif heading",frame.transform,heading,30,Cream,true);s.Heading.rectTransform.anchorMin=new Vector2(0,1);s.Heading.rectTransform.anchorMax=Vector2.one;s.Heading.rectTransform.pivot=new Vector2(.5f,1);
            s.Close=MakeButton("Back or close",frame.transform,"Back",NativeIconKind.Return,false);var cr=(RectTransform)s.Close.transform;cr.anchorMin=cr.anchorMax=Vector2.one;cr.pivot=Vector2.one;cr.anchoredPosition=new Vector2(-16,-14);cr.sizeDelta=new Vector2(90,44);s.Close.GetComponentInChildren<Text>().fontSize=17;
            if(tabs!=null)
            {var strip=Rect("Native labelled tabs",frame.transform);s.Tabs=(RectTransform)strip.transform;s.Tabs.anchorMin=new Vector2(0,1);s.Tabs.anchorMax=Vector2.one;s.Tabs.pivot=new Vector2(.5f,1);var group=strip.AddComponent<HorizontalLayoutGroup>();group.spacing=4;group.childControlHeight=group.childControlWidth=true;group.childForceExpandHeight=group.childForceExpandWidth=true;
                s.TabButtons=tabs.Select((label,index)=>MakeButton(label,strip.transform,label,index==0?NativeIconKind.Book:NativeIconKind.Document,false)).ToArray();}
            else s.TabButtons=Array.Empty<Button>();
            if(sidebar)
            {var side=Rect("Current pair and gate",frame.transform);s.Sidebar=(RectTransform)side.transform;s.Sidebar.anchorMin=Vector2.zero;s.Sidebar.anchorMax=new Vector2(0,1);side.AddComponent<NativeFrame>().Kind=NativeFrameKind.QuietButton;
                var big=Rect("Gate symbol",side.transform);var gate=big.AddComponent<NativeIcon>();gate.Kind=icon;gate.color=Ink;Place((RectTransform)big.transform,65,28,72,72);
                s.SideText=Label("Current pair and status",side.transform,"Task 1\nRubric 1\n\nDraft\n\n13:20",23,Ink,true);Stretch(s.SideText.rectTransform,18,18,12,124);s.SideText.alignment=TextAnchor.UpperCenter;}
            var scrollObject=Rect("Native content scroll",frame.transform);s.Scroll=scrollObject.AddComponent<ScrollRect>();s.Scroll.horizontal=false;s.Scroll.movementType=ScrollRect.MovementType.Clamped;s.Scroll.inertia=false;s.Scroll.scrollSensitivity=40;Stretch((RectTransform)scrollObject.transform,20,20,132,80);
            var viewport=Rect("Clipped paper viewport",scrollObject.transform);Stretch((RectTransform)viewport.transform,0,18);viewport.AddComponent<Image>().color=new Color(1,1,1,.01f);viewport.AddComponent<RectMask2D>();s.Scroll.viewport=(RectTransform)viewport.transform;
            var content=Rect("Saved native reading and action slots",viewport.transform);var contentRect=(RectTransform)content.transform;contentRect.anchorMin=new Vector2(0,1);contentRect.anchorMax=Vector2.one;contentRect.pivot=new Vector2(.5f,1);contentRect.sizeDelta=Vector2.zero;contentRect.anchoredPosition=Vector2.zero;
            var layout=content.AddComponent<VerticalLayoutGroup>();layout.spacing=14;layout.padding=new RectOffset(12,12,10,10);layout.childControlHeight=layout.childControlWidth=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;content.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;s.Scroll.content=contentRect;
            var pool=content.AddComponent<NativeRowPool>();pool.TextPrefab=textRow;pool.ActionPrefab=actionRow;pool.InputPrefab=inputRow;s.Rows=pool;
            for(int i=0;i<4;i++)foreach(var prefab in new[]{textRow,actionRow,inputRow}){var row=(GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject,content.transform);row.name="Editable "+prefab.Kind+" slot "+(i+1);pool.AuthoredRows.Add(row.GetComponent<NativeRow>());row.SetActive(false);}
            var track=Rect("Gold scroll track",scrollObject.transform);var trackRect=(RectTransform)track.transform;trackRect.anchorMin=new Vector2(1,0);trackRect.anchorMax=Vector2.one;trackRect.pivot=new Vector2(1,.5f);trackRect.sizeDelta=new Vector2(12,0);track.AddComponent<Image>().color=new Color(.77f,.68f,.52f);
            var handle=Rect("Navy scroll handle",track.transform);Stretch((RectTransform)handle.transform);handle.AddComponent<Image>().color=Navy;var scrollbar=track.AddComponent<Scrollbar>();scrollbar.handleRect=(RectTransform)handle.transform;scrollbar.targetGraphic=handle.GetComponent<Image>();scrollbar.direction=Scrollbar.Direction.BottomToTop;s.Scroll.verticalScrollbar=scrollbar;
            s.Status=Label("Actual status and request identity",frame.transform,"Local state only. No live service.",17,Ink);var status=(RectTransform)s.Status.transform;status.anchorMin=Vector2.zero;status.anchorMax=new Vector2(1,0);status.pivot=new Vector2(.5f,0);
            s.Primary=MakeButton("Primary action",frame.transform,"Continue",NativeIconKind.Check);s.Secondary=MakeButton("Secondary action",frame.transform,"Back",NativeIconKind.Return);s.Tertiary=MakeButton("Tertiary action",frame.transform,"More",NativeIconKind.Document);
            if(composer)AddComposer(s,frame.transform);
            s.Focus=frame.AddComponent<KeyboardFocus>();s.Focus.Gameplay=UnityEngine.Object.FindFirstObjectByType<CampusGameplayFacade>();s.Focus.FocusRoot=s.Frame;s.Focus.Scroll=s.Scroll;
            s.ApplyScale(1);return s;
        }
        static void AddComposer(PresentationSurface s,Transform parent)
        {
            var container=Rect("Persistent own-word composer",parent);s.ComposerFrame=(RectTransform)container.transform;s.ComposerFrame.anchorMin=Vector2.zero;s.ComposerFrame.anchorMax=new Vector2(1,0);s.ComposerFrame.pivot=new Vector2(.5f,0);
            var inputObject=Rect("Own text, never prefilled",container.transform);var r=(RectTransform)inputObject.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(0,6);r.offsetMax=new Vector2(-194,-6);inputObject.AddComponent<Image>().color=Cream;AddFrame(inputObject,NativeFrameKind.Input,false);
            s.Composer=inputObject.AddComponent<InputField>();s.Composer.targetGraphic=inputObject.GetComponent<Image>();s.Composer.lineType=InputField.LineType.MultiLineNewline;s.Composer.characterLimit=12000;s.Composer.selectionColor=new Color(.2f,.62f,.86f,.4f);s.Composer.customCaretColor=true;s.Composer.caretColor=Ink;
            var text=Label("Editable own words",inputObject.transform,"",21,Ink);Stretch(text.rectTransform,14,14,12,12);s.Composer.textComponent=text;
            var placeholder=Label("Free question placeholder",inputObject.transform,"Write in your own words...",20,new Color(.32f,.30f,.26f));Stretch(placeholder.rectTransform,14,14,12,12);s.Composer.placeholder=placeholder;
            s.ComposerDraft=inputObject.AddComponent<NativeRow>();s.ComposerDraft.Kind=NativeRowKind.Input;s.ComposerDraft.Caption=placeholder;s.ComposerDraft.Input=s.Composer;
            s.Send=MakeButton("Send actual own question",container.transform,"Send",NativeIconKind.Send);var send=(RectTransform)s.Send.transform;send.anchorMin=new Vector2(1,.5f);send.anchorMax=Vector2.one;send.offsetMin=new Vector2(-178,6);send.offsetMax=new Vector2(0,-6);
            s.CancelRequest=MakeButton("Cancel original local request",container.transform,"Cancel request",NativeIconKind.Return);var cancel=(RectTransform)s.CancelRequest.transform;cancel.anchorMin=new Vector2(1,0);cancel.anchorMax=new Vector2(1,.5f);cancel.offsetMin=new Vector2(-178,6);cancel.offsetMax=new Vector2(0,-6);
        }
        static void AddSettingsControls(PresentationSurface screen)
        {
            var rows=new List<NativeSettingRow>();
            var master=SettingRow(screen,"master",0,"Master volume",out var owner);var slider=DefaultControls.CreateSlider(new DefaultControls.Resources());slider.name="Native master volume slider";slider.transform.SetParent(owner,false);Stretch((RectTransform)slider.transform);screen.MasterSlider=slider.GetComponent<Slider>();screen.MasterSlider.minValue=0;screen.MasterSlider.maxValue=100;screen.MasterSlider.wholeNumbers=true;
            screen.MasterValue=Label("Actual master percentage",owner,"80%",20,Ink,true);var val=screen.MasterValue.rectTransform;val.anchorMin=new Vector2(1,0);val.anchorMax=Vector2.one;val.offsetMin=new Vector2(-64,0);val.offsetMax=Vector2.zero;((RectTransform)slider.transform).offsetMax=new Vector2(-76,0);rows.Add(master);
            slider.transform.Find("Background").GetComponent<Image>().color=new Color(.66f,.56f,.4f);slider.transform.Find("Fill Area/Fill").GetComponent<Image>().color=new Color(.015f,.48f,.74f);screen.MasterSlider.targetGraphic=AddFrame(screen.MasterSlider.handleRect.gameObject,NativeFrameKind.Button,false);
            screen.SilentToggle=Toggle(screen,"silent",0,"Silent mode",rows);screen.MotionToggle=Toggle(screen,"motion",3,"Reduce motion",rows);screen.ContrastToggle=Toggle(screen,"contrast",3,"High contrast labels",rows);
            var text=SettingRow(screen,"textSize",3,"Text size",out var textOwner);var dropdown=DefaultControls.CreateDropdown(new DefaultControls.Resources());dropdown.name="Native text size selection";dropdown.transform.SetParent(textOwner,false);Stretch((RectTransform)dropdown.transform);screen.TextSizeDropdown=dropdown.GetComponent<Dropdown>();screen.TextSizeDropdown.ClearOptions();screen.TextSizeDropdown.AddOptions(new List<string>{"100%","125%","150%","200%"});screen.TextSizeDropdown.targetGraphic=AddFrame(dropdown,NativeFrameKind.QuietButton,false);var arrow=dropdown.transform.Find("Arrow");arrow.GetComponent<Image>().enabled=false;var arrowShape=Rect("Editable down arrow",arrow);Stretch((RectTransform)arrowShape.transform);var arrowIcon=arrowShape.AddComponent<NativeIcon>();arrowIcon.Kind=NativeIconKind.Return;arrowIcon.color=Ink;arrow.localRotation=Quaternion.Euler(0,0,90);rows.Add(text);
            screen.SettingRows=rows.ToArray();foreach(var row in screen.SettingRows){StyleText(row.gameObject);row.gameObject.SetActive(row.Tab==0);row.ApplyScale(1);}
        }
        static NativeSettingRow SettingRow(PresentationSurface screen,string id,int tab,string label,out Transform control)
        {
            var g=Rect("Editable setting "+id,screen.Scroll.content);g.transform.SetAsFirstSibling();g.AddComponent<LayoutElement>().preferredHeight=76;var row=g.AddComponent<NativeSettingRow>();row.SettingId=id;row.Tab=tab;
            row.Label=Label("Setting label",g.transform,label,20,Ink,true);row.Label.rectTransform.anchorMin=Vector2.zero;row.Label.rectTransform.anchorMax=new Vector2(.46f,1);row.Label.rectTransform.offsetMin=new Vector2(12,8);row.Label.rectTransform.offsetMax=new Vector2(-10,-8);row.Label.alignment=TextAnchor.MiddleLeft;
            var icon=Rect("Setting semantic icon",g.transform);var glyph=icon.AddComponent<NativeIcon>();glyph.Kind=id=="master"||id=="silent"?NativeIconKind.Conversation:id=="textSize"?NativeIconKind.Document:NativeIconKind.Settings;glyph.color=Ink;var iconRect=(RectTransform)icon.transform;iconRect.anchorMin=iconRect.anchorMax=new Vector2(0,.5f);iconRect.pivot=new Vector2(0,.5f);iconRect.anchoredPosition=new Vector2(6,0);iconRect.sizeDelta=new Vector2(28,28);row.Label.rectTransform.offsetMin=new Vector2(48,8);
            var c=Rect("Editable native setting control",g.transform);row.Control=(RectTransform)c.transform;row.Control.anchorMin=new Vector2(.49f,0);row.Control.anchorMax=Vector2.one;row.Control.offsetMin=new Vector2(0,12);row.Control.offsetMax=new Vector2(-12,-12);control=c.transform;return row;
        }
        static Toggle Toggle(PresentationSurface screen,string id,int tab,string label,List<NativeSettingRow> rows)
        {var row=SettingRow(screen,id,tab,label,out var owner);var g=DefaultControls.CreateToggle(new DefaultControls.Resources());g.name="Native labelled "+id+" toggle";g.transform.SetParent(owner,false);Stretch((RectTransform)g.transform);var t=g.GetComponent<Toggle>();g.GetComponentInChildren<Text>().text="On / off";t.targetGraphic=AddFrame(g.transform.Find("Background").gameObject,NativeFrameKind.QuietButton,false);var check=t.graphic;check.enabled=false;var checkShape=Rect("Editable check shape",check.transform);Stretch((RectTransform)checkShape.transform);var mark=checkShape.AddComponent<NativeIcon>();mark.Kind=NativeIconKind.Check;mark.color=Ink;t.graphic=mark;rows.Add(row);return t;}
        static void StyleExistingCanvas(WorkbenchView view)
        {
            StyleText(view.gameObject);foreach(var button in view.GetComponentsInChildren<Button>(true))StyleButton(button);
            AddFrame(view.HudHeader.gameObject,NativeFrameKind.Navy,true);AddFrame(view.Window,NativeFrameKind.Paper,true);AddFrame(view.LeenaDock.gameObject,NativeFrameKind.Navy,true);AddFrame(view.WorldHelp.gameObject,NativeFrameKind.Navy,false);
            view.HudTitle.font=Heading;view.HudTitle.text="Before You Publish";view.Heading.font=Heading;view.HudTitle.color=view.HudStatus.color=Cream;view.LeenaIdentity.color=Cream;view.LeenaStatus.color=new Color(.69f,.91f,1);view.LeenaIdentity.text="Leena | AI coach";
            foreach(var pool in view.GetComponentsInChildren<NativeRowPool>(true)){pool.TextPrefab=textRow;pool.ActionPrefab=actionRow;pool.InputPrefab=inputRow;foreach(var row in pool.AuthoredRows)if(row){StyleText(row.gameObject);if(row.GetComponent<Image>())row.GetComponent<Image>().color=Color.clear;if(row.Input)AddFrame(row.Input.gameObject,NativeFrameKind.Input,false);}}
            foreach(var field in view.GetComponentsInChildren<InputField>(true))AddFrame(field.gameObject,NativeFrameKind.Input,false);
            // Added art is a native child; existing field and control references remain intact.
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        }
        public static void RefineBoundSurfaces()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Saved refinement requires Stop.");var p=UnityEngine.Object.FindFirstObjectByType<PresentationView>();if(!p)throw new InvalidOperationException("No saved presentation shell.");
            foreach(var s in p.Surfaces)s.ApplyScale(1);EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);
        }
        public static object ApplyObservedReadabilityFixes()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before saved readability fixes.");
            textRow=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"TextRow.prefab").GetComponent<NativeRow>();actionRow=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"ActionRow.prefab").GetComponent<NativeRow>();inputRow=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"InputRow.prefab").GetComponent<NativeRow>();
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root}))
            {var path=AssetDatabase.GUIDToAssetPath(guid);var asset=PrefabUtility.LoadPrefabContents(path);foreach(var surface in asset.GetComponentsInChildren<PresentationSurface>(true)){if(surface.SurfaceId=="title")SeedVisibleTitle(surface);OrderSettingControls(surface);surface.ApplyScale(1);}PrefabUtility.SaveAsPrefabAsset(asset,path);PrefabUtility.UnloadPrefabContents(asset);}
            var p=UnityEngine.Object.FindFirstObjectByType<PresentationView>();StyleExistingCanvas(p.Workbench);
            foreach(var surface in p.Surfaces){if(surface.SurfaceId=="title")SeedVisibleTitle(surface);OrderSettingControls(surface);surface.ApplyScale(1);surface.gameObject.SetActive(surface.SurfaceId=="title");}
            p.Overlay.SetActive(true);p.Workbench.HudHeader.gameObject.SetActive(false);p.Workbench.WorldHelp.gameObject.SetActive(false);
            foreach(var component in p.Workbench.GetComponentsInChildren<Component>(true))if(component){EditorUtility.SetDirty(component);PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);AssetDatabase.SaveAssets();return CampusDiagnostics.SavedAudit();
        }
        static void OrderSettingControls(PresentationSurface surface)
        {
            if(surface.SettingRows==null)return;var order=new[]{"master","silent","textSize","motion","contrast"};foreach(var id in order.Reverse()){var row=surface.SettingRows.FirstOrDefault(r=>r.SettingId==id);if(row)row.transform.SetAsFirstSibling();}
        }
        public static int RepairGraphicsRenderers()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before the bounded native graphic repair.");int added=0;
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var asset=PrefabUtility.LoadPrefabContents(path);bool changed=false;
                foreach(var icon in asset.GetComponentsInChildren<NativeIcon>(true))if(!icon.GetComponent<CanvasRenderer>()){icon.gameObject.AddComponent<CanvasRenderer>();added++;changed=true;}
                if(changed)PrefabUtility.SaveAsPrefabAsset(asset,path);PrefabUtility.UnloadPrefabContents(asset);
            }
            var view=UnityEngine.Object.FindFirstObjectByType<WorkbenchView>();foreach(var icon in view.GetComponentsInChildren<NativeIcon>(true))if(!icon.GetComponent<CanvasRenderer>()){icon.gameObject.AddComponent<CanvasRenderer>();added++;}
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);EditorSceneManager.SaveScene(view.gameObject.scene);AssetDatabase.SaveAssets();return added;
        }
        static void StyleText(GameObject root)
        {foreach(var text in root.GetComponentsInChildren<Text>(true)){text.font=text.name.IndexOf("heading",StringComparison.OrdinalIgnoreCase)>=0||text.name=="Caption"||text.transform.parent.GetComponent<Button>()?Heading:Regular;text.fontStyle=FontStyle.Normal;text.supportRichText=false;if(text.color.r<.7f)text.color=Ink;}}
        static void StyleButton(Button button)
        {if(!button)return;var art=AddFrame(button.gameObject,NativeFrameKind.Button,false);button.targetGraphic=art;SetButtonColours(button);var label=button.GetComponentInChildren<Text>();if(label){label.font=Heading;label.color=Cream;label.fontStyle=FontStyle.Normal;}}
        static void SetButtonColours(Selectable button)
        {var c=button.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.12f,1.12f,1.12f);c.selectedColor=new Color(.65f,1.3f,1.4f);c.pressedColor=new Color(.7f,.82f,.9f);c.disabledColor=new Color(.55f,.57f,.59f);c.fadeDuration=.08f;button.colors=c;}
        internal static NativeFrame AddFrame(GameObject owner,NativeFrameKind kind,bool corners)
        {
            var previous=owner.transform.Find("Editable dimensional frame");var art=previous?previous.gameObject:Rect("Editable dimensional frame",owner.transform);Stretch((RectTransform)art.transform);art.transform.SetAsFirstSibling();var f=art.GetComponent<NativeFrame>();if(!f)f=art.AddComponent<NativeFrame>();f.Kind=kind;f.GoldCorners=corners;f.raycastTarget=false;return f;
        }
        static Button MakeButton(string name,Transform parent,string caption,NativeIconKind icon,bool withIcon=true)
        {
            var g=Rect(name,parent);g.AddComponent<Image>().color=Navy;var button=g.AddComponent<Button>();var outline=g.AddComponent<Outline>();outline.effectColor=new Color(.18f,.78f,.94f);outline.effectDistance=new Vector2(1,-1);var art=AddFrame(g,NativeFrameKind.Button,false);button.targetGraphic=art;SetButtonColours(button);
            var text=Label("Live action label",g.transform,caption,20,Cream,true);text.alignment=TextAnchor.MiddleCenter;Stretch(text.rectTransform,withIcon?40:10,10,6,6);
            if(withIcon){var symbol=Rect("Meaningful action icon",g.transform);var glyph=symbol.AddComponent<NativeIcon>();glyph.Kind=icon;glyph.color=Cream;var rect=(RectTransform)symbol.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(12,0);rect.sizeDelta=new Vector2(24,24);}
            return button;
        }
        static Text Label(string name,Transform parent,string text,int size,Color colour,bool heading=false)
        {var g=Rect(name,parent);var label=g.AddComponent<Text>();label.font=heading?Heading:Regular;label.text=text;label.color=colour;label.fontSize=size;label.supportRichText=false;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;return label;}
        static GameObject Rect(string name,Transform parent){var g=new GameObject(name,typeof(RectTransform));if(parent)g.transform.SetParent(parent,false);return g;}
        static void Stretch(RectTransform r,float left=0,float right=0,float bottom=0,float top=0){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);}
        static void Place(RectTransform r,float x,float y,float width,float height){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
    }
}
