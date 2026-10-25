using System.Collections.Generic;
using System.Linq;

namespace Altaius.Demo02.Domain
{
    // Selective public learner passages only. No private annotations, marks or worked oracle.
    public static class LearnerCatalogue
    {
        public const string FixtureSha256 = "aa4fc7b0bd405d5828bc63d69bdebb588dec09402a3cfadc72b2087d4a325133";
        public const string RuleVersion = "6.0-proposed";
        internal static IReadOnlyList<Passage> Create() => new List<Passage>
        {
            new Passage(new PassageRef("D02.outcome.O1", 1, "bd0990b117044e6692f5ed280bdf6d5cb0c0f227be4fa001e3edf3387e39813c", "D02.outcome.O1.p1"), "Explain two information sources.", InspectionLocation.Studio),
            new Passage(new PassageRef("D02.outcome.O2", 1, "2718bc68d36105cb895f3b8a3ff855307f3ee6308c09d52becb3a84b7ec039b6", "D02.outcome.O2.p1"), "Evaluate two sources and justify which is more reliable for a stated decision.", InspectionLocation.Studio),
            new Passage(new PassageRef("D02.task.v1.Q1", 1, "5aec12d9abcb1a20934e836a625520fbe5dfefa9055f3b296d09393ec1a75df3", "D02.task.v1.Q1.p1"), "Name two sources of information.", InspectionLocation.Studio),
            new Passage(new PassageRef("D02.task.v1.Q2", 1, "a68f761fe7d851bf73965ede6f14f82ca02ff7f19ad4544a3dd7e9493038da26", "D02.task.v1.Q2.p1"), "List three features of a reliable source.", InspectionLocation.Studio),
            new Passage(new PassageRef("D02.rubric.v1.naming", 1, "3bd49af71779608f291e9f2b1b357a08605053c6d1d27d9ea45aa61000c74c88", "D02.rubric.v1.naming.p1"), "naming", InspectionLocation.Studio),
            new Passage(new PassageRef("D02.rubric.v1.listing", 1, "0fc4dfc4feb3b154d20f22308900bf028642220b043804b050d3caaa49dede60", "D02.rubric.v1.listing.p1"), "listing", InspectionLocation.Studio),
            new Passage(new PassageRef("S_A", 1, "e20067e0df992db4a4fdd01cc73799b1ef1d88fb65de2f8e9ab6efe5242331c6", "S_A.p1"), "Facilities counted arrivals during five weekdays in term: 08:00\u201309:00 average 180 arrivals and 45 waiting; 09:00\u201310:00 average 90 arrivals and 5 waiting. Counts taken at the same entrance each day. No weekend data.", InspectionLocation.Archive),
            new Passage(new PassageRef("S_B", 1, "479f0fceb613b24aa3e33e5686e894a3cb4b5545dc28aebb3ee2950e31a7060b", "S_B.p1"), "A supplier brochure says most campuses eliminate morning waiting with its app. No sample, dates, local counts or method are supplied.", InspectionLocation.Archive),
            new Passage(new PassageRef("W_LIST", 1, "451ead07d1527259518a5cf9b848e0356478d956e873dc72373a2a102830dd24", "W_LIST.p1"), "Two information sources are a facilities count and a supplier brochure. Three features of a reliable source are a stated method, current dates and a relevant sample.", InspectionLocation.ExperimentTable),
            new Passage(new PassageRef("W_COMPARE", 1, "c5af6e339f9e2fc26df928b67c04d43bef9323d45a7f57c2f2ce8d85208c1a3e", "W_COMPARE.p1"), "S_A is a local count useful for describing entrance demand; S_B is a supplier claim useful for understanding its offer but not independent evidence of impact. S_A states five term weekdays at the same entrance and 180 arrivals with 45 waiting in the earlier hour, against 90 and 5 later. S_B gives no sample, dates or method. S_A is more relevant to deciding whether to trial a steward at 08:00\u201309:00 on term weekdays. It excludes weekends and does not prove that an added steward would reduce waiting.", InspectionLocation.ExperimentTable),
            new Passage(new PassageRef("W_PITCH", 1, "0c1a2bab89383e8b901933b09367a82e754b12e906c66c31aa769e25a75e3aa1", "W_PITCH.p1"), "A facilities count and a supplier brochure are information sources. The brochure is polished, recent-looking and confident. Its app will eliminate waiting, so buy it instead of testing an entrance steward.", InspectionLocation.ExperimentTable),
        }.AsReadOnly();
    }
}
