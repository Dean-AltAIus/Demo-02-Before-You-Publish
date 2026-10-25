if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Outside Play required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new System.Exception("Preserve unsaved authoring");
string folder="LocalOnly/Validation/Production/World360/Closure_v002";var source=UnityEngine.Camera.main;var rig=source.GetComponent<Altaius.Demo02.Gameplay.CampusCamera>();
var positions=new System.Collections.Generic.List<UnityEngine.Vector3>();var rotations=new System.Collections.Generic.List<UnityEngine.Quaternion>();var names=new System.Collections.Generic.List<string>();
UnityEngine.Physics.SyncTransforms();
foreach(int x in new int[]{-1,1})foreach(int z in new int[]{-1,1})foreach(float pitch in new float[]{8,55}){
 var target=new UnityEngine.Vector3(x*11.2f,0,z*8.2f);float yaw=z<0?(x<0?45:315):(x<0?135:225);var rotation=UnityEngine.Quaternion.Euler(pitch,yaw,0);var origin=target+UnityEngine.Vector3.up*rig.Height;var offset=rotation*new UnityEngine.Vector3(0,0,-rig.Distance);UnityEngine.RaycastHit hit;
 if(UnityEngine.Physics.SphereCast(origin,.18f,offset.normalized,out hit,rig.Distance,rig.CollisionLayers,UnityEngine.QueryTriggerInteraction.Ignore))offset=offset.normalized*UnityEngine.Mathf.Max(0,hit.distance-.12f);
 names.Add("corner-"+x+"-"+z+"-pitch"+pitch);positions.Add(origin+offset);rotations.Add(rotation);
}
foreach(var station in UnityEngine.Object.FindObjectsByType<Altaius.Demo02.Gameplay.CampusStation>(UnityEngine.FindObjectsSortMode.None)){names.Add("inspection-"+station.StationId);positions.Add(station.InspectionCamera.position);rotations.Add(station.InspectionCamera.rotation);}
var active=UnityEngine.RenderTexture.active;UnityEngine.GameObject probe=null;UnityEngine.RenderTexture rt=null;UnityEngine.Texture2D tex=null;
try{
 probe=new UnityEngine.GameObject("Temporary corner and inspection audit");probe.hideFlags=UnityEngine.HideFlags.HideAndDontSave;var cam=probe.AddComponent<UnityEngine.Camera>();cam.CopyFrom(source);cam.enabled=false;var extra=source.GetComponent("UniversalAdditionalCameraData");if(extra)UnityEditor.EditorUtility.CopySerialized(extra,probe.AddComponent(extra.GetType()));
 rt=new UnityEngine.RenderTexture(960,540,24,UnityEngine.RenderTextureFormat.ARGB32);rt.Create();cam.targetTexture=rt;cam.aspect=960f/540f;tex=new UnityEngine.Texture2D(960,540,UnityEngine.TextureFormat.RGB24,false);
 for(int i=0;i<names.Count;i++){var file=folder+"/"+names[i]+".png";if(System.IO.File.Exists(file))throw new System.Exception("Preserve prior capture");cam.transform.SetPositionAndRotation(positions[i],rotations[i]);cam.Render();UnityEngine.RenderTexture.active=rt;tex.ReadPixels(new UnityEngine.Rect(0,0,960,540),0,0);tex.Apply();System.IO.File.WriteAllBytes(file,tex.EncodeToPNG());}
}finally{UnityEngine.RenderTexture.active=active;if(tex)UnityEngine.Object.DestroyImmediate(tex);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(probe)UnityEngine.Object.DestroyImmediate(probe);}
return "Captured "+names.Count+" actual saved-scene corner/inspection views. Original camera, player and UI unchanged.";
