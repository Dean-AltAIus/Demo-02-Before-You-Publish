using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5SkinImportAudit
    {
        [Serializable]sealed class Index{public FileRef rest;public Pose[] poses;}
        [Serializable]sealed class FileRef{public string path;public int vertices;}
        [Serializable]sealed class Pose{public string path,take;public float frame,seconds;}
        public static string LearnerFixture(string model,string indexFile,string output)
        {
            if(Directory.Exists(output))throw new InvalidOperationException("Preserve existing surface evidence.");
            var index=JsonUtility.FromJson<Index>(File.ReadAllText(indexFile));
            return Run("Assets/Demo02/Art/FinalContacts_v009/AcademicLearner.prefab",model,index.rest.path,index.poses.Select(p=>p.path).ToArray(),index.poses.Select(p=>"D02_Learner_"+p.take+"_v013").ToArray(),index.poses.Select(p=>p.seconds).ToArray(),output);
        }
        public static string Learner()
        {
            const string folder="LocalOnly/Validation/Production/ContactLibrary_v013/skin-import/";var index=JsonUtility.FromJson<Index>(File.ReadAllText(folder+"index.json"));
            return Run("Assets/Demo02/Art/FinalContacts_v009/AcademicLearner.prefab","Assets/Demo02/Art/FinalContacts_v013/AcademicLearner_ContactLibrary_v013.fbx",index.rest.path,index.poses.Select(p=>p.path).ToArray(),index.poses.Select(p=>"D02_Learner_"+p.take+"_v013").ToArray(),index.poses.Select(p=>p.seconds).ToArray(),folder);
        }
        public static string Run(string prefab,string model,string restFile,string[] poseFiles,string[] clips,float[] times,string output)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Saved import comparison outside Play only.");
            var native=Read(restFile);
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefab));actor.hideFlags=HideFlags.HideAndDontSave;actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var source=skin.sharedMesh;var original=source.vertices;var map=new int[original.Length];float restError=0,maximum=0;var lines=new List<string>{"clip,seconds,unity_vertices,max_surface_error_m,rms_error_m,vertices_over_0.1mm,vertices_over_1mm"};
            try
            {
                map=MapRest(skin,restFile,out restError);
                var takes=Directory.Exists(model)?AssetDatabase.FindAssets("t:AnimationClip",new[]{model}).Select(g=>AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray():AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();var baked=new Mesh();
                try
                {
                    for(int p=0;p<poseFiles.Length;p++)
                    {
                        var expected=Read(poseFiles[p]);if(expected.Length!=native.Length)throw new InvalidOperationException("Native vertex order/count changed.");var clip=takes.Single(c=>c.name.EndsWith(clips[p],StringComparison.Ordinal));clip.SampleAnimation(actor,times[p]);skin.BakeMesh(baked,true);var vertices=baked.vertices;if(vertices.Length!=map.Length)throw new InvalidOperationException("Imported vertex topology changed during sampling.");
                        double sum=0;float max=0;int over01=0,over1=0;for(int i=0;i<vertices.Length;i++){float distance=Vector3.Distance(skin.transform.TransformPoint(vertices[i]),expected[map[i]]);max=Mathf.Max(max,distance);sum+=distance*distance;if(distance>.0001f)over01++;if(distance>.001f)over1++;}maximum=Mathf.Max(maximum,max);lines.Add(clips[p]+","+times[p]+","+vertices.Length+","+max+","+Math.Sqrt(sum/vertices.Length)+","+over01+","+over1);
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(baked);}
                Directory.CreateDirectory(output);File.WriteAllLines(Path.Combine(output,"unity-skin-audit.csv"),lines);string report="All "+map.Length+" Unity vertices mapped to "+native.Length+" native rest vertices, max rest error "+restError+" m. "+poseFiles.Length+" complete evaluated poses, max surface error "+maximum+" m. Imported max influences "+source.GetBonesPerVertex().ToArray().Max(n=>(int)n)+", quality "+QualitySettings.skinWeights+". CPU BakeMesh/import fidelity only; rendered playback still required.";File.WriteAllText(Path.Combine(output,"unity-skin-summary.txt"),report);if(restError>.00005f||maximum>.00005f)throw new InvalidOperationException(report);return report;
            }
            finally{UnityEngine.Object.DestroyImmediate(actor);}
        }
        public static int[] MapRest(SkinnedMeshRenderer skin,string restFile,out float restError)
        {
            var native=Read(restFile);var cells=new Dictionary<Vector3Int,List<int>>();for(int i=0;i<native.Length;i++){var key=Cell(native[i]);if(!cells.TryGetValue(key,out var list))cells.Add(key,list=new List<int>());list.Add(i);}
            var original=skin.sharedMesh.vertices;var map=new int[original.Length];restError=0;
            for(int i=0;i<original.Length;i++)
            {
                var v=skin.transform.TransformPoint(original[i]);var cell=Cell(v);float best=float.MaxValue;int found=-1;
                for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)if(cells.TryGetValue(cell+new Vector3Int(x,y,z),out var candidates))foreach(int j in candidates){float distance=(v-native[j]).sqrMagnitude;if(distance<best){best=distance;found=j;}}
                if(found<0)throw new InvalidOperationException("No native rest counterpart for Unity vertex "+i+" at "+v.ToString("F7"));map[i]=found;restError=Mathf.Max(restError,Mathf.Sqrt(best));
            }
            return map;
        }
        static Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.FloorToInt(p.x*10000),Mathf.FloorToInt(p.y*10000),Mathf.FloorToInt(p.z*10000));
        static Vector3[] Read(string path){var bytes=File.ReadAllBytes(path);if(bytes.Length%12!=0)throw new InvalidOperationException("Expected flat native float32 XYZ triplets.");var result=new Vector3[bytes.Length/12];for(int i=0;i<result.Length;i++)result[i]=ContactMotionLibrary.NativePoint(new Vector3(BitConverter.ToSingle(bytes,i*12),BitConverter.ToSingle(bytes,i*12+4),BitConverter.ToSingle(bytes,i*12+8)));return result;}
    }
}
