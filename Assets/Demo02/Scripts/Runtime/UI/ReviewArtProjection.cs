using System.Linq;
using Altaius.Demo02.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Altaius.Demo02.UI
{
    public enum ReviewArtState { Locked, Pending, Scheduled, Approved, Returned, Stale, Released, HoldRequested, Held, UnverifiedHold, Denied, Unavailable, Unknown }
    /// <summary>Native status objects are projections of the same current receipts as the readable controls.</summary>
    [DisallowMultipleComponent]
    public sealed class ReviewArtProjection : MonoBehaviour
    {
        public PresentationView UI;
        public NativePaperEmblem[] Emblems;
        public NativeIcon ReviewHeaderIcon;
        public ReviewArtLibrary Library;
        public Image[] Portraits;
        [Range(0,.2f)]public float StatusFadeSeconds=.1f;
        public ReviewArtState Current { get; private set; }
        float opacity=1;
        bool initialized;
        public static ReviewArtState Resolve(PublicationStatus publication,ReviewStatus review,bool currentApproval)
        {
            if(publication==PublicationStatus.Released)return ReviewArtState.Released;
            if(publication==PublicationStatus.Held)return ReviewArtState.Held;
            if(publication==PublicationStatus.HoldCommittedUnverified)return ReviewArtState.UnverifiedHold;
            if(publication==PublicationStatus.Stale||review==ReviewStatus.Stale)return ReviewArtState.Stale;
            if(publication==PublicationStatus.HoldRequested)return ReviewArtState.HoldRequested;
            switch(review)
            {
                case ReviewStatus.Pending:return ReviewArtState.Pending;
                case ReviewStatus.Scheduled:return ReviewArtState.Scheduled;
                case ReviewStatus.Approved:return currentApproval?ReviewArtState.Approved:ReviewArtState.Locked;
                case ReviewStatus.Returned:return ReviewArtState.Returned;
                case ReviewStatus.Denied:return ReviewArtState.Denied;
                case ReviewStatus.Unavailable:return ReviewArtState.Unavailable;
                case ReviewStatus.Unknown:return ReviewArtState.Unknown;
                default:return ReviewArtState.Locked;
            }
        }
        void LateUpdate()
        {
            if(!UI||!UI.Host||UI.Host.Session==null)return;
            var s=UI.Host.Session.Snapshot;var next=Resolve(s.Publication,s.ReviewStatus,s.CurrentApproval!=null);
            if(initialized&&Current!=next)opacity=.45f;Current=next;initialized=true;
            if(UI.Applied?.ReducedMotion==true||StatusFadeSeconds<=0||Current==ReviewArtState.Stale||Current==ReviewArtState.Locked)opacity=1;
            else if(UI.Lifecycle?.Paused!=true)opacity=Mathf.MoveTowards(opacity,1,.55f*Time.unscaledDeltaTime/StatusFadeSeconds);
            foreach(var emblem in Emblems)if(emblem){emblem.BindReview(Current);var tint=emblem.color;tint.a=opacity;emblem.color=tint;}
            var sprite=Library?Library.For(Current):null;
            if(Portraits!=null)foreach(var portrait in Portraits)if(portrait){portrait.enabled=sprite!=null;if(sprite){portrait.sprite=sprite;portrait.color=new Color(1,1,1,opacity);}}
            foreach(var emblem in Emblems)if(emblem)emblem.enabled=!sprite;
            if(ReviewHeaderIcon)
            {
                var kind=Current==ReviewArtState.Released?NativeIconKind.Check:Current==ReviewArtState.Returned?NativeIconKind.Return:
                    Current==ReviewArtState.Held||Current==ReviewArtState.UnverifiedHold||Current==ReviewArtState.HoldRequested?NativeIconKind.Pause:
                    Current==ReviewArtState.Pending||Current==ReviewArtState.Scheduled?NativeIconKind.Clock:NativeIconKind.Lock;
                if(ReviewHeaderIcon.Kind!=kind){ReviewHeaderIcon.Kind=kind;ReviewHeaderIcon.SetVerticesDirty();}
            }
        }
    }
}
