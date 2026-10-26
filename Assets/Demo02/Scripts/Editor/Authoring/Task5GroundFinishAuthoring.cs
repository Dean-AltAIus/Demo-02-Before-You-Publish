using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5GroundFinishAuthoring
    {
        public static string ApplyOnce()
        {
            const string folder="Assets/Demo02/Art/FinalPresentation/GroundFinish_v001/";var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||Directory.Exists(folder))throw new InvalidOperationException("Clean saved campus and absent ground finish required.");
            var r=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(x=>x.gameObject.scene.IsValid()&&x.name=="Surrounding lawn");var mesh=r.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var uv=mesh.uv;var normal=mesh.normals;
            var ids=Enumerable.Range(0,vertices.Length).Where(i=>Vector3.Dot(r.transform.TransformDirection(normal[i]),Vector3.up)>.999f).ToArray();var u=ids.Max(i=>uv[i].x)-ids.Min(i=>uv[i].x);var v=ids.Max(i=>uv[i].y)-ids.Min(i=>uv[i].y);var scale=new Vector2((r.bounds.size.x/4)/u,(r.bounds.size.z/4)/v);
            Directory.CreateDirectory(folder);string texturePath=folder+"CampusLawn_Albedo_v001.png";File.Copy("SourceArt/Demo02/Exports/CampusLawnFinish_v001/CampusLawn_Albedo_v001.png",texturePath);AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.sRGBTexture=true;importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.anisoLevel=4;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            var material=new Material(r.sharedMaterial);material.name="D02_CampusLawnFinish_v001";material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetColor("_BaseColor",Color.white);material.SetTextureScale("_BaseMap",scale);material.SetFloat("_Smoothness",.1f);material.SetFloat("_SpecularHighlights",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");AssetDatabase.CreateAsset(material,folder+material.name+".mat");
            string prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(r.gameObject);var root=PrefabUtility.LoadPrefabContents(prefab);try{var saved=root.GetComponentInChildren<MeshRenderer>();saved.sharedMaterial=material;saved.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;PrefabUtility.SaveAsPrefabAsset(root,prefab);}finally{PrefabUtility.UnloadPrefabContents(root);}
            r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;EditorUtility.SetDirty(r);PrefabUtility.RecordPrefabInstancePropertyModifications(r);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            string report="Ground geometry and footprint unchanged. Existing UV top span "+u+","+v+"; material tiling "+scale+" gives a 4m tile. Ground-only casting off removes proven self-shadow diagonal, reception retained.";File.WriteAllText("LocalOnly/Validation/Production/GroundMaterial_v001/admission.txt",report);return report;
        }
    }
}
