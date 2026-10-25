using System;
using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    public enum NativeRowKind { Text, Action, Input }
    public sealed class NativeRow : MonoBehaviour
    {
        public NativeRowKind Kind;
        public Text Caption;
        public Text Body;
        public InputField Input;
        public Button Action;
        public LayoutElement InputLayout;
        public LayoutElement ActionLayout;
        public string ControlId;
        public void BindText(string title, string body, float scale)
        { Caption.text = title; Body.text = body; ApplyScale(scale); }
        public void BindAction(string title, Action action, float scale, bool available = true)
        {
            Caption.text = title; Action.onClick.RemoveAllListeners(); Action.interactable = available;
            if (action != null) Action.onClick.AddListener(() => action()); ApplyScale(scale);
        }
        public void BindInput(string title, string value, Action<string> changed, float scale, bool multiline)
        {
            Caption.text = title; Input.onValueChanged.RemoveAllListeners(); Input.onEndEdit.RemoveAllListeners();
            Input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            Input.SetTextWithoutNotify(value ?? ""); Input.onValueChanged.AddListener(v => changed(v));
            if (InputLayout) InputLayout.preferredHeight = (multiline ? 112 : 48) * scale;
            ApplyScale(scale);
        }
        public void ApplyScale(float scale)
        {
            if (Caption) Caption.fontSize = Mathf.RoundToInt(20 * scale);
            if (Body) Body.fontSize = Mathf.RoundToInt(21 * scale);
            if (Input) { Input.textComponent.fontSize = Mathf.RoundToInt(21 * scale); if (Input.placeholder is Text p) p.fontSize = Mathf.RoundToInt(21 * scale); }
            if (ActionLayout) ActionLayout.minHeight = 52 * scale;
        }
    }
}
