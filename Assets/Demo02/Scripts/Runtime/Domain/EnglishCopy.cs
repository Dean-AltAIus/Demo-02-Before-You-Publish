using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Altaius.Demo02.Domain
{
    /// <summary>Presentation text only. No content, command, source digest or authority is changed.</summary>
    public static class EnglishCopy
    {
        // The saved campus composition installs its native table. Engine-free consumers and
        // isolated domain tests retain exact defaults when no native composition is present.
        public static Func<string,string> Resolver { get; set; }
        public static string Get(string key,string authoredDefault)=>Resolver==null?authoredDefault:Resolver(key);
        public static string Label(Enum state)=>Get("state."+state.GetType().Name+"."+state,state.ToString());
        static readonly Regex Slot=new Regex(@"\{([A-Za-z_][A-Za-z0-9_.]*)\}");
        public static string Format(string key,string authoredDefault,params object[] slots)=>Substitute(Get(key,authoredDefault),slots);
        public static string Substitute(string template,params object[] slots)
        {
            if(template==null)throw new ArgumentNullException(nameof(template));
            if(slots==null||slots.Length%2!=0)throw new ArgumentException("Named slot/value pairs required.");
            var values=new Dictionary<string,string>(StringComparer.Ordinal);
            for(int i=0;i<slots.Length;i+=2)values.Add((string)slots[i],Convert.ToString(slots[i+1],CultureInfo.InvariantCulture)??"");
            // Match evaluator inserts each value once. Braces and punctuation inside payloads
            // are never interpreted, normalized or searched again.
            return Slot.Replace(template,m=>values.TryGetValue(m.Groups[1].Value,out var value)?value:throw new KeyNotFoundException("Missing literal slot: "+m.Groups[1].Value));
        }
    }
}
