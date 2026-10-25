using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Altaius.Demo02.Authoring
{
    /// <summary>Checks the entire write set before an authoring operation mutates scenes or assets.</summary>
    public static class AuthoringOutputGuard
    {
        public static void RequireAbsent(IEnumerable<string> outputPaths)
        {
            var occupied=outputPaths.Distinct().Where(path=>File.Exists(path) || Directory.Exists(path) ||
                File.Exists(path+".meta") || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))).ToArray();
            if(occupied.Length>0)throw new InvalidOperationException(
                "Authoring refused before making changes. Existing outputs or metadata must be preserved:\n"+string.Join("\n",occupied));
        }
    }
}
