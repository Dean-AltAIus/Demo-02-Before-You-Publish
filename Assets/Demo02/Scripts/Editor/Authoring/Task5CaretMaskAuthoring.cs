using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.Authoring
{
    public static class Task5CaretMaskAuthoring
    {
        public static string ApplyOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty)throw new InvalidOperationException("Clean saved campus required.");
            int count=0;
            foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Demo02/Prefabs/UI"}).Select(AssetDatabase.GUIDToAssetPath))
            {var root=PrefabUtility.LoadPrefabContents(path);try{if(Apply(root)>0){count++;PrefabUtility.SaveAsPrefabAsset(root,path);}}finally{PrefabUtility.UnloadPrefabContents(root);}}
            foreach(var root in scene.GetRootGameObjects())Apply(root);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Saved stencil viewport masks in "+count+" UI prefabs. Native InputField caret meshes now inherit viewport clipping through their modified text material.";
        }
        static int Apply(GameObject root)
        {
            int count=0;foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true))
            {
                var viewport=scroll.viewport;if(!viewport||!viewport.GetComponent<RectMask2D>())continue;
                UnityEngine.Object.DestroyImmediate(viewport.GetComponent<RectMask2D>());
                var image=viewport.GetComponent<Image>();if(!image)throw new InvalidOperationException("Expected saved viewport graphic.");image.color=Color.white;
                var mask=viewport.GetComponent<Mask>();if(!mask)mask=viewport.gameObject.AddComponent<Mask>();mask.showMaskGraphic=false;EditorUtility.SetDirty(image);EditorUtility.SetDirty(mask);count++;
            }return count;
        }
    }
}
