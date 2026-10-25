using System.Collections.Generic;
using UnityEngine;

namespace Altaius.Demo02.UI
{
    public sealed class NativeRowPool : MonoBehaviour
    {
        public NativeRow TextPrefab;
        public NativeRow ActionPrefab;
        public NativeRow InputPrefab;
        public List<NativeRow> AuthoredRows = new List<NativeRow>();
        readonly List<NativeRow> runtimeRows = new List<NativeRow>();
        readonly HashSet<NativeRow> used = new HashSet<NativeRow>();
        public void Begin()
        {
            used.Clear(); foreach (var row in AuthoredRows) if (row) row.gameObject.SetActive(false);
            foreach (var row in runtimeRows) if (row) row.gameObject.SetActive(false);
        }
        public NativeRow Take(NativeRowKind kind, string id)
        {
            NativeRow row = null;
            foreach (var r in AuthoredRows) if (r && r.Kind == kind && !used.Contains(r)) { row = r; break; }
            if (!row) foreach (var r in runtimeRows) if (r && r.Kind == kind && !used.Contains(r)) { row = r; break; }
            if (!row)
            {
                var prefab = kind == NativeRowKind.Text ? TextPrefab : kind == NativeRowKind.Action ? ActionPrefab : InputPrefab;
                row = Instantiate(prefab, transform); runtimeRows.Add(row);
            }
            used.Add(row); row.ControlId = id; row.name = "Bound_" + id; row.transform.SetAsLastSibling(); row.gameObject.SetActive(true); return row;
        }
        public NativeRow Find(string id)
        { foreach (var r in used) if (r.ControlId == id) return r; return null; }
    }
}
