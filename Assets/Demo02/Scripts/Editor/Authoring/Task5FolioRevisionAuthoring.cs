using System;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    /// <summary>Explicit six-clip replacement. Preserves the admitted skin, avatar, prop tracks, stations and UI.</summary>
    public static class Task5FolioRevisionAuthoring
    {
        static readonly string[] Keys={"FolioOpenApproach","FolioOpen","FolioOpenRecovery","FolioCloseApproach","FolioClose","FolioCloseRecovery"};
        [Serializable] class RevisionReceipt { public RevisionClip[] clips; }
        [Serializable] class RevisionClip { public string key; }
        static string[] SelectedKeys(int version)
        {
            var receipt=JsonUtility.FromJson<RevisionReceipt>(File.ReadAllText("SourceArt/Demo02/Exports/ContactLibrary_v"+version.ToString("000")+"/receipt.json"));
            var selected=receipt.clips.Select(c=>c.key).ToArray();if(selected.Length==0||selected.Distinct().Count()!=selected.Length||selected.Any(k=>!Keys.Contains(k)))throw new InvalidOperationException("Receipt must select unique existing M1 keys.");return selected;
        }
        static string Folder(int version)=>"Assets/Demo02/Art/FinalContacts_v"+version.ToString("000")+"/";
        static string Model(int version)=>Folder(version)+"AcademicLearner_ContactLibrary_v"+version.ToString("000")+".fbx";
        static void Clean()
        {var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new InvalidOperationException("Clean saved campus outside Play required.");}
        public static string ImportOnce(int version)
        {
            Clean();string path=Model(version);if(File.Exists(path))throw new InvalidOperationException("Preserve existing imported revision.");
            Directory.CreateDirectory(Folder(version));File.Copy("SourceArt/Demo02/Exports/ContactLibrary_v"+version.ToString("000")+"/"+Path.GetFileName(path),path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.optimizeGameObjects=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=32;importer.minBoneWeight=0;
            var definitions=importer.defaultClipAnimations;foreach(var c in definitions){c.loopTime=false;c.loopPose=false;c.lockRootRotation=c.lockRootPositionXZ=c.lockRootHeightY=true;}importer.clipAnimations=definitions;importer.SaveAndReimport();return Verify(version);
        }
        public static string Verify(int version)
        {
            var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var original=d.Learner.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;var imported=AssetDatabase.LoadAssetAtPath<GameObject>(Model(version)).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            if(original.vertexCount!=imported.vertexCount||original.bindposes.Length!=imported.bindposes.Length)throw new InvalidOperationException("Changed rest topology requires new character admission.");
            float delta=0;var a=original.vertices;var b=imported.vertices;for(int i=0;i<a.Length;i++)delta=Mathf.Max(delta,(a[i]-b[i]).magnitude);if(delta>.000001f)throw new InvalidOperationException("Rest mesh changed: "+delta);
            var selected=SelectedKeys(version);var clips=Clips(version);if(clips.Length!=selected.Length)throw new InvalidOperationException("Imported clip count must match explicit source receipt.");
            string result="Rest mesh delta "+delta+" m; preserved "+original.vertexCount+" vertices and "+original.bindposes.Length+" bind poses.";
            foreach(var key in selected){var c=clips.Single(x=>x.name.EndsWith("D02_Learner_"+key+"_v013",StringComparison.Ordinal));float expected=key.EndsWith("Approach")?19f/30:key.EndsWith("Recovery")?18f/30:15f/30;int keys=AnimationUtility.GetCurveBindings(c).Select(x=>AnimationUtility.GetEditorCurve(c,x).length).Max();if(Mathf.Abs(c.length-expected)>.0001f||c.frameRate<239||keys<Mathf.RoundToInt(expected*240)+1)throw new InvalidOperationException("Dense timing lost: "+key+" length="+c.length+" fps="+c.frameRate+" keys="+keys);result+="\n"+key+" seconds="+c.length+" fps="+c.frameRate+" maximum curve keys="+keys;}
            return result;
        }
        public static string DeriveLinearOnce(int version)
        {
            Clean();string folder=Folder(version)+"NativeLinearClips/";if(Directory.Exists(folder)&&Directory.EnumerateFiles(folder).Any())throw new InvalidOperationException("Preserve existing editable clip derivatives.");
            var originals=AssetDatabase.LoadAllAssetsAtPath(Model(version)).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();if(originals.Length!=SelectedKeys(version).Length)throw new InvalidOperationException("Dense original clips must match receipt.");Directory.CreateDirectory(folder);
            foreach(var original in originals)
            {
                var clip=UnityEngine.Object.Instantiate(original);clip.name=original.name.Substring(original.name.LastIndexOf('|')+1);clip.EnsureQuaternionContinuity();
                foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var curve=AnimationUtility.GetEditorCurve(clip,binding);CompactConstant(curve);for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}AnimationUtility.SetEditorCurve(clip,binding,curve);
                }
                AssetDatabase.CreateAsset(clip,folder+clip.name+".anim");
            }
            AssetDatabase.SaveAssets();return Verify(version)+"\nSaved independent linear interpolation derivatives, matching the native linear quaternion/position keys. Surface revalidation required.";
        }
        public static string CompactExactConstants(int version)
        {
            Clean();int removed=0;foreach(var clip in Clips(version)){if(!AssetDatabase.GetAssetPath(clip).EndsWith(".anim"))throw new InvalidOperationException("Independent derivatives required.");foreach(var b in AnimationUtility.GetCurveBindings(clip)){var c=AnimationUtility.GetEditorCurve(clip,b);int before=c.length;CompactConstant(c);if(c.length<before){removed+=before-c.length;AnimationUtility.SetEditorCurve(clip,b,c);}}EditorUtility.SetDirty(clip);}AssetDatabase.SaveAssets();return "Removed "+removed+" exactly redundant constant keys, no tolerance reduction. "+Verify(version);
        }
        static void CompactConstant(AnimationCurve curve)
        {
            var keys=curve.keys;if(keys.Length>2&&keys.All(k=>k.value==keys[0].value))curve.keys=new[]{new Keyframe(keys[0].time,keys[0].value),new Keyframe(keys[keys.Length-1].time,keys[0].value)};
        }
        public static string VerifyLinearDerivation(int version,string logicalKey=null)
        {
            Clean();var saved=Clips(version);int curves=0,keys=0,quaternionSignChanges=0;float maximum=0,valueMaximum=0,timeMaximum=0;string where="";
            foreach(var original in AssetDatabase.LoadAllAssetsAtPath(Model(version)).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")))
            {
                if(logicalKey!=null&&!original.name.EndsWith("D02_Learner_"+logicalKey+"_v013",StringComparison.Ordinal))continue;
                var expected=UnityEngine.Object.Instantiate(original);try
                {
                    expected.EnsureQuaternionContinuity();var actual=saved.Single(c=>original.name.EndsWith(c.name,StringComparison.Ordinal));
                    foreach(var binding in AnimationUtility.GetCurveBindings(expected)){var curve=AnimationUtility.GetEditorCurve(expected,binding);CompactConstant(curve);for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}AnimationUtility.SetEditorCurve(expected,binding,curve);}
                    // Unity may serialize a whole quaternion curve in the opposite
                    // hemisphere. One constant sign for all four channels is exactly
                    // the same rotation and interpolation, including tangent signs.
                    var signs=new System.Collections.Generic.Dictionary<string,float>();
                    foreach(var binding in AnimationUtility.GetCurveBindings(expected).Where(b=>b.propertyName=="m_LocalRotation.w"))
                    {
                        float dot=0;foreach(char axis in "xyzw"){var part=binding;part.propertyName="m_LocalRotation."+axis;var ec=AnimationUtility.GetEditorCurve(expected,part);var ac=AnimationUtility.GetEditorCurve(actual,part);if(ec==null||ac==null)throw new InvalidOperationException("Missing quaternion channel.");dot+=ec.keys[0].value*ac.keys[0].value;}
                        float sign=dot<0?-1:1;signs[binding.path]=sign;if(sign<0)quaternionSignChanges++;
                    }
                    foreach(var binding in AnimationUtility.GetCurveBindings(expected))
                    {
                        var curve=AnimationUtility.GetEditorCurve(expected,binding);var found=AnimationUtility.GetEditorCurve(actual,binding);if(found==null||found.length!=curve.length)throw new InvalidOperationException("Derivative curve count mismatch: "+binding.path+"/"+binding.propertyName);
                        float sign=binding.propertyName.StartsWith("m_LocalRotation.")?signs[binding.path]:1;var a=curve.keys;var b=found.keys;for(int i=0;i<a.Length;i++){float td=Mathf.Abs(a[i].time-b[i].time),vd=Mathf.Abs(sign*a[i].value-b[i].value),id=Mathf.Abs(sign*a[i].inTangent-b[i].inTangent),od=Mathf.Abs(sign*a[i].outTangent-b[i].outTangent);timeMaximum=Mathf.Max(timeMaximum,td);valueMaximum=Mathf.Max(valueMaximum,vd);float delta=Mathf.Max(td,vd,id,od);if(delta>maximum){maximum=delta;where=actual.name+" "+binding.path+" "+binding.propertyName+" key="+i+"/"+a.Length+" timeDelta="+td+" valueDelta="+vd+" in="+a[i].inTangent+"/"+b[i].inTangent+" out="+a[i].outTangent+"/"+b[i].outTangent;}}curves++;keys+=a.Length;
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(expected);}
            }
            string result="Reconstructed selected clip derivatives in memory from retained FBX: "+curves+" curves, "+keys+" keys, maximum time/value/tangent difference="+maximum+", time="+timeMaximum+", value="+valueMaximum+", constant equivalent quaternion sign changes="+quaternionSignChanges+". Worst: "+where+". No source/asset writes.";if(curves==0||maximum!=0)throw new InvalidOperationException(result);return result;
        }
        public static string BindOnce(int version)
        {
            Clean();Verify(version);string folder=Folder(version);if(File.Exists(folder+"ContactLibrary.asset"))throw new InvalidOperationException("Preserve existing bound revision.");
            var d=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var clips=Clips(version);Func<string,AnimationClip> clip=id=>clips.Single(c=>c.name.EndsWith("D02_Learner_"+id+"_v013",StringComparison.Ordinal));
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(d.Learner.runtimeAnimatorController),folder+"LearnerContacts.controller");var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"LearnerContacts.controller");int changed=0;
            var selected=SelectedKeys(version);foreach(var state in controller.layers[0].stateMachine.states)foreach(var key in selected)if(state.state.name=="D02_Learner_"+key+"_v013"){state.state.motion=clip(key);changed++;}
            if(changed!=selected.Length)throw new InvalidOperationException("Expected every selected existing graph state.");
            var library=UnityEngine.Object.Instantiate(d.Library);library.name="ContactLibrary";library.SourceReceipt="SourceArt/Demo02/Exports/ContactLibrary_v"+version.ToString("000")+"/integration-profile.json";
            foreach(var id in new[]{"FolioOpen","FolioClose"}){var m=library.Find(id);if(selected.Contains(id+"Approach"))m.Approach=clip(id+"Approach");if(selected.Contains(id))m.Contact=clip(id);if(selected.Contains(id+"Recovery"))m.Recovery=clip(id+"Recovery");}
            AssetDatabase.CreateAsset(library,folder+"ContactLibrary.asset");d.Library=library;
            var path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(d.Learner.gameObject);var actor=PrefabUtility.LoadPrefabContents(path);try{actor.GetComponent<Animator>().runtimeAnimatorController=controller;PrefabUtility.SaveAsPrefabAsset(actor,path);}finally{PrefabUtility.UnloadPrefabContents(actor);}
            d.Learner.runtimeAnimatorController=controller;EditorUtility.SetDirty(d);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());return selected.Length+" selected dense M1 clips bound. Every unselected clip, exact skin, avatar, prop tracks and compact UI preserved. Actual playback acceptance pending.";
        }
        static AnimationClip[] Clips(int version)
        {
            string folder=Folder(version)+"NativeLinearClips";
            return Directory.Exists(folder)?AssetDatabase.FindAssets("t:AnimationClip",new[]{folder}).Select(g=>AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray():AssetDatabase.LoadAllAssetsAtPath(Model(version)).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
        }
    }
}
