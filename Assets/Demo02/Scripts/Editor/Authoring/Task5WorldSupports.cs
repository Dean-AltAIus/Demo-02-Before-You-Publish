using System;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5WorldSupports
    {
        const string Folder="Assets/Demo02/Art/FinalPresentation/WorldSupports_v001/";
        public static string AddOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity"||Directory.Exists(Folder))throw new InvalidOperationException("Clean saved campus and absent support batch required. Preserve existing edits.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            var p=UnityEngine.Object.FindFirstObjectByType<CampusExperimentProjection>();
            Material oak=Material("D02_WarmOak"),paper=Material("D02_CreamPaper"),brass=Material("D02_BrushedBrass"),metal=Material("D02_InkMetal");
            var stand=new GameObject("Studio status lectern | editable physical support");stand.transform.position=new Vector3(7.73f,0,-2.25f);
            Part(stand.transform,"Weighted foot",new Vector3(.03f,.025f,0),new Vector3(.54f,.05f,.78f),metal,true);
            Part(stand.transform,"Oak upright",new Vector3(.03f,.51f,0),new Vector3(.065f,.97f,.065f),oak,true);
            Part(stand.transform,"Solid oak notice board",new Vector3(.02f,1.27f,0),new Vector3(.045f,.80f,1.12f),oak,true);
            Part(stand.transform,"Cream readable face",new Vector3(-.005f,1.27f,0),new Vector3(.006f,.75f,1.07f),paper,false);
            foreach(float y in new[]{.875f,1.665f})Part(stand.transform,"Brass horizontal rule",new Vector3(-.010f,y,0),new Vector3(.008f,.012f,1.12f),brass,false);
            foreach(float z in new[]{-.555f,.555f})Part(stand.transform,"Brass edge",new Vector3(-.010f,1.27f,z),new Vector3(.008f,.8f,.012f),brass,false);
            PrefabUtility.SaveAsPrefabAssetAndConnect(stand,Folder+"StudioStatusLectern.prefab",InteractionMode.AutomatedAction);
            foreach(var text in new[]{p.WorkLabel,p.TotalLabel,p.PairLabel}){text.transform.SetParent(stand.transform,true);text.transform.position=new Vector3(7.714f,text==p.WorkLabel?1.51f:text==p.TotalLabel?1.27f:1.015f,-2.25f);}
            p.TotalLabel.characterSize=.014f;
            var lanePath=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(p.AuthoredLanes[0].gameObject);
            var root=PrefabUtility.LoadPrefabContents(lanePath);
            try
            {
                var lane=root.GetComponent<WorldMarkingLane>();
                if(root.transform.Find("Supported criterion paper"))throw new InvalidOperationException("Preserve existing lane support authoring.");
                Part(root.transform,"Supported criterion paper",new Vector3(0,.002f,-.10f),new Vector3(.55f,.004f,.085f),paper,false);
                Part(root.transform,"Supported mark paper",new Vector3(0,.002f,.10f),new Vector3(.55f,.004f,.085f),paper,false);
                lane.CriterionLabel.transform.localPosition=new Vector3(0,.006f,-.10f);lane.MarkLabel.transform.localPosition=new Vector3(0,.006f,.10f);
                lane.PassageProxy.transform.localPosition=new Vector3(0,.001f,.28f);
                PrefabUtility.SaveAsPrefabAsset(root,lanePath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var rotation=Quaternion.Euler(0,345,0);
            for(int i=0;i<p.AuthoredLanes.Count;i++)
            {
                var lane=p.AuthoredLanes[i];lane.transform.SetPositionAndRotation(new Vector3(6.7f,.8125f,-1.2f)+rotation*new Vector3(.62f,0,i==0?.15f:-.34f),rotation);
                lane.CriterionLabel.transform.localPosition=new Vector3(0,.006f,-.10f);lane.MarkLabel.transform.localPosition=new Vector3(0,.006f,.10f);lane.PassageProxy.transform.localPosition=new Vector3(0,.001f,.28f);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved one physical status lectern and supported paper labels on the two criterion lanes; retained TextMesh bindings and state references.";
        }
        static Material Material(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo02/Materials/CampusPalette_v002/"+name+".mat");
        static void Part(Transform parent,string name,Vector3 position,Vector3 dimensions,Material material,bool collision)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=dimensions;g.GetComponent<Renderer>().sharedMaterial=material;if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
    }
}
