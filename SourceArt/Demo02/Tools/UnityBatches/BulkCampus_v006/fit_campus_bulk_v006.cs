if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Outside Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity")throw new System.Exception("Wrong scene");
if(scene.isDirty){string copy="LocalOnly/Validation/Production/pre-fit-unsaved-check.unity";UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,copy,true);if(System.IO.File.ReadAllText(scene.path)!=System.IO.File.ReadAllText(copy))throw new System.Exception("Preserve nonidentical authoring");UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);}
var sign=UnityEngine.GameObject.Find("SOURCE ARCHIVE sign").transform;
var face=UnityEngine.GameObject.Find("SOURCE ARCHIVE cream sign face").transform;
var frame=UnityEngine.GameObject.Find("SOURCE ARCHIVE gold frame").transform;
var foliage=UnityEngine.GameObject.Find("Archive canopy foliage | smooth foliage v002").transform;
if(UnityEngine.Vector3.Distance(sign.position,new UnityEngine.Vector3(0,2.8f,-5.85f))>.001f||UnityEngine.Mathf.Abs(foliage.position.y-2.9f)>.001f)throw new System.Exception("Preserve revised archive placement");
var banners=new System.Collections.Generic.List<UnityEngine.Transform>();foreach(string name in new[]{"Lantern_4.5_-4.4 CampusBanner | academic detail v001","Lantern_4.5_6.3 CampusBanner | academic detail v001"}){var t=UnityEngine.GameObject.Find(name).transform;if(UnityEngine.Mathf.Abs(t.localPosition.x-.38f)>.001f)throw new System.Exception("Preserve revised banner");banners.Add(t);}
UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();
foreach(var t in new[]{sign,face,frame,foliage})UnityEditor.Undo.RecordObject(t,"Fit source archive canopy and live sign");
sign.position=new UnityEngine.Vector3(0,2.45f,-5.48f);face.position=sign.position+sign.forward*.045f;frame.position=sign.position+sign.forward*.075f;
foliage.position+=UnityEngine.Vector3.up*.45f;
foreach(var t in new[]{sign,face,frame,foliage})UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);
foreach(var t in banners){UnityEditor.Undo.RecordObject(t,"Face campus banner inward");var p=t.localPosition;p.x=-.38f;t.localPosition=p;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
UnityEditor.Undo.CollapseUndoOperations(group);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Fitted live archive sign below canopy, foliage at roof edge and two banners inward. Saved existing objects and overrides.";
