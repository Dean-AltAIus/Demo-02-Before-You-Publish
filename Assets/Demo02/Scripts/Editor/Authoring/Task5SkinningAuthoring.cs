using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5SkinningAuthoring
    {
        public static string PreserveInfluences()
        {
            if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Clean saved edit scene required.");
            string[] paths={"Assets/Demo02/Art/FinalContacts_v009/AcademicLearner_ContactLibrary_v009.fbx","Assets/Demo02/Art/FinalContacts_v013/AcademicLearner_ContactLibrary_v013.fbx","Assets/Demo02/Art/NPCInteractions_v001/Colleague_Interactions_v001.fbx","Assets/Demo02/Art/NPCInteractions_v001/ModuleLead_Interactions_v019.fbx"};
            foreach(var path in paths){var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=32;importer.minBoneWeight=0;importer.SaveAndReimport();}
            var settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").Single();var serialized=new SerializedObject(settings);var levels=serialized.FindProperty("m_QualitySettings");for(int i=0;i<levels.arraySize;i++)levels.GetArrayElementAtIndex(i).FindPropertyRelative("skinWeights").intValue=(int)SkinWeights.Unlimited;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(settings);QualitySettings.skinWeights=SkinWeights.Unlimited;AssetDatabase.SaveAssets();
            return string.Join("\n",paths.Select(p=>p+" | max influences "+AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.GetBonesPerVertex().ToArray().Max(v=>(int)v)))+"\nSaved quality profiles retain all imported influences. Skinned-surface and performance validation remains required.";
        }
    }
}
