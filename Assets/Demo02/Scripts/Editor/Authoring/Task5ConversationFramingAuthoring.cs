using System;
using System.Linq;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Altaius.Demo02.Authoring
{
    public static class Task5ConversationFramingAuthoring
    {
        public static string ApplyOnce()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var d=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();
            if(EditorApplication.isPlaying||scene.isDirty||d.ColleagueConversationCamera||d.StudioSourceCamera)throw new InvalidOperationException("Clean saved campus and absent conversation cameras required.");
            var frame=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.gameObject.scene.IsValid()&&t.name=="Studio contact coordinate frame");
            d.ColleagueConversationCamera=View("Colleague conversation view",frame,new Vector3(-.05f,1.65f,1.65f),new Vector3(-1.55f,1.1f,.1f));
            d.StudioSourceCamera=View("Shared source interaction view",frame,new Vector3(.1f,1.9f,-1.5f),new Vector3(-1.2f,1.05f,.4f));
            EditorUtility.SetDirty(d);EditorSceneManager.SaveScene(scene);return "Saved two editable normal-interaction cameras. Actual framing pending.";
        }
        static Transform View(string name,Transform parent,Vector3 p,Vector3 target){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;t.LookAt(parent.TransformPoint(target));return t;}
    }
}

