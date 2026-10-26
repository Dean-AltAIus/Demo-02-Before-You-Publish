using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    /// <summary>One reused ID-labelled link ribbon and at most two actual draft totals. No commands or authority callbacks.</summary>
    [DisallowMultipleComponent]
    public sealed class LearningCueProjection : MonoBehaviour
    {
        public PresentationView UI;
        public WorkbenchView Workbench;
        public Image PassageRibbon;
        public Transform RibbonStorage;
        public TextMesh WorldTotal;
        [Range(.01f,.3f)]public float LinkOnset=.12f,LinkDecay=.15f,TotalFade=.1f;
        string linkIdentity,worldValue,uiValue;
        Text uiTotal;
        float linkAlpha,totalAlpha=1;
        void LateUpdate()
        {
            if(!Workbench||!PassageRibbon)return;
            bool reduced=UI&&UI.Applied?.ReducedMotion==true,paused=UI&&UI.Lifecycle?.Paused==true;
            var host=Workbench.Host;var model=host?host.Workbench:null;var snapshot=host&&host.Session!=null?host.Session.Snapshot:null;
            var mark=model?.Mark;
            bool admitted=mark!=null&&snapshot!=null&&model.MarkIsCurrent&&snapshot.Notebook.Any(p=>p.Reference.SourceId==mark.Work.SourceId&&p.Reference.Version==mark.Work.Version&&p.Reference.Sha256==mark.Work.Sha256&&p.Reference.PassageId==mark.Work.PassageId);
            var row=admitted&&Workbench.IsOpen&&Workbench.ViewId=="mark"?Workbench.Rows.Find("Mark.Link"):null;
            bool live=row&&row.gameObject.activeInHierarchy&&row.Caption;
            var identity=live?snapshot.AttemptId+"/"+mark.Work.PassageId+"/"+mark.Work.Version+"/"+mark.TaskVersion+"/"+mark.RubricVersion+"/"+mark.CriterionId:null;
            if(identity!=linkIdentity){linkIdentity=identity;if(identity!=null)linkAlpha=0;}
            if(live)
            {
                var rect=PassageRibbon.rectTransform;rect.SetParent(row.transform,false);rect.SetAsFirstSibling();
                rect.position=row.Caption.rectTransform.TransformPoint(new Vector3(row.Caption.rectTransform.rect.center.x,row.Caption.rectTransform.rect.yMin+3,0));
                rect.sizeDelta=new Vector2(row.Caption.rectTransform.rect.width,3);PassageRibbon.enabled=true;
            }
            else if(PassageRibbon.transform.parent!=RibbonStorage)PassageRibbon.transform.SetParent(RibbonStorage,true);
            if(!admitted){linkAlpha=0;PassageRibbon.enabled=false;}
            if(reduced)linkAlpha=live?1:0;
            else if(!paused)linkAlpha=Mathf.MoveTowards(linkAlpha,live?1:0,Time.unscaledDeltaTime/(live?LinkOnset:LinkDecay));
            var tint=PassageRibbon.color;tint.a=.40f*linkAlpha;PassageRibbon.color=tint;if(!live&&linkAlpha<=0)PassageRibbon.enabled=false;
            // The caption identifies the inspected work and current criterion. The ribbon adds no new meaning.
            var totalRow=Workbench.IsOpen&&Workbench.ViewId=="work"?Workbench.Rows.Find("Work.Total"):null;
            var current=totalRow?totalRow.Caption:null;
            string nextWorld=WorldTotal?WorldTotal.text:null,nextUi=current?current.text:null;
            if(nextWorld!=worldValue||nextUi!=uiValue){worldValue=nextWorld;uiValue=nextUi;totalAlpha=.55f;}
            if(uiTotal&&uiTotal!=current){var old=uiTotal.color;old.a=1;uiTotal.color=old;}uiTotal=current;
            if(reduced)totalAlpha=1;else if(!paused)totalAlpha=Mathf.MoveTowards(totalAlpha,1,.45f*Time.unscaledDeltaTime/TotalFade);
            if(WorldTotal){var c=WorldTotal.color;c.a=totalAlpha;WorldTotal.color=c;}
            if(uiTotal){var c=uiTotal.color;c.a=totalAlpha;uiTotal.color=c;}
        }
        void OnDisable()
        {
            linkIdentity=null;worldValue=null;uiValue=null;linkAlpha=0;totalAlpha=1;
            if(PassageRibbon){PassageRibbon.enabled=false;if(RibbonStorage)PassageRibbon.transform.SetParent(RibbonStorage,false);}
            if(WorldTotal){var c=WorldTotal.color;c.a=1;WorldTotal.color=c;}
            if(uiTotal){var c=uiTotal.color;c.a=1;uiTotal.color=c;}uiTotal=null;
        }
    }
}
