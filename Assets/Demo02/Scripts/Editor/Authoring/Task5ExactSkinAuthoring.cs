using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Creates independent, editable meshes from native weights without changing the FBX or native rig.</summary>
    public static class Task5ExactSkinAuthoring
    {
        [Serializable] sealed class Package
        {
            public string source,source_sha256,rest,rest_sha256,weights,weights_sha256;
            public string[] bone_names;
            public int[] non_surface_vertices;
            public int vertices,maximum_influences;
        }
        [Serializable] sealed class Receipt
        {
            public string package,package_sha256,source,source_sha256,prefab,original_mesh,mesh;
            public int native_vertices,unity_vertices,mapped_native_vertices,maximum_requested_influences,maximum_saved_influences;
            public int[] non_surface_vertices;
            public float maximum_rest_error_m,maximum_saved_weight_error;
            public string contract="All positive native deform influences mapped by evaluated rest position and bone name. Geometry, UVs, normals, bindposes and editable source rig remain unchanged. Engine weight storage precision is measured, not assumed.";
        }
        public static string CreateAllOnce()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Clean saved edit scene required.");
            const string dest="Assets/Demo02/Art/ExactNativeSkin_v001/";
            if(Directory.Exists(dest))throw new InvalidOperationException("Preserve existing derivative. Choose a new version after source changes.");
            string[] roles={"Learner","ModuleLead","Colleague"};
            string[] prefabs={"Assets/Demo02/Art/FinalContacts_v009/AcademicLearner.prefab","Assets/Demo02/Art/NPCInteractions_v001/ModuleLead.prefab","Assets/Demo02/Art/NPCInteractions_v001/Colleague.prefab"};
            // Validate every source pin before any authored asset mutation.
            foreach(var role in roles)Validate("SourceArt/Demo02/Exports/ExactSkinWeights_v001/"+role+"/");
            Directory.CreateDirectory(dest);AssetDatabase.Refresh();var reports=new List<string>();
            for(int i=0;i<roles.Length;i++)reports.Add(Create(roles[i],prefabs[i],dest));
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());return string.Join("\n",reports);
        }
        public static string CreateRoleOnce(string role)
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Clean saved edit scene required.");
            const string dest="Assets/Demo02/Art/ExactNativeSkin_v001/";
            string prefab=role=="Learner"?"Assets/Demo02/Art/FinalContacts_v009/AcademicLearner.prefab":role=="ModuleLead"||role=="Colleague"?"Assets/Demo02/Art/NPCInteractions_v001/"+role+".prefab":null;
            if(prefab==null||File.Exists(dest+role+".asset")||File.Exists(dest+role+".receipt.json"))throw new InvalidOperationException("Unknown role or existing derivative; preserve authored output.");
            Directory.CreateDirectory(dest);AssetDatabase.Refresh();string result=Create(role,prefab,dest);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());return result;
        }
        static Package Validate(string folder)
        {
            var p=JsonUtility.FromJson<Package>(File.ReadAllText(folder+"receipt.json"));
            if(Hash(p.source)!=p.source_sha256||Hash(folder+p.rest)!=p.rest_sha256||Hash(folder+p.weights)!=p.weights_sha256)throw new InvalidOperationException("Native skin package pin mismatch: "+folder);
            return p;
        }
        static string Create(string role,string prefab,string dest)
        {
            string folder="SourceArt/Demo02/Exports/ExactSkinWeights_v001/"+role+"/";var p=Validate(folder);var actor=PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();float restError;var map=Task5SkinImportAudit.MapRest(skin,folder+p.rest,out restError);
                var mapped=new HashSet<int>(map);var omitted=Enumerable.Range(0,p.vertices).Where(i=>!mapped.Contains(i)).ToArray();
                if(restError>.000002f||!omitted.SequenceEqual(p.non_surface_vertices??new int[0]))throw new InvalidOperationException("Rest topology mapping failed: "+role);
                var names=skin.bones.Select(b=>b.name).ToArray();if(names.Distinct().Count()!=names.Length)throw new InvalidOperationException("Ambiguous renderer bone names.");
                var boneMap=p.bone_names.Select(n=>Array.IndexOf(names,n)).ToArray();if(boneMap.Any(i=>i<0))throw new InvalidOperationException("Native deform bone missing.");
                var records=new BoneWeight1[p.vertices][];
                using(var stream=new BinaryReader(File.OpenRead(folder+p.weights)))
                {
                    for(int i=0;i<records.Length;i++)
                    {
                        int count=stream.ReadByte();if(count<1||count>32)throw new InvalidOperationException("Invalid native influence count.");records[i]=new BoneWeight1[count];double sum=0;
                        for(int j=0;j<count;j++){int index=stream.ReadUInt16();float weight=stream.ReadSingle();if(index>=boneMap.Length||weight<=0||float.IsNaN(weight))throw new InvalidOperationException("Invalid native influence.");records[i][j]=new BoneWeight1{boneIndex=boneMap[index],weight=weight};sum+=weight;}
                        if(Math.Abs(sum-1)>.000001)throw new InvalidOperationException("Native weight normalization failed.");
                    }
                    if(stream.BaseStream.Position!=stream.BaseStream.Length)throw new InvalidOperationException("Trailing native weights.");
                }
                var counts=map.Select(i=>(byte)records[i].Length).ToArray();var weights=map.SelectMany(i=>records[i]).ToArray();
                var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.name=role+"_ExactNativeWeights_v001";string original=AssetDatabase.GetAssetPath(skin.sharedMesh);
                using(var nc=new NativeArray<byte>(counts,Allocator.Temp))using(var nw=new NativeArray<BoneWeight1>(weights,Allocator.Temp))mesh.SetBoneWeights(nc,nw);
                string path=dest+role+".asset";AssetDatabase.CreateAsset(mesh,path);skin.sharedMesh=mesh;skin.quality=SkinQuality.Auto;PrefabUtility.SaveAsPrefabAsset(actor,prefab);
                float maxWeightError=0;int maxCount=0;using(var savedCounts=mesh.GetBonesPerVertex())using(var savedWeights=mesh.GetAllBoneWeights())
                {
                    int offset=0;for(int i=0;i<counts.Length;i++){maxCount=Math.Max(maxCount,savedCounts[i]);var expected=records[map[i]].ToDictionary(w=>w.boneIndex,w=>w.weight);for(int j=0;j<savedCounts[i];j++){var w=savedWeights[offset++];maxWeightError=Mathf.Max(maxWeightError,Mathf.Abs(w.weight-expected[w.boneIndex]));expected.Remove(w.boneIndex);}foreach(float w in expected.Values)maxWeightError=Mathf.Max(maxWeightError,w);}
                }
                var receipt=new Receipt{package=folder+"receipt.json",package_sha256=Hash(folder+"receipt.json"),source=p.source,source_sha256=p.source_sha256,prefab=prefab,original_mesh=original,mesh=path,native_vertices=p.vertices,unity_vertices=map.Length,mapped_native_vertices=map.Distinct().Count(),non_surface_vertices=omitted,maximum_requested_influences=p.maximum_influences,maximum_saved_influences=maxCount,maximum_rest_error_m=restError,maximum_saved_weight_error=maxWeightError};
                File.WriteAllText(dest+role+".receipt.json",JsonUtility.ToJson(receipt,true));return role+": "+map.Length+" vertices, "+maxCount+" stored influences, max weight storage error "+maxWeightError;
            }
            finally{PrefabUtility.UnloadPrefabContents(actor);}
        }
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    }
}
