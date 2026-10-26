using Altaius.Demo02.Domain;
using Altaius.Demo02.UI;
using NUnit.Framework;
namespace Altaius.Demo02.Tests
{
    public sealed class ReviewArtSafetyTests
    {
        [Test]public void ApprovalDoesNotShowReleaseAndStaleApprovalCannotShowCurrentSeal()
        {
            Assert.That(ReviewArtProjection.Resolve(PublicationStatus.Draft,ReviewStatus.Approved,true),Is.EqualTo(ReviewArtState.Approved));
            Assert.That(ReviewArtProjection.Resolve(PublicationStatus.Draft,ReviewStatus.Approved,false),Is.EqualTo(ReviewArtState.Locked));
            Assert.That(ReviewArtProjection.Resolve(PublicationStatus.Stale,ReviewStatus.Stale,false),Is.EqualTo(ReviewArtState.Stale));
            Assert.That(ReviewArtProjection.Resolve(PublicationStatus.Released,ReviewStatus.Approved,true),Is.EqualTo(ReviewArtState.Released));
        }
        [Test]public void MissingPendingReturnedUnavailableAndUnknownStayDistinct()
        {
            var seen=new System.Collections.Generic.HashSet<ReviewArtState>();
            foreach(var state in new[]{ReviewStatus.Unrequested,ReviewStatus.Pending,ReviewStatus.Returned,ReviewStatus.Unavailable,ReviewStatus.Unknown})
                seen.Add(ReviewArtProjection.Resolve(PublicationStatus.Draft,state,false));
            Assert.That(seen.Count,Is.EqualTo(5));
        }
        [Test]public void RequestedAndUnverifiedHoldsDoNotShowAcceptedHold()
        {
            Assert.That(ReviewArtProjection.Resolve(PublicationStatus.HoldRequested,ReviewStatus.Pending,false),Is.EqualTo(ReviewArtState.HoldRequested));
            Assert.That(ReviewArtProjection.Resolve(PublicationStatus.HoldCommittedUnverified,ReviewStatus.Stale,false),Is.EqualTo(ReviewArtState.UnverifiedHold));
            Assert.That(ReviewArtProjection.Resolve(PublicationStatus.Held,ReviewStatus.Returned,false),Is.EqualTo(ReviewArtState.Held));
        }
    }
}
