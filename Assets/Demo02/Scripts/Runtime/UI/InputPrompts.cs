using Altaius.Demo02.Domain;
namespace Altaius.Demo02.UI
{
    public static class InputPrompts
    {
        public static string For(string id)
        {
            id=id??"";
            if(id=="Mark.Marks")return EnglishCopy.Get("input.mark.integer","Enter a whole number from 0 to the maximum");
            if(id.EndsWith(".Marks"))return EnglishCopy.Get("input.allocation.integer","Enter whole-number marks");
            if(id.EndsWith(".Time"))return EnglishCopy.Get("input.time","Enter a same-day time, HH:MM");
            if(id.EndsWith(".Outcomes"))return EnglishCopy.Get("input.outcomes","Enter O1, O2 or O1,O2");
            if(id.EndsWith(".Question"))return EnglishCopy.Get("input.question.id","Enter Q1 or Q2");
            if(id.EndsWith(".Id"))return EnglishCopy.Get("input.criterion.id","Enter a unique criterion ID");
            if(id=="Mark.Rationale")return EnglishCopy.Get("input.mark.rationale","Explain your marking in your own words...");
            if(id=="Review.Summary")return EnglishCopy.Get("input.review.summary","Describe the changes you want reviewed...");
            if(id=="Hold.Defect")return EnglishCopy.Get("input.hold.defect","Describe the specific unresolved defect...");
            if(id=="Hold.NextAction")return EnglishCopy.Get("input.hold.next","Describe the owner's next action...");
            if(id=="Task.Context")return EnglishCopy.Get("input.task.context","Describe your decision context...");
            return EnglishCopy.Get("input.prose","Write in your own words...");
        }
    }
}
