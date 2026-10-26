using System;
using Altaius.Demo02.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5CameraOcclusionAuthoring
    {
        public static string ApplyOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var game=UnityEngine.Object.FindFirstObjectByType<CampusGameplayFacade>();
            if(EditorApplication.isPlaying||scene.isDirty||game.CameraRig.LocalBodyRenderers.Length>0)throw new InvalidOperationException("Clean campus with unbound local body visibility required.");
            game.CameraRig.LocalBodyRenderers=game.Motor.CharacterVisual.GetComponentsInChildren<Renderer>(true);EditorUtility.SetDirty(game.CameraRig);PrefabUtility.RecordPrefabInstancePropertyModifications(game.CameraRig);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            return "Saved "+game.CameraRig.LocalBodyRenderers.Length+" learner-only renderer references. Hide below .75m, restore above .95m, inspection always visible, original render flags restored on disable.";
        }
    }
}
