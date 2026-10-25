using System;
using System.IO;
using Altaius.Demo02.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Altaius.Demo02.Authoring
{
    public static class CampusTableAuthoring
    {
        const string Folder="Assets/Demo02/Prefabs/Gameplay/";
        const string Kit="Assets/Demo02/Prefabs/Environment/Kit_v001/";
        public static readonly string[] NewOutputs={Folder+"MarkingLane.prefab",Folder+"InspectableFolio.prefab",Folder+"PublicationCabinet.prefab"};
        [MenuItem("Altaius/Demo 02/Add physical table bindings once")]
        public static void AddOnce()
        {
            AuthoringOutputGuard.RequireAbsent(NewOutputs);
            if(EditorApplication.isPlaying || UnityEngine.Object.FindFirstObjectByType<CampusExperimentProjection>())
                throw new InvalidOperationException("Stop Play. Existing table bindings are preserved; this command only creates them once.");
            var root=new GameObject("Editable receipt-bound marking table");
            var projection=root.AddComponent<CampusExperimentProjection>();
            projection.Host=UnityEngine.Object.FindFirstObjectByType<CampusSession>();projection.Gameplay=UnityEngine.Object.FindFirstObjectByType<CampusGameplayFacade>();
            projection.WorkLabel=Text(root.transform,"Selected work","No work selected",new Vector3(-7.7f,1.39f,-.15f),.018f);
            projection.TotalLabel=Text(root.transform,"Draft total","No marks assigned",new Vector3(-7.7f,1.20f,-.15f),.016f);
            projection.PairLabel=Text(root.transform,"Current pair","Task 1 / Rubric 1",new Vector3(-7.7f,1.03f,-.15f),.014f);
            var laneObject=new GameObject("Editable marking lane");var lane=laneObject.AddComponent<WorldMarkingLane>();
            var rail=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"CriterionRail.prefab"),laneObject.transform);
            rail.transform.localPosition=Vector3.zero;
            var sheet=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"PassageSheet.prefab"),laneObject.transform);
            sheet.transform.localPosition=new Vector3(0,.03f,.2f);lane.PassageProxy=sheet;sheet.SetActive(false);
            lane.CriterionLabel=Text(laneObject.transform,"Criterion","Q1 | naming",new Vector3(0,.045f,0),.012f);
            lane.MarkLabel=Text(laneObject.transform,"Mark","Unmarked / 10",new Vector3(0,.045f,.1f),.011f);
            lane.CriterionLabel.transform.localRotation=Quaternion.Euler(90,0,0)*Quaternion.Euler(0,0,180);lane.MarkLabel.transform.localRotation=lane.CriterionLabel.transform.localRotation;
            if(File.Exists(Folder+"MarkingLane.prefab"))throw new InvalidOperationException("Refusing to overwrite marking lane.");
            var prefab=PrefabUtility.SaveAsPrefabAsset(laneObject,Folder+"MarkingLane.prefab");UnityEngine.Object.DestroyImmediate(laneObject);
            projection.AdditionalLanePrefab=prefab.GetComponent<WorldMarkingLane>();
            for(int i=0;i<2;i++)
            {
                var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);g.name="Authored criterion lane "+(i+1);
                g.transform.position=new Vector3(-7.85f,.83f,-1.9f+i*.65f);g.transform.rotation=Quaternion.Euler(0,90,0);
                var item=g.GetComponent<WorldMarkingLane>();item.CriterionLabel.text=i==0?"Q1 | naming":"Q2 | listing";projection.AuthoredLanes.Add(item);
            }
            var extra=new GameObject("Additional editable criterion rows");extra.transform.SetParent(root.transform);extra.transform.position=new Vector3(-7.85f,.83f,-.6f);extra.transform.rotation=Quaternion.Euler(0,90,0);projection.AdditionalLaneRoot=extra.transform;
            var old=GameObject.Find("Studio_Lane");if(old)old.SetActive(false);
            foreach(var name in new[]{"Folio","PublicationCabinet"})
            {
                var path=Folder+(name=="Folio"?"InspectableFolio":"PublicationCabinet")+".prefab";
                if(File.Exists(path))throw new InvalidOperationException("Refusing to overwrite "+path);
                var g=new GameObject(name=="Folio"?"Editable folio source":"Editable cabinet source");PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit+name+".prefab"),g.transform);
                PrefabUtility.SaveAsPrefabAsset(g,path);UnityEngine.Object.DestroyImmediate(g);
            }
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        static TextMesh Text(Transform parent,string name,string value,Vector3 position,float size)
        {
            var g=new GameObject(name);g.transform.SetParent(parent);g.transform.position=position;g.transform.rotation=Quaternion.Euler(0,270,0);
            var t=g.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.richText=false;t.text=value;t.characterSize=size;t.fontSize=64;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(.01f,.08f,.13f);g.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;return t;
        }
    }
}
