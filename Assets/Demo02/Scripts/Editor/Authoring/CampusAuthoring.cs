using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Altaius.Demo02.Domain;
using Altaius.Demo02.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Altaius.Demo02.Authoring
{
    public static class CampusAuthoring
    {
        public const string ScenePath="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity";
        const string Kit="Assets/Demo02/Prefabs/Environment/Kit_v001/";
        const string Prefabs="Assets/Demo02/Prefabs/Gameplay/";
        const string PreviewMaterial="Assets/Demo02/Settings/Gameplay/PreviewLearner.mat";
        public static IEnumerable<string> NewSceneOutputs => new[]{ScenePath,PreviewMaterial}
            .Concat(new[]{"CourtyardStation","StudioStation","ReviewStation","ArchiveStation","LearnerController","CampusCamera","CampusLighting"}.Select(name=>Prefabs+name+".prefab"))
            .Concat(NativeUiAuthoring.NewOutputs);
        [Serializable] sealed class Layout {public Placement[] placements;}
        [Serializable] sealed class Placement {public string asset,name;public float[] blender_position;public float rotation_z_degrees;}
        [MenuItem("Altaius/Demo 02/Author new campus once")]
        public static void CreateNewScene()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring.");
            AuthoringOutputGuard.RequireAbsent(NewSceneOutputs);
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Preserve the current unsaved scene before authoring.");
            foreach(var path in new[]{"Assets/Demo02/Scenes",Prefabs,"Assets/Demo02/Art/UI","Assets/Demo02/Settings/Gameplay"})Directory.CreateDirectory(path);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var campus=new GameObject("Campus | saved 24 x 18 metres");
            var groups=new Dictionary<string,GameObject>();foreach(var name in new[]{"Courtyard","Studio","Review","Archive"})groups[name]=new GameObject(name+" authored assembly");
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText("SourceArt/Demo02/Environment/campus-placements-v001.json"));
            foreach(var p in layout.placements)
            {
                var key=p.name.StartsWith("Studio")?"Studio":p.name.StartsWith("Review")?"Review":p.name.StartsWith("Archive")?"Archive":"Courtyard";
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit+p.asset+".prefab"),groups[key].transform);
                instance.name=p.name;
                instance.transform.position=new Vector3(p.blender_position[0],p.blender_position[2],-p.blender_position[1]);
                instance.transform.rotation=Quaternion.AngleAxis(p.rotation_z_degrees,Vector3.up)*instance.transform.rotation;
                if(p.asset!="CourtyardPavers_2m" && p.asset!="CourtyardTree" && p.asset!="Folio" && p.asset!="PassageSheet" && p.asset!="AnnotationPen" && p.asset!="CriterionRail")
                    foreach(var mesh in instance.GetComponentsInChildren<MeshFilter>())if(mesh.sharedMesh)mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
            }
            // Source assembly chair crossed the route into the experiment table. Preserve source prefab, use a scene override.
            var chair=GameObject.Find("Studio_LearnerChair");chair.transform.position=new Vector3(-9.05f,0,-.2f);
            var paver=GameObject.Find("Paving_00_00");var groundY=paver.GetComponentsInChildren<Renderer>().Max(r=>r.bounds.max.y);
            var ground=new GameObject("Measured paving collision | top "+groundY.ToString("F3"));ground.transform.SetParent(groups["Courtyard"].transform);var floor=ground.AddComponent<BoxCollider>();floor.center=new Vector3(0,groundY-.1f,0);floor.size=new Vector3(24,.2f,18);
            Boundary(groups["Courtyard"].transform,new Vector3(-12.15f,1,0),new Vector3(.3f,2,18));Boundary(groups["Courtyard"].transform,new Vector3(12.15f,1,0),new Vector3(.3f,2,18));
            Boundary(groups["Courtyard"].transform,new Vector3(0,1,-9.15f),new Vector3(24,2,.3f));Boundary(groups["Courtyard"].transform,new Vector3(0,1,9.15f),new Vector3(24,2,.3f));
            // Physical interaction positions are reachable from the 2.4 m central cross-route.
            var studio=Station(groups["Studio"],"studio","Task and rubric folios",StationKind.Studio,InspectionLocation.Studio,
                new Vector3(-8,.82f,-1.05f),new Vector3(-6.5f,groundY,-.6f),new Vector3(-6.7f,2.6f,-.2f),new Vector3(-8,.7f,-1.2f));
            var experiment=Station(groups["Studio"],"experiment","Experiment table",StationKind.Experiment,InspectionLocation.ExperimentTable,
                new Vector3(-8,.82f,-1.7f),new Vector3(-6.6f,groundY,-2.1f),new Vector3(-6.75f,2.4f,-.3f),new Vector3(-8,.7f,-1.65f));
            var archive=Station(groups["Archive"],"archive","Source archive",StationKind.Archive,InspectionLocation.Archive,
                new Vector3(0,.9f,-5.7f),new Vector3(0,groundY,-4.2f),new Vector3(2.5f,2.4f,-3.5f),new Vector3(0,.9f,-6));
            var review=Station(groups["Review"],"review","Review and publication",StationKind.Review,InspectionLocation.Studio,
                new Vector3(8,.82f,-1.2f),new Vector3(6.5f,groundY,-1.2f),new Vector3(6.7f,2.6f,-.2f),new Vector3(8,.8f,-1.2f));
            studio.EditableFolioHinge=FindContains(GameObject.Find("Studio_Task").transform,"Hinge");
            review.EditableCabinetHinge=FindContains(GameObject.Find("Review_Cabinet").transform,"Hinge");
            Sign(groups["Studio"].transform,"ASSESSMENT STUDIO",new Vector3(-6,3.1f,-1.2f),180);
            Sign(groups["Review"].transform,"REVIEW / PUBLICATION",new Vector3(6,3.1f,-1.2f),180);
            Sign(groups["Archive"].transform,"SOURCE ARCHIVE",new Vector3(0,2.8f,-5.85f),180);
            foreach(var key in groups.Keys.ToArray())
            {
                var group=groups[key];PrefabUtility.SaveAsPrefabAssetAndConnect(group,Prefabs+key+"Station.prefab",InteractionMode.AutomatedAction);group.transform.SetParent(campus.transform);
            }
            var learner=new GameObject("Academic learner | editable visual slot");learner.transform.position=new Vector3(0,groundY,4.5f);learner.layer=2;
            var controller=learner.AddComponent<CharacterController>();controller.center=new Vector3(0,.9f,0);controller.height=1.8f;controller.radius=.28f;controller.stepOffset=.18f;controller.skinWidth=.025f;
            var motor=learner.AddComponent<CampusMotor>();var visual=new GameObject("CharacterVisual | temporary preview until Task 5");visual.transform.SetParent(learner.transform,false);motor.CharacterVisual=visual.transform;
            PreviewBody(visual.transform);visual.transform.rotation=Quaternion.Euler(0,180,0);
            PrefabUtility.SaveAsPrefabAssetAndConnect(learner,Prefabs+"LearnerController.prefab",InteractionMode.AutomatedAction);
            var cameraObject=new GameObject("Campus camera rig");cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();camera.fieldOfView=56;camera.nearClipPlane=.08f;camera.farClipPlane=160;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.56f,.77f,.91f);cameraObject.AddComponent<AudioListener>();
            var rig=cameraObject.AddComponent<CampusCamera>();rig.FollowTarget=learner.transform;rig.Yaw=180;rig.Pitch=20;rig.SnapFollow();motor.ViewCamera=camera;
            PrefabUtility.SaveAsPrefabAssetAndConnect(cameraObject,Prefabs+"CampusCamera.prefab",InteractionMode.AutomatedAction);
            // The learner prefab cannot store a reference to a separate scene camera.
            // Persist this explicit scene binding after saving both prefab instances.
            motor.ViewCamera=camera;PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
            var lightObject=new GameObject("Warm campus sun");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.92f,.78f);light.intensity=2.1f;light.shadows=LightShadows.Soft;lightObject.transform.rotation=Quaternion.Euler(48,-35,0);
            PrefabUtility.SaveAsPrefabAssetAndConnect(lightObject,Prefabs+"CampusLighting.prefab",InteractionMode.AutomatedAction);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.6f,.7f,.8f);RenderSettings.ambientEquatorColor=new Color(.48f,.49f,.43f);RenderSettings.ambientGroundColor=new Color(.3f,.25f,.19f);RenderSettings.sun=light;
            var runtime=new GameObject("Offline gameplay | no live integration");var host=runtime.AddComponent<CampusSession>();var game=runtime.AddComponent<CampusGameplayFacade>();game.SessionHost=host;game.Motor=motor;game.CameraRig=rig;game.Stations=new[]{studio,experiment,archive,review};
            var view=NativeUiAuthoring.Create(game,host);game.WorkspaceBehaviour=view;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};AssetDatabase.SaveAssets();
        }
        static void Boundary(Transform parent,Vector3 position,Vector3 size)
        {var g=new GameObject("Campus perimeter collision");g.transform.SetParent(parent);g.transform.position=position;g.AddComponent<BoxCollider>().size=size;}
        static CampusStation Station(GameObject parent,string id,string label,StationKind kind,InspectionLocation location,Vector3 point,Vector3 arrival,Vector3 cam,Vector3 look)
        {
            var g=new GameObject(label+" interaction");g.transform.SetParent(parent.transform);var s=g.AddComponent<CampusStation>();s.StationId=id;s.DisplayName=label;s.Kind=kind;s.Location=location;
            s.InteractionPoint=Anchor(g.transform,"Interaction point",point);s.Arrival=Anchor(g.transform,"Direct route arrival",arrival);s.Arrival.rotation=Quaternion.LookRotation(new Vector3(point.x-arrival.x,0,point.z-arrival.z));
            s.InspectionCamera=Anchor(g.transform,"Inspection camera anchor",cam);s.InspectionCamera.LookAt(look);
            s.NpcVisualSlot=Anchor(g.transform,"NPC visual slot (Task 5)",kind==StationKind.Review?new Vector3(9.1f,arrival.y,-1.2f):new Vector3(-9.1f,arrival.y,.5f));return s;
        }
        static Transform Anchor(Transform parent,string name,Vector3 position)
        {var g=new GameObject(name);g.transform.SetParent(parent);g.transform.position=position;return g.transform;}
        static Transform FindContains(Transform root,string part)=>root.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.IndexOf(part,StringComparison.OrdinalIgnoreCase)>=0);
        static void Sign(Transform parent,string label,Vector3 pos,float yaw)
        {
            var g=new GameObject(label+" sign");g.transform.SetParent(parent);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);
            var text=g.AddComponent<TextMesh>();text.text=label;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.richText=false;text.fontSize=64;text.characterSize=.04f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.02f,.09f,.18f);
            g.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
        }
        static void PreviewBody(Transform parent)
        {
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.07f,.35f,.48f);AssetDatabase.CreateAsset(mat,PreviewMaterial);
            Part("Preview torso",PrimitiveType.Capsule,parent,new Vector3(0,1.12f,0),new Vector3(.48f,.37f,.3f),mat);
            Part("Preview head",PrimitiveType.Sphere,parent,new Vector3(0,1.67f,0),new Vector3(.29f,.31f,.29f),mat);
            foreach(float x in new[]{-.14f,.14f}){Part("Preview leg",PrimitiveType.Capsule,parent,new Vector3(x,.43f,0),new Vector3(.19f,.39f,.19f),mat);Part("Preview shoe",PrimitiveType.Cube,parent,new Vector3(x,.055f,.06f),new Vector3(.2f,.11f,.32f),mat);}
            foreach(float x in new[]{-.32f,.32f})Part("Preview arm",PrimitiveType.Capsule,parent,new Vector3(x,1.09f,0),new Vector3(.15f,.34f,.15f),mat);
        }
        static void Part(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material mat)
        {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.layer=2;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;}
    }
}
