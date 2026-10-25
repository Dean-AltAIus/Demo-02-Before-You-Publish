using System;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.UI
{
    public sealed class RenderedRowDelivery
    {
        public Text Text { get; }
        public bool Help { get; }
        public Guid Identity { get; }
        readonly NativeRow row;readonly PresentationSurface owner;readonly ulong rowGeneration,surfaceGeneration;
        readonly string controlId,content;bool committed;
        public bool Matches => !committed&&Text&&row&&owner&&Identity!=Guid.Empty&&Text.transform.IsChildOf(owner.transform)&&row.Body==Text&&row.BindingGeneration==rowGeneration&&owner.BindingGeneration==surfaceGeneration&&row.ControlId==controlId&&Text.text==content;
        readonly Action commit;
        public RenderedRowDelivery(Text text,PresentationSurface owner,Guid identity,Action commit,bool help=false)
        {Text=text;this.owner=owner;Identity=identity;this.commit=commit;Help=help;row=text?text.GetComponentInParent<NativeRow>():null;rowGeneration=row?row.BindingGeneration:0;surfaceGeneration=owner?owner.BindingGeneration:0;controlId=row?row.ControlId:null;content=text?text.text:null;}
        public bool TryCommit(bool eligible){if(!eligible||!Matches)return false;committed=true;commit();return true;}
    }
}
