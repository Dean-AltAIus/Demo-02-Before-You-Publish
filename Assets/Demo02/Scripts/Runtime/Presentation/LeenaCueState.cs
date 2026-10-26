using System;
using System.Linq;
using Altaius.Demo02.Domain;

namespace Altaius.Demo02.Presentation
{
    /// <summary>Read-only presentation clock. It cannot admit a delivery or alter an attempt.</summary>
    public sealed class LeenaCueState
    {
        public Guid Attempt { get; private set; }
        public Guid Delivery { get; private set; }
        public Guid ReportDisplay { get; private set; }
        public string State { get; private set; }="Attention";
        public float Remaining { get; private set; }
        public uint Revision { get; private set; }
        public void Observe(SessionSnapshot snapshot)
        {
            if(Attempt!=snapshot.AttemptId)
            {
                Attempt=snapshot.AttemptId;
                Delivery=snapshot.Assistance.LastOrDefault()?.Id??Guid.Empty;
                ReportDisplay=Guid.Empty;Remaining=0;State="Attention";Revision++;
            }
            var delivered=snapshot.Assistance.LastOrDefault();
            if(delivered==null||delivered.Id==Delivery)return;
            Delivery=delivered.Id;Show(delivered.Debrief?"DeliveredDebrief":"DeliveredHelp");
        }
        public bool ReportRendered(Guid attempt,Guid display)
        {
            if(attempt!=Attempt||display==Guid.Empty||display==ReportDisplay)return false;
            ReportDisplay=display;Show("DeliveredDebrief");return true;
        }
        void Show(string state){State=state;Remaining=8;Revision++;}
        public void Tick(float deltaTime,bool paused)
        {
            if(paused||Remaining<=0)return;
            Remaining=Math.Max(0,Remaining-Math.Max(0,deltaTime));
            if(Remaining==0){State="Attention";Revision++;}
        }
        public void Cancel(){Remaining=0;State="Attention";Revision++;}
    }
}
