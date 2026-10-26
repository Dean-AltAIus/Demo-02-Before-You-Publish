using System;
using System.Collections.Generic;
using UnityEngine;

namespace Altaius.Demo02.UI
{
    [CreateAssetMenu(menuName="Altaius/Demo02/English string table")]
    public sealed class NativeEnglishStringTable:ScriptableObject
    {
        [Serializable] public sealed class Entry { public string Key; [TextArea(1,10)] public string English; }
        public Entry[] Entries=Array.Empty<Entry>();
        Dictionary<string,string> lookup;
        void OnEnable()=>Invalidate();
        void OnValidate()=>Invalidate();
        public void Invalidate(){lookup=null;}
        public string Get(string key)
        {
            if(lookup==null)
            {
                var next=new Dictionary<string,string>(StringComparer.Ordinal);
                foreach(var entry in Entries)
                {if(entry==null||string.IsNullOrEmpty(entry.Key))throw new InvalidOperationException("An English entry has no key.");if(next.ContainsKey(entry.Key))throw new InvalidOperationException("Duplicate English key: "+entry.Key);next.Add(entry.Key,entry.English??"");}
                lookup=next;
            }
            if(!lookup.TryGetValue(key,out var value))throw new KeyNotFoundException("Missing English key: "+key);
            return value;
        }
    }
}
