using System;
using System.Linq;
using Altaius.Demo02.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace Altaius.Demo02.Gameplay
{
    public interface ICampusWorkspace
    {
        bool IsOpen { get; }
        void OpenStation(CampusStation station);
        void OpenRoutes();
        void Back();
        void Pause();
    }
    /// <summary>Single gameplay surface for ordinary input and the later isolated presenter driver.</summary>
    public sealed class CampusGameplayFacade : MonoBehaviour
    {
        public CampusSession SessionHost;
        public CampusMotor Motor;
        public CampusCamera CameraRig;
        public CampusStation[] Stations;
        public MonoBehaviour WorkspaceBehaviour;
        public Text WorldPrompt;
        public bool PresenterOwnsInput;
        public bool ContactOwnsInput { get; private set; }
        public event Action ContactCancelled;
        public event Action ContactRouteRequested;
        public void SetContactInput(bool owned) { ContactOwnsInput=owned; }
        public CampusStation ActiveStation { get; private set; }
        public string SelectedSpecimenId { get; private set; }
        public bool IsComposing { get; private set; }
        public ICampusWorkspace Workspace => WorkspaceBehaviour as ICampusWorkspace;
        public bool Typing => EventSystem.current && EventSystem.current.currentSelectedGameObject &&
            EventSystem.current.currentSelectedGameObject.TryGetComponent<InputField>(out var f) && f.isFocused;
        public bool WorldOwnsInput => !ContactOwnsInput && !PresenterOwnsInput && !IsComposing && !Typing && !(Workspace?.IsOpen ?? false);
        Vector2 presenterMove;
        void OnEnable() { if (Keyboard.current != null) Keyboard.current.onIMECompositionChange += Composition; }
        void OnDisable() { if (Keyboard.current != null) Keyboard.current.onIMECompositionChange -= Composition; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void Composition(IMECompositionString value) { IsComposing = value.Count > 0; }
        public static bool CanUseWorld(bool panelOpen, bool focusedTyping, bool composing) => !panelOpen && !focusedTyping && !composing;
        void Update()
        {
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (!PresenterOwnsInput && keyboard != null && !IsComposing)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) HandleEscape();
                else if (keyboard.tabKey.wasPressedThisFrame && !(Workspace?.IsOpen ?? false)) Workspace?.OpenRoutes();
                else if (WorldOwnsInput && keyboard.eKey.wasPressedThisFrame) TryInspectNearest();
            }
            Motor.InputBlocked = !CanUseWorld(Workspace?.IsOpen ?? false, Typing, IsComposing);
            var movement = Vector2.zero;
            if (PresenterOwnsInput) movement = presenterMove;
            else if (WorldOwnsInput && keyboard != null)
                movement = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            if(!ContactOwnsInput)Motor.Move(movement, Time.unscaledDeltaTime);
            if (WorldOwnsInput && mouse != null && mouse.rightButton.isPressed) CameraRig.Look(mouse.delta.ReadValue());
            if (WorldPrompt && !(Workspace?.IsOpen ?? false))
            {
                var station = Nearest();
                WorldPrompt.text = station != null ? "E  Inspect " + station.DisplayName + "    |    Tab  Direct routes" : "WASD  Move    |    Hold right mouse  Look    |    Tab  Direct routes";
            }
        }
        public void HandleEscape()
        {
            if(IsComposing)return;
            ContactCancelled?.Invoke();
            // End native field ownership before rebuilding the view. NativeRow also protects
            // the draft if uGUI processed its Escape cancellation earlier in this frame.
            EventSystem.current?.SetSelectedGameObject(null);
            if(Workspace!=null && Workspace.IsOpen)Workspace.Back();else Workspace?.Pause();
        }
        CampusStation Nearest()
        {
            CampusStation nearest = null; float best = float.PositiveInfinity;
            foreach(var station in Stations) { if (!station) continue; var d = station.DistanceTo(Motor.transform.position);
                if (d <= station.InteractionRange && d < best) { best = d; nearest = station; } }
            return nearest;
        }
        public bool TryInspectNearest() { var station = Nearest(); return station != null && Inspect(station); }
        public bool Inspect(CampusStation station)
        {
            if (!station || station.DistanceTo(Motor.transform.position) > station.InteractionRange) return false;
            ActiveStation = station; CameraRig.EnterInspection(station.InspectionCamera); Workspace?.OpenStation(station); return true;
        }
        public bool DirectRoute(string stationId)
        {
            var station = Array.Find(Stations, s => s && s.StationId == stationId);
            if (!station || !station.Arrival) return false;
            ContactRouteRequested?.Invoke();
            CloseInspection(); Motor.GoTo(station.Arrival.position, station.Arrival.rotation); CameraRig.SnapFollow(); return Inspect(station);
        }
        public Passage InspectPassage(string sourceId)
        {
            if (!ActiveStation || ActiveStation.DistanceTo(Motor.transform.position) > ActiveStation.InteractionRange)
                throw new InvalidOperationException("Return to the station to inspect this passage.");
            var p = SessionHost.Session.Inspect(sourceId, ActiveStation.Location); SessionHost.NotifyChanged(); return p;
        }
        public void CloseInspection() { ActiveStation = null; CameraRig.ExitInspection(); }
        public void SelectInspectedWork(string sourceId)
        {
            if(!SessionHost.Session.Snapshot.Notebook.Any(p=>p.Reference.SourceId==sourceId)) throw new InvalidOperationException("Inspect work before selecting it.");
            SelectedSpecimenId=sourceId;
        }
        // Composition-only handback of an existing context, including null. This admits no passage.
        public void RestoreSelectedSpecimen(string sourceId) { SelectedSpecimenId=sourceId; }
        public void DriveMovement(Vector2 movement) { presenterMove = Vector2.ClampMagnitude(movement,1); }
        public void DriveLook(Vector2 delta) { if (CanUseWorld(Workspace?.IsOpen ?? false, Typing, IsComposing)) CameraRig.Look(delta); }
        public void ReleasePresenter() { presenterMove = Vector2.zero; PresenterOwnsInput = false; }
    }
}
