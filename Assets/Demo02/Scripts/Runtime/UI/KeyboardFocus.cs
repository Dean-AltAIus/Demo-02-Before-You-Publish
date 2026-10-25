using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Altaius.Demo02.Gameplay;
using System.Linq;

namespace Altaius.Demo02.UI
{
    public sealed class KeyboardFocus : MonoBehaviour
    {
        public CampusGameplayFacade Gameplay;
        public RectTransform FocusRoot;
        public ScrollRect Scroll;
        public Selectable[] AdditionalFocusTargets=System.Array.Empty<Selectable>();
        GameObject last;
        Outline focusOutline;
        Vector2 previousDistance;
        Color previousColor;
        int activatedFrame;
        void OnEnable() { activatedFrame=Time.frameCount; }
        void OnDisable() { RestoreOutline();last=null; }
        void RestoreOutline()
        {if(focusOutline){focusOutline.effectDistance=previousDistance;focusOutline.effectColor=previousColor;}focusOutline=null;}
        void Update()
        {
            if (!Gameplay || Gameplay.PresenterOwnsInput || !(Gameplay.Workspace?.IsOpen ?? false) || !EventSystem.current || Gameplay.IsComposing) return;
            var k = Keyboard.current;
            if(Time.frameCount==activatedFrame)return;
            if(k!=null && k.spaceKey.wasPressedThisFrame && !Gameplay.Typing)
            {
                var selectedButton=EventSystem.current.currentSelectedGameObject;
                if(selectedButton && selectedButton.TryGetComponent<Button>(out var button) && button.IsInteractable())
                    ExecuteEvents.Execute(selectedButton,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
            }
            if (k != null && k.tabKey.wasPressedThisFrame)
            {
                var all = FocusRoot.GetComponentsInChildren<Selectable>().Concat(AdditionalFocusTargets).Where(s => s && s.IsActive() && s.IsInteractable()).Distinct().ToArray();
                if (all.Length > 0)
                {
                    int index = System.Array.FindIndex(all, s => s.gameObject == EventSystem.current.currentSelectedGameObject);
                    index = index<0 ? (k.shiftKey.isPressed?all.Length-1:0) : (index + (k.shiftKey.isPressed ? -1 : 1) + all.Length) % all.Length;
                    EventSystem.current.SetSelectedGameObject(all[index].gameObject);
                }
            }
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected != last)
            {
                RestoreOutline();last=selected;
                if(selected && selected.TryGetComponent<Outline>(out var outline))
                {focusOutline=outline;previousDistance=outline.effectDistance;previousColor=outline.effectColor;outline.effectDistance=new Vector2(3,-3);outline.effectColor=new Color(.05f,.22f,.6f);}
                EnsureVisible(selected);
            }
        }
        public void EnsureVisible(GameObject selected)
        {
            if (!selected || !Scroll || !selected.transform.IsChildOf(Scroll.content)) return;
            Canvas.ForceUpdateCanvases();
            var r = selected.GetComponent<RectTransform>(); if (!r) return;
            var b = RectTransformUtility.CalculateRelativeRectTransformBounds(Scroll.viewport,r);
            var view = Scroll.viewport.rect; var pos = Scroll.content.anchoredPosition;
            if (b.min.y < view.yMin) pos.y += view.yMin - b.min.y + 12;
            else if (b.max.y > view.yMax) pos.y -= b.max.y - view.yMax + 12;
            Scroll.content.anchoredPosition = pos;
        }
    }
}
