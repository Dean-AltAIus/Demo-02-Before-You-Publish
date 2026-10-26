using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task6ValidationBuild
    {
        public static string Schedule(string output,string committedRevision)
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new InvalidOperationException("Clean stopped saved campus required.");
            if(Directory.Exists(output)||committedRevision.Length!=40)throw new InvalidOperationException("New single build directory and exact committed revision required.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64||!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new InvalidOperationException("Existing installed Windows64 target required.");
            Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"build-started.txt"),DateTime.UtcNow.ToString("O")+"\n"+committedRevision+"\nOne authorized local validation build, DEMO02_VALIDATION, graphics enabled.");
            var scenes=EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled).ToArray();
            File.WriteAllLines(Path.Combine(output,"build-settings-before.txt"),scenes);
            EditorApplication.delayCall+=()=>
            {
                try
                {
                    var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene.path},locationPathName=Path.Combine(output,"Demo02Validation.exe"),target=BuildTarget.StandaloneWindows64,targetGroup=BuildTargetGroup.Standalone,options=BuildOptions.Development,extraScriptingDefines=new[]{"DEMO02_VALIDATION"}});
                    File.WriteAllText(Path.Combine(output,"build-result.json"),JsonUtility.ToJson(new Result{revision=committedRevision,status=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds,bytes=report.summary.totalSize},true));
                }
                catch(Exception e){File.WriteAllText(Path.Combine(output,"build-error.txt"),e.ToString());}
                finally{File.WriteAllLines(Path.Combine(output,"build-settings-after.txt"),EditorBuildSettings.scenes.Select(s=>s.path+"|"+s.enabled));}
            };
            return "Scheduled one validation build at "+output;
        }
        [Serializable]sealed class Result{public string revision,status;public int errors,warnings;public double seconds;public ulong bytes;}
    }
}
