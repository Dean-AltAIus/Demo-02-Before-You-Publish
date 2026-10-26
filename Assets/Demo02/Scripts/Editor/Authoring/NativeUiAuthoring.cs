using System;
using System.IO;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Altaius.Demo02.Authoring
{
    internal static class NativeUiAuthoring
    {
        internal static readonly Color Navy=new Color(.035f,.11f,.22f), Cream=new Color(.98f,.965f,.91f), Cobalt=new Color(.035f,.29f,.55f), Teal=new Color(.02f,.49f,.52f);
        const string UI="Assets/Demo02/Prefabs/UI/";
        internal static readonly string[] NewOutputs={UI+"TextRow.prefab",UI+"ActionRow.prefab",UI+"InputRow.prefab",UI+"CampusInterface.prefab"};
        static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        internal static GameObject Rect(string name,Transform parent)
        {var g=new GameObject(name,typeof(RectTransform));if(parent)g.transform.SetParent(parent,false);return g;}
        internal static void Stretch(RectTransform r,float left=0,float right=0,float bottom=0,float top=0)
        {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);}
        internal static GameObject Panel(string name,Transform parent,Color color)
        {var g=Rect(name,parent);g.AddComponent<Image>().color=color;return g;}
        internal static Text Label(string name,Transform parent,string text,int size,Color color)
        {
            var g=Rect(name,parent);var t=g.AddComponent<Text>();t.font=Font;t.text=text;t.fontSize=size;t.color=color;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;t.supportRichText=false;return t;
        }
        internal static Button Button(string name,Transform parent,string caption)
        {
            var g=Panel(name,parent,Cobalt);var b=g.AddComponent<Button>();var colors=b.colors;
            colors.normalColor=Color.white;colors.highlightedColor=new Color(.8f,.95f,1);colors.selectedColor=new Color(.45f,1,1);
            colors.pressedColor=new Color(.65f,.8f,.9f);colors.disabledColor=new Color(.4f,.45f,.5f);b.colors=colors;
            var label=Label("Label",g.transform,caption,20,Color.white);label.alignment=TextAnchor.MiddleCenter;Stretch(label.rectTransform,12,12,6,6);
            var outline=g.AddComponent<Outline>();outline.effectColor=new Color(.1f,.65f,.72f);outline.effectDistance=new Vector2(1,-1);
            return b;
        }
        static void Vertical(GameObject g,int pad=14,int spacing=8,bool fit=true)
        {
            var v=g.AddComponent<VerticalLayoutGroup>();v.padding=new RectOffset(pad,pad,pad,pad);v.spacing=spacing;
            v.childControlHeight=true;v.childControlWidth=true;v.childForceExpandHeight=false;v.childForceExpandWidth=true;
            if(fit)g.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        }
        static NativeRow RowPrefab(NativeRowKind kind)
        {
            var path=UI+kind+"Row.prefab";if(File.Exists(path))throw new InvalidOperationException("Refusing to replace "+path);
            var g=Panel(kind+"Row",null,new Color(1,1,1,.76f));Vertical(g,14,8);
            var row=g.AddComponent<NativeRow>();row.Kind=kind;
            if(kind==NativeRowKind.Action)
            {
                var button=Button("Action",g.transform,"Editable action label");row.Action=button;
                row.Caption=button.GetComponentInChildren<Text>();row.ActionLayout=button.gameObject.AddComponent<LayoutElement>();row.ActionLayout.minHeight=52;
            }
            else
            {
                row.Caption=Label("Caption",g.transform,kind==NativeRowKind.Input?"Editable field label":"Editable heading",20,Navy);row.Caption.fontStyle=FontStyle.Bold;
                if(kind==NativeRowKind.Text)row.Body=Label("Body",g.transform,"Live source-bound text appears here. This saved native text remains editable.",21,Navy);
                else
                {
                    var input=Panel("Input",g.transform,Color.white);var outline=input.AddComponent<Outline>();outline.effectColor=Teal;outline.effectDistance=new Vector2(1,-1);
                    var field=input.AddComponent<InputField>();field.targetGraphic=input.GetComponent<Image>();field.lineType=InputField.LineType.MultiLineNewline;
                    field.selectionColor=new Color(.3f,.75f,.9f,.45f);field.caretColor=Navy;field.customCaretColor=true;field.characterLimit=12000;
                    var text=Label("Text",input.transform,"",21,Navy);Stretch(text.rectTransform,10,10,8,8);text.supportRichText=false;field.textComponent=text;
                    var placeholder=Label("Placeholder",input.transform,"Write in your own words...",21,new Color(.42f,.47f,.52f));Stretch(placeholder.rectTransform,10,10,8,8);field.placeholder=placeholder;
                    row.Input=field;row.InputLayout=input.AddComponent<LayoutElement>();row.InputLayout.preferredHeight=112;
                }
            }
            var asset=PrefabUtility.SaveAsPrefabAsset(g,path).GetComponent<NativeRow>();UnityEngine.Object.DestroyImmediate(g);return asset;
        }
        internal static WorkbenchView Create(CampusGameplayFacade gameplay,CampusSession host)
        {
            AuthoringOutputGuard.RequireAbsent(NewOutputs);
            Directory.CreateDirectory(UI);
            var textPrefab=RowPrefab(NativeRowKind.Text);var actionPrefab=RowPrefab(NativeRowKind.Action);var inputPrefab=RowPrefab(NativeRowKind.Input);
            var canvasObject=Rect("Campus interface",null);var canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            canvasObject.AddComponent<GraphicRaycaster>();var view=canvasObject.AddComponent<WorkbenchView>();view.Gameplay=gameplay;view.Host=host;
            var header=Panel("Campus header",canvasObject.transform,Navy);var hr=(RectTransform)header.transform;hr.anchorMin=new Vector2(0,1);hr.anchorMax=Vector2.one;hr.pivot=new Vector2(.5f,1);hr.sizeDelta=new Vector2(0,78);
            view.HudHeader=hr;
            var title=Label("Title",header.transform,"BEFORE YOU PUBLISH",23,Color.white);title.fontStyle=FontStyle.Bold;Place(title.rectTransform,18,10,410,30);
            view.HudTitle=title;
            view.HudStatus=Label("Current pair and clock",header.transform,"Task 1 / Rubric 1 | 13:20 | Offline",17,new Color(.7f,.9f,1));Place(view.HudStatus.rectTransform,18,44,510,24);
            view.RoutesButton=HeaderButton(header.transform,"Routes",560,120);view.NotebookButton=HeaderButton(header.transform,"Notebook",692,136);view.HistoryButton=HeaderButton(header.transform,"History",840,120);
            view.TextSizeButton=HeaderButton(header.transform,"Text 100%",972,148);view.TextSizeLabel=view.TextSizeButton.GetComponentInChildren<Text>();
            var closeWorld=HeaderButton(header.transform,"Pause",1132,130);
            view.PauseButton=closeWorld;
            // Persist the pause binding, as listener delegates do not survive scene serialization.
            UnityEditor.Events.UnityEventTools.AddPersistentListener(closeWorld.onClick,view.Pause);
            var help=Panel("World controls",canvasObject.transform,new Color(.035f,.11f,.22f,.94f));var helpR=(RectTransform)help.transform;
            view.WorldHelp=helpR;
            helpR.anchorMin=new Vector2(.29f,0);helpR.anchorMax=new Vector2(.99f,0);helpR.pivot=new Vector2(.5f,0);helpR.anchoredPosition=new Vector2(0,12);helpR.sizeDelta=new Vector2(0,48);
            gameplay.WorldPrompt=Label("Control hint",help.transform,"Arrow keys Move | Right mouse Look | E Inspect | Tab Routes",18,Color.white);Stretch(gameplay.WorldPrompt.rectTransform,14,14,6,6);gameplay.WorldPrompt.alignment=TextAnchor.MiddleLeft;
            CreateLeena(canvasObject.transform,view);
            var window=Panel("Editable workspace",canvasObject.transform,Cream);view.Window=window;
            var wr=(RectTransform)window.transform;wr.anchorMin=new Vector2(.30f,0);wr.anchorMax=new Vector2(.99f,1);wr.offsetMin=new Vector2(0,76);wr.offsetMax=new Vector2(0,-90);
            var edge=window.AddComponent<Outline>();edge.effectColor=Cobalt;edge.effectDistance=new Vector2(3,-3);
            view.Heading=Label("Workspace heading",window.transform,"Inspect and mark",27,Navy);view.Heading.fontStyle=FontStyle.Bold;
            var headingR=view.Heading.rectTransform;headingR.anchorMin=new Vector2(0,1);headingR.anchorMax=new Vector2(1,1);headingR.pivot=new Vector2(.5f,1);headingR.offsetMin=new Vector2(20,-44);headingR.offsetMax=new Vector2(-66,-10);
            view.PairLabel=Label("Pair",window.transform,"Task 1 / Rubric 1 | Draft",17,Cobalt);var pr=view.PairLabel.rectTransform;pr.anchorMin=new Vector2(0,1);pr.anchorMax=Vector2.one;pr.pivot=new Vector2(.5f,1);pr.offsetMin=new Vector2(20,-72);pr.offsetMax=new Vector2(-20,-46);
            view.Close=Button("Close",window.transform,"X");var cr=(RectTransform)view.Close.transform;cr.anchorMin=cr.anchorMax=Vector2.one;cr.pivot=Vector2.one;cr.anchoredPosition=new Vector2(-12,-10);cr.sizeDelta=new Vector2(44,40);
            var scrollObject=Rect("Focused reading scroll",window.transform);var sr=(RectTransform)scrollObject.transform;Stretch(sr,16,16,132,80);
            var scroll=scrollObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=36;scroll.inertia=false;view.Scroll=scroll;
            var viewport=Panel("Viewport",scrollObject.transform,new Color(1,1,1,.02f));Stretch((RectTransform)viewport.transform,0,18);viewport.AddComponent<RectMask2D>();scroll.viewport=(RectTransform)viewport.transform;
            var content=Rect("Editable bound rows",viewport.transform);var rect=(RectTransform)content.transform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.zero;Vertical(content,0,12);scroll.content=rect;
            var rows=content.AddComponent<NativeRowPool>();rows.TextPrefab=textPrefab;rows.ActionPrefab=actionPrefab;rows.InputPrefab=inputPrefab;view.Rows=rows;
            // Saved data-bound slots can be inspected and edited before Play; extra runtime rows use these same native prefab sources.
            for(int i=0;i<4;i++)
            {
                foreach(var prefab in new[]{textPrefab,actionPrefab,inputPrefab})
                {var row=(GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject,content.transform);row.name="Authored "+prefab.Kind+" slot "+(i+1);rows.AuthoredRows.Add(row.GetComponent<NativeRow>());row.SetActive(i==0);}
            }
            var bar=Panel("Scroll track",scrollObject.transform,new Color(.8f,.84f,.85f));var br=(RectTransform)bar.transform;br.anchorMin=new Vector2(1,0);br.anchorMax=Vector2.one;br.pivot=new Vector2(1,.5f);br.sizeDelta=new Vector2(12,0);
            var handle=Panel("Scroll handle",bar.transform,Teal);Stretch((RectTransform)handle.transform);var scrollbar=bar.AddComponent<Scrollbar>();scrollbar.handleRect=(RectTransform)handle.transform;scrollbar.targetGraphic=handle.GetComponent<Image>();scrollbar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=scrollbar;
            view.Message=Label("Action result and recovery",window.transform,"Local session. Drafts are not submitted until you save.",17,Navy);var mr=view.Message.rectTransform;mr.anchorMin=new Vector2(0,0);mr.anchorMax=new Vector2(1,0);mr.pivot=new Vector2(.5f,0);mr.offsetMin=new Vector2(20,64);mr.offsetMax=new Vector2(-20,124);
            view.Primary=Button("Primary action",window.transform,"Save");view.PrimaryText=view.Primary.GetComponentInChildren<Text>();var ar=(RectTransform)view.Primary.transform;ar.anchorMin=Vector2.zero;ar.anchorMax=new Vector2(.5f,0);ar.pivot=new Vector2(.5f,0);ar.offsetMin=new Vector2(20,12);ar.offsetMax=new Vector2(-8,56);
            view.Secondary=Button("Secondary action",window.transform,"Back");view.SecondaryText=view.Secondary.GetComponentInChildren<Text>();var er=(RectTransform)view.Secondary.transform;er.anchorMin=new Vector2(.5f,0);er.anchorMax=new Vector2(1,0);er.pivot=new Vector2(.5f,0);er.offsetMin=new Vector2(8,12);er.offsetMax=new Vector2(-20,56);
            var focus=window.AddComponent<KeyboardFocus>();focus.Gameplay=gameplay;focus.FocusRoot=wr;focus.Scroll=scroll;view.Focus=focus;
            view.ConversationExtensionSlot=Rect("NPC conversation extension (unbound)",canvasObject.transform).transform;
            var eventSystem=new GameObject("Native UI EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            window.SetActive(false);
            PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObject,UI+"CampusInterface.prefab",InteractionMode.AutomatedAction);
            return view;
        }
        static Button HeaderButton(Transform parent,string label,float x,float width)
        {var b=Button(label,parent,label);Place((RectTransform)b.transform,x,20,width,40);return b;}
        internal static void Place(RectTransform r,float x,float y,float width,float height)
        {r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
        static void CreateLeena(Transform parent,WorkbenchView view)
        {
            var dock=Panel("Leena persistent offline dock",parent,Navy);var d=(RectTransform)dock.transform;d.anchorMin=d.anchorMax=Vector2.zero;d.pivot=Vector2.zero;d.anchoredPosition=new Vector2(14,12);d.sizeDelta=new Vector2(342,112);
            view.LeenaDock=d;
            var picture=Rect("Canonical Leena unchanged",dock.transform);var image=picture.AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Demo02/Art/UI/Leena_canonical_transparent.png");image.preserveAspect=true;image.raycastTarget=false;
            Place((RectTransform)picture.transform,10,5,94,102);
            var name=Label("Identity",dock.transform,"Leena / AI coach",21,Color.white);Place(name.rectTransform,112,20,220,32);name.fontStyle=FontStyle.Bold;
            view.LeenaIdentity=name;
            var status=Label("Offline status",dock.transform,"Offline | Voice unavailable",17,new Color(.55f,.9f,1));Place(status.rectTransform,112,58,220,40);
            view.LeenaStatus=status;
            view.CoachExtensionSlot=Rect("Coach response extension (unbound)",dock.transform).transform;
        }
    }
}
