using System;
using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Integration;
namespace Altaius.Demo02.Presentation
{
    public sealed class ConversationDispatchQueue
    {
        readonly ConversationCoordinator conversation;
        readonly Dictionary<Guid,double> pending=new Dictionary<Guid,double>();
        public ConversationDispatchQueue(ConversationCoordinator conversation){this.conversation=conversation;}
        public int PendingCount=>pending.Count;
        public void Schedule(ConversationRecord record,double readyAt)
        {if(record==null||record.State!=ReplyState.Sending)throw new InvalidOperationException("A captured pending conversation request is required.");if(!pending.ContainsKey(record.Request.Id))pending.Add(record.Request.Id,readyAt);}
        public bool Tick(double now,bool paused)
        {if(paused)return false;bool changed=false;foreach(var id in pending.Where(p=>p.Value<=now).Select(p=>p.Key).ToArray()){pending.Remove(id);conversation.Process(id);changed=true;}return changed;}
        public bool Cancel(CancelRequest request)
        {if(!conversation.Cancel(request))return false;pending.Remove(request.RequestId);return true;}
        public void Clear()=>pending.Clear();
    }
}
