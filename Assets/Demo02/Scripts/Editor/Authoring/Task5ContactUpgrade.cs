using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5ContactUpgrade
    {
        [Serializable]sealed class Root{public Track[] tracks;}
        [Serializable]sealed class Track{public string family;public Key[] samples;}
        [Serializable]sealed class Key{public float time;public float[] position,rotation;}
        public static string Apply(int version,int propVersion)
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty)throw new InvalidOperationException("Clean saved campus outside Play required.");
            string suffix="v"+version.ToString("000"),folder="Assets/Demo02/Art/FinalContacts_"+suffix+"/",modelPath=folder+"AcademicLearner_ContactLibrary_"+suffix+".fbx";
            if(AssetDatabase.LoadAssetAtPath<ContactMotionLibrary>(folder+"ContactLibrary.asset"))throw new InvalidOperationException("Preserve the already authored candidate.");
            Directory.CreateDirectory(folder);if(!File.Exists(modelPath))File.Copy("SourceArt/Demo02/Exports/ContactLibrary_"+suffix+"/AcademicLearner_ContactLibrary_"+suffix+".fbx",modelPath);
            AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceSynchronousImport);var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.optimizeGameObjects=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=32;importer.minBoneWeight=0;
            var imports=importer.defaultClipAnimations;foreach(var c in imports){c.loopTime=c.name.Contains("_Idle_")||c.name.Contains("_Walk_");c.loopPose=c.loopTime;c.lockRootRotation=true;c.lockRootPositionXZ=true;c.lockRootHeightY=true;}importer.clipAnimations=imports;importer.SaveAndReimport();
            var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var original=d.Learner.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;var imported=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            if(original.vertexCount!=imported.vertexCount||original.bindposes.Length!=imported.bindposes.Length)throw new InvalidOperationException("A mesh or rig change requires separate character admission.");
            float delta=0;var a=original.vertices;var b=imported.vertices;for(int i=0;i<a.Length;i++)delta=Mathf.Max(delta,(a[i]-b[i]).magnitude);if(delta>.000001f)throw new InvalidOperationException("The clip update changes the admitted character rest mesh: "+delta);
            var clips=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();Func<string,AnimationClip> clip=id=>clips.Single(c=>c.name.EndsWith("D02_Learner_"+id+"_"+suffix,StringComparison.Ordinal));
            var oldController=AssetDatabase.GetAssetPath(d.Learner.runtimeAnimatorController);var controllerPath=folder+"LearnerContacts.controller";AssetDatabase.CopyAsset(oldController,controllerPath);var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            foreach(var state in controller.layers[0].stateMachine.states)
            {
                var s=state.state;if(s.name=="Idle")s.motion=clip("Idle");else if(s.name=="Walk")s.motion=clip("Walk");else if(s.name.StartsWith("D02_Learner_")){var key=s.name.Substring(12);key=key.Substring(0,key.LastIndexOf("_v",StringComparison.Ordinal));s.name="D02_Learner_"+key+"_"+suffix;s.motion=clip(key);}
            }
            var tracks=JsonUtility.FromJson<Root>(File.ReadAllText("SourceArt/Demo02/Exports/ContactPropTracks_v"+propVersion.ToString("000")+"/native-prop-tracks.json"));
            var library=UnityEngine.Object.Instantiate(d.Library);library.name="ContactLibrary";library.SourceReceipt="SourceArt/Demo02/Exports/ContactLibrary_"+suffix+"/receipt.json";
            foreach(var motion in library.Motions)
            {
                motion.Contact=clip(motion.Id);motion.ContactState="D02_Learner_"+motion.Id+"_"+suffix;
                if(motion.Id=="PacketPresent")continue;
                motion.Approach=clip(motion.Id+"Approach");motion.Recovery=clip(motion.Id+"Recovery");motion.ApproachState="D02_Learner_"+motion.Id+"Approach_"+suffix;motion.RecoveryState="D02_Learner_"+motion.Id+"Recovery_"+suffix;
                motion.PropKeys=tracks.tracks.Single(t=>t.family==motion.Id).samples.Select(k=>new NativePropKey{Time=k.time,Position=new Vector3(k.position[0],k.position[1],k.position[2]),Rotation=new Quaternion(k.rotation[0],k.rotation[1],k.rotation[2],k.rotation[3])}).ToArray();
            }
            AssetDatabase.CreateAsset(library,folder+"ContactLibrary.asset");d.Library=library;
            var actorPath=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(d.Learner.gameObject);var actor=PrefabUtility.LoadPrefabContents(actorPath);try{actor.GetComponent<Animator>().runtimeAnimatorController=controller;PrefabUtility.SaveAsPrefabAsset(actor,actorPath);}finally{PrefabUtility.UnloadPrefabContents(actor);}
            d.Learner.runtimeAnimatorController=controller;
            var pen=d.Bindings.Single(x=>x.Id=="MarkPoint");var key0=library.Find("PenPoint").Sample(0);pen.Prop.SetPositionAndRotation(pen.Stance.TransformPoint(ContactMotionLibrary.NativePoint(key0.Position)),pen.Stance.rotation*ContactMotionLibrary.NativeRotation(key0.Rotation)*Quaternion.Euler(-90,0,0));
            var oldRest=d.transform.Find("Pen rest support");if(oldRest)UnityEngine.Object.DestroyImmediate(oldRest.gameObject);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo02/Materials/CampusPalette_v002/D02_BrushedBrass.mat");var rest=new GameObject("Pen rest | angled socket and support");rest.transform.SetParent(d.transform,false);
            var stem=GameObject.CreatePrimitive(PrimitiveType.Cylinder);stem.name="Editable brass pedestal";stem.transform.SetParent(rest.transform,false);stem.transform.localScale=new Vector3(.026f,.02045f,.026f);stem.transform.position=pen.Stance.TransformPoint(ContactMotionLibrary.NativePoint(new Vector3(-.239f,-.353f,.83295f)));stem.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(stem.GetComponent<Collider>());
            var mesh=Socket();AssetDatabase.CreateAsset(mesh,folder+"AngledPenSocket.asset");var socket=new GameObject("Editable open nib socket",typeof(MeshFilter),typeof(MeshRenderer));socket.transform.SetParent(rest.transform,false);socket.GetComponent<MeshFilter>().sharedMesh=mesh;socket.GetComponent<MeshRenderer>().sharedMaterial=material;socket.transform.SetPositionAndRotation(pen.Stance.TransformPoint(ContactMotionLibrary.NativePoint(new Vector3(-.239f,-.353f,.855412f))),pen.Prop.rotation);
            PrefabUtility.SaveAsPrefabAssetAndConnect(rest,folder+"PenRest.prefab",InteractionMode.AutomatedAction);EditorUtility.SetDirty(d);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Upgraded candidate clips/configuration to "+suffix+", unchanged rest mesh delta "+delta+", saved angled pen socket outside selected paper. Runtime acceptance pending.";
        }
        static Mesh Socket()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();for(int ring=0;ring<4;ring++){float z=ring>=2?.008f:0,r=ring%2==0?.004f:.009f;for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32;vertices.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,z));}}
            Action<int,int,int,int> quad=(a,b,c,d)=>{triangles.AddRange(new[]{a,b,c,a,c,d});};
            for(int i=0;i<32;i++){int j=(i+1)%32;quad(i,j,32+j,32+i);quad(64+i,96+i,96+j,64+j);quad(32+i,32+j,96+j,96+i);quad(i,64+i,64+j,j);}
            var mesh=new Mesh{name="Owned editable angled nib socket"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
