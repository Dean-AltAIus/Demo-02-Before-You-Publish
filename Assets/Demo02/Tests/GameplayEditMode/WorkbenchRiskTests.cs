using System;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using NUnit.Framework;

namespace Altaius.Demo02.Gameplay.Tests
{
    public class WorkbenchRiskTests
    {
        static WorkbenchModel Model()
        {
            var session=OfflineSession.Create(SessionMode.Independent);
            session.Inspect("W_LIST",InspectionLocation.ExperimentTable);
            session.Inspect("D02.rubric.v1.naming",InspectionLocation.Studio);
            return new WorkbenchModel(session);
        }
        [Test] public void RetryRetainsOriginalEnvelopeEvenWhenAnInspectionStalesTheReadset()
        {
            var m=Model();var draft=m.OpenMark("W_LIST","naming");draft.Marks="10";draft.Rationale="My own reason.";
            var original=m.StageMark();m.Session.Inspect("S_A",InspectionLocation.Archive);
            var rejected=m.ConfirmPending();Assert.That(rejected.Status,Is.EqualTo(ReceiptStatus.Rejected));
            Assert.That(rejected.ReasonCode,Is.EqualTo("StaleWorldRevision"));
            Assert.That(m.RetryOriginal(),Is.SameAs(rejected));Assert.That(m.StageMark(),Is.SameAs(original));
            Assert.That(draft.Rationale,Is.EqualTo("My own reason."));Assert.That(m.Session.Snapshot.AnnotationHistory,Is.Empty);
            m.StageMark(true);Assert.That(m.ConfirmPending().Status,Is.EqualTo(ReceiptStatus.Accepted));
        }
        [Test] public void CancelAndReturnDoNotEraseDraftOrAdmitAnUninspectedCriterion()
        {
            var m=Model();var d=m.OpenMark("W_LIST","naming");d.Marks="7";d.Rationale="Negated and mixed: not yet supported, but named.";
            m.StageMark();m.CancelPreview();Assert.That(m.Session.Snapshot.AnnotationHistory,Is.Empty);
            Assert.That(m.OpenMark("W_LIST","naming"),Is.SameAs(d));
            Assert.Throws<InvalidOperationException>(()=>m.OpenMark("W_LIST","listing"));
            Assert.That(m.Mark.Rationale,Is.EqualTo(d.Rationale));
        }
        [Test] public void FullBaselineTotalRemainsUnverifiedAndFreeEditMakesAnnotationsHistorical()
        {
            var m=Model();m.Session.Inspect("D02.rubric.v1.listing",InspectionLocation.Studio);
            foreach(var c in new[]{"naming","listing"}){var d=m.OpenMark("W_LIST",c);d.Marks="10";d.Rationale="I assigned this mark to the actual passage.";m.StageMark();m.ConfirmPending();}
            Assert.That(m.Session.Snapshot.DraftTotal("W_LIST"),Is.EqualTo(20));Assert.That(m.Session.Snapshot.CurrentEvaluations,Is.Empty);
            var r=m.OpenRubric();r.Rows[0].Text="My free alternative description with no oracle admission.";
            m.PreviewRubric();m.CancelPreview();Assert.That(m.Session.Snapshot.Rubric.Version,Is.EqualTo(1));
            Assert.That(m.ConfirmPending().Status,Is.EqualTo(ReceiptStatus.Accepted));
            Assert.That(m.Session.Snapshot.CurrentAnnotations,Is.Empty);Assert.That(m.Session.Snapshot.AnnotationHistory.Count,Is.EqualTo(2));
            Assert.That(m.MarkIsCurrent,Is.False);Assert.That(m.Mark.Rationale,Does.Contain("actual passage"));
        }
        [Test] public void CrossQuestionMismatchCannotBePreviewedDespiteCorrectGrandTotal()
        {
            var m=Model();var r=m.OpenRubric();r.Rows[0].Marks="11";r.Rows[1].Marks="9";
            Assert.Throws<InvalidOperationException>(()=>m.PreviewRubric());Assert.That(m.Session.Snapshot.Rubric.Version,Is.EqualTo(1));
            Assert.That(r.Rows[0].Marks,Is.EqualTo("11"));Assert.That(m.Pending,Is.Null);
        }
    }
}
