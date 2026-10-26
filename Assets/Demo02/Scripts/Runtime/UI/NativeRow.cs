using System;
using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    public enum NativeRowKind { Text, Action, Input }
    public sealed class NativeRow : MonoBehaviour
    {
        public NativeUiStyle Style;
        public NativeRowKind Kind;
        public Text Caption;
        public Text Body;
        public InputField Input;
        public Button Action;
        public LayoutElement InputLayout;
        public LayoutElement ActionLayout;
        public string ControlId;
        public ulong BindingGeneration { get; private set; }
        string latestDraft;
        bool cancellationHandled;
        public void BindText(string title, string body, float scale)
        { BindingGeneration++;Caption.supportRichText=false;Body.supportRichText=false;Caption.text = title; Body.text = body; ApplyScale(scale); }
        public void BindAction(string title, Action action, float scale, bool available = true)
        {
            BindingGeneration++;
            Caption.supportRichText=false;Caption.text = title; Action.onClick.RemoveAllListeners(); Action.interactable = available;
            if (action != null) Action.onClick.AddListener(() => action()); ApplyScale(scale);
        }
        public void BindInput(string title, string value, Action<string> changed, float scale, bool multiline)
        {
            BindingGeneration++;
            Caption.supportRichText=false;Caption.text = title; Input.onValueChanged.RemoveAllListeners(); Input.onEndEdit.RemoveAllListeners();
            Input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            latestDraft=value??"";cancellationHandled=Input.wasCanceled;
            Input.SetTextWithoutNotify(latestDraft);
            Input.onValueChanged.AddListener(v =>
            {
                // uGUI rolls back to its activation text during Escape deactivation.
                // That rollback is not a learner edit and must not overwrite the latest draft.
                if(Input.wasCanceled && !cancellationHandled)
                {cancellationHandled=true;Input.SetTextWithoutNotify(latestDraft);return;}
                cancellationHandled=Input.wasCanceled;latestDraft=v;changed(v);
            });
            Input.onEndEdit.AddListener(_=>
            {
                if(!Input.wasCanceled)return;
                cancellationHandled=true;Input.SetTextWithoutNotify(latestDraft);Input.ForceLabelUpdate();
            });
            if (InputLayout) InputLayout.preferredHeight = (multiline ? (Style?Style.MultilineHeight:76) : (Style?Style.InputHeight:34)) * scale;
            ApplyScale(scale);
        }
        public void ApplyScale(float scale)
        {
            if (Caption) Caption.fontSize = Mathf.RoundToInt((Style?Style.BodyFont:14) * scale);
            if (Body) Body.fontSize = Mathf.RoundToInt((Style?Style.BodyFont:14) * scale);
            if (Input) { Input.textComponent.fontSize = Mathf.RoundToInt((Style?Style.BodyFont:14) * scale); if (Input.placeholder is Text p) p.fontSize = Mathf.RoundToInt((Style?Style.BodyFont:14) * scale); }
            if (Input && InputLayout) InputLayout.preferredHeight=(Input.lineType==InputField.LineType.MultiLineNewline?(Style?Style.MultilineHeight:76):(Style?Style.InputHeight:34))*scale;
            if (ActionLayout) ActionLayout.minHeight = (Style?Style.ControlHeight:32) * scale;
        }
    }
}
