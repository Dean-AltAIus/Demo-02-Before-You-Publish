using System;
using System.IO;
using System.Linq;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    public static class Task5StillLifeAuthoring
    {
        const string Folder="Assets/Demo02/Art/FinalPresentation/ReviewStillLife_v002/";
        public static string ImportAndBindOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new InvalidOperationException("Clean saved campus outside Play required.");
            var projection=UnityEngine.Object.FindFirstObjectByType<ReviewArtProjection>();
            if(!projection||projection.Library||File.Exists(Folder+"ReviewArtLibrary.asset"))throw new InvalidOperationException("Preserve existing library and saved overrides.");
            Directory.CreateDirectory(Folder);
            foreach(var source in Directory.GetFiles("SourceArt/Demo02/UI/ReviewStillLife_v002","*.png"))File.Copy(source,Folder+Path.GetFileName(source),false);
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(Folder,"*.png"))
            {var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
            var library=ScriptableObject.CreateInstance<ReviewArtLibrary>();library.GenericFolio=Sprite("folio");
            var names=new[]{"locked","pending","pending","approved","returned","stale","released","unverified-hold","held","unverified-hold","unavailable","unavailable","unknown"};
            library.States=Enum.GetValues(typeof(ReviewArtState)).Cast<ReviewArtState>().Select(state=>new ReviewArtEntry{State=state,Sprite=Sprite(names[(int)state])}).ToArray();
            AssetDatabase.CreateAsset(library,Folder+"ReviewArtLibrary.asset");
            var images=new System.Collections.Generic.List<Image>();
            foreach(var emblem in UnityEngine.Object.FindObjectsByType<NativePaperEmblem>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var g=new GameObject("Editable native still-life render",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));g.transform.SetParent(emblem.transform,false);
                var r=(RectTransform)g.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
                var image=g.GetComponent<Image>();image.sprite=projection.Emblems.Contains(emblem)?library.For(ReviewArtState.Locked):library.GenericFolio;image.preserveAspect=true;image.raycastTarget=false;
                if(projection.Emblems.Contains(emblem))images.Add(image);emblem.enabled=false;
            }
            projection.Library=library;projection.Portraits=images.ToArray();
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved native still-life library with "+library.States.Length+" review states and "+images.Count+" bound review portraits.";
        }
        static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+name+".png");
    }
}
