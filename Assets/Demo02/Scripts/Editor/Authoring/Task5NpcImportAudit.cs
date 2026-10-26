using System;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5NpcImportAudit
    {
        [Serializable]sealed class Root{public Pose[] samples;}
        [Serializable]sealed class Pose{public string take;public float seconds;public Prop[] props;public Bones bones;}
        [Serializable]sealed class Prop{public string name;public float[] min,max;}
        [Serializable]sealed class Bones{public float[] LeftHand,RightHand,LeftFoot,RightFoot,Head;}
        public static string Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode audit only.");
            string folder="LocalOnly/Validation/Production/NPCUnity_v001/";var input=JsonUtility.FromJson<Root>("{\"samples\":"+File.ReadAllText(folder+"native-baseline.json")+"}");
            var d=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Art/NPCInteractions_v001/ModuleLead.prefab"));actor.hideFlags=HideFlags.HideAndDontSave;actor.transform.SetPositionAndRotation(d.LeadOrigin.position,d.LeadOrigin.rotation);
            var p=d.Packet.position;var q=d.Packet.rotation;var c=d.PacketCover.localRotation;float boneError=0,propError=0,minFloor=float.MaxValue;var lines=new System.Collections.Generic.List<string>{"take,seconds,maximum_bone_error_m,maximum_prop_bounds_error_m,minimum_skin_floor_m"};
            try
            {
                foreach(var pose in input.samples)
                {
                    var take=d.LeadLibrary.Find(pose.take);take.Clip.SampleAnimation(actor,pose.seconds);var k=take.Sample(pose.seconds);d.Packet.SetPositionAndRotation(d.LeadOrigin.TransformPoint(ContactMotionLibrary.NativePoint(k.Position)),d.LeadOrigin.rotation*ContactMotionLibrary.NativeRotation(k.Rotation)*Quaternion.Euler(-90,0,0));d.PacketCover.localRotation=NpcMotionLibrary.ImportedLocalRotation(k.Cover);
                    float be=0,pe=0;foreach(var name in new[]{"LeftHand","RightHand","LeftFoot","RightFoot","Head"}){var xyz=(float[])typeof(Bones).GetField(name).GetValue(pose.bones);var expected=ContactMotionLibrary.NativePoint(V(xyz));var actual=d.LeadOrigin.InverseTransformPoint(actor.GetComponentsInChildren<Transform>().Single(t=>t.name==name).position);be=Mathf.Max(be,Vector3.Distance(actual,expected));}
                    foreach(var prop in pose.props)
                    {var mf=d.Packet.GetComponentsInChildren<MeshFilter>().Single(t=>t.name==prop.name);var points=mf.sharedMesh.vertices.Select(v=>d.LeadOrigin.InverseTransformPoint(mf.transform.TransformPoint(v))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var v in points)bounds.Encapsulate(v);pe=Mathf.Max(pe,Vector3.Distance(bounds.min,V(prop.min)),Vector3.Distance(bounds.max,V(prop.max)));}
                    var renderer=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();renderer.BakeMesh(mesh,true);float floor=mesh.vertices.Min(v=>renderer.transform.TransformPoint(v).y);UnityEngine.Object.DestroyImmediate(mesh);boneError=Mathf.Max(be,boneError);propError=Mathf.Max(pe,propError);minFloor=Mathf.Min(floor,minFloor);lines.Add(pose.take+","+pose.seconds+","+be+","+pe+","+floor);
                }
            }
            finally{d.Packet.SetPositionAndRotation(p,q);d.PacketCover.localRotation=c;UnityEngine.Object.DestroyImmediate(actor);}
            File.WriteAllLines(folder+"unity-import-audit.csv",lines);string result=input.samples.Length+" native poses, "+(input.samples.Length*5)+" bone heads: max "+boneError+" m. Three evaluated prop meshes per pose: max bounds difference "+propError+" m. Skin floor minimum "+minFloor+" m.";File.WriteAllText(folder+"unity-import-summary.txt",result);if(boneError>.00002f||propError>.00002f)throw new InvalidOperationException(result);return result;
        }
        static Vector3 V(float[] v)=>new Vector3(v[0],v[1],v[2]);
    }
}
