using System;
using System.IO;
using System.Linq;
using Altaius.Demo02.Gameplay;
using Altaius.Demo02.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Altaius.Demo02.Authoring
{
    public static class Task5ContactDiagnostics
    {
        public static string PreviewNpc(string takeId,float seconds,string path,bool close=false,int materialProbe=0)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Saved candidate preview is edit mode only.");
            var d=UnityEngine.Object.FindFirstObjectByType<NpcInteractionDirector>();var take=d.LeadLibrary.Find(takeId);var origin=d.LeadOrigin;
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Art/NPCInteractions_v001/ModuleLead.prefab"));actor.hideFlags=HideFlags.HideAndDontSave;actor.transform.SetPositionAndRotation(origin.position,origin.rotation);
            Material probe=null;if(materialProbe>0&&materialProbe<7){var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();probe=materialProbe==4||materialProbe==6?new Material(Shader.Find("Universal Render Pipeline/Unlit")):new Material(skin.sharedMaterial);probe.SetFloat("_SpecularHighlights",0);probe.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");if(materialProbe==2||materialProbe==4||materialProbe==6){probe.SetTexture("_BaseMap",null);probe.SetTexture("_MainTex",null);probe.SetColor("_BaseColor",new Color(.3f,.08f,.22f));}if(materialProbe==3){probe.SetFloat("_ReceiveShadows",0);probe.EnableKeyword("_RECEIVE_SHADOWS_OFF");}if(materialProbe==5||materialProbe==6)probe.SetFloat("_Cull",0);skin.sharedMaterial=probe;}
            var enabled=d.Lead.GetComponentsInChildren<Renderer>().Select(r=>r.enabled).ToArray();var renders=d.Lead.GetComponentsInChildren<Renderer>();foreach(var r in renders)r.enabled=false;
            var oldP=d.Packet.position;var oldQ=d.Packet.rotation;var oldC=d.PacketCover.localRotation;
            var go=new GameObject("Temporary NPC candidate camera"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();var rt=new RenderTexture(1200,900,24,RenderTextureFormat.ARGB32);Texture2D pixels=null;var prior=RenderTexture.active;
            try
            {
                take.Clip.SampleAnimation(actor,seconds);var k=take.Sample(seconds);d.Packet.SetPositionAndRotation(origin.TransformPoint(ContactMotionLibrary.NativePoint(k.Position)),origin.rotation*ContactMotionLibrary.NativeRotation(k.Rotation)*Quaternion.Euler(-90,0,0));d.PacketCover.localRotation=NpcMotionLibrary.ImportedLocalRotation(k.Cover);
                var main=d.UI.Gameplay.CameraRig.GetComponent<Camera>();camera.CopyFrom(main);var a=main.GetUniversalAdditionalCameraData();var b=camera.GetUniversalAdditionalCameraData();b.renderPostProcessing=a.renderPostProcessing;b.volumeLayerMask=a.volumeLayerMask;b.renderShadows=true;b.antialiasing=a.antialiasing;b.antialiasingQuality=a.antialiasingQuality;if(materialProbe==7){b.renderPostProcessing=true;b.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;b.antialiasingQuality=AntialiasingQuality.High;}
                var look=origin.TransformPoint(new Vector3(0,.9f,.30f));camera.transform.position=look+origin.rotation*new Vector3(close?.5f:1.3f,close?.4f:1.0f,close?.8f:1.6f);camera.transform.LookAt(look);camera.orthographic=true;camera.orthographicSize=close?.38f:1.0f;camera.nearClipPlane=.02f;camera.targetTexture=rt;camera.enabled=false;camera.Render();RenderTexture.active=rt;pixels=new Texture2D(1200,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1200,900),0,0);pixels.Apply();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally{d.Packet.SetPositionAndRotation(oldP,oldQ);d.PacketCover.localRotation=oldC;for(int i=0;i<renders.Length;i++)renders[i].enabled=enabled[i];RenderTexture.active=prior;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(actor);if(probe)UnityEngine.Object.DestroyImmediate(probe);if(pixels)UnityEngine.Object.DestroyImmediate(pixels);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            return path;
        }
        public static string Preview(string bindingId,float seconds,string path,bool close=false)
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Saved candidate preview is edit mode only.");
            var director=UnityEngine.Object.FindFirstObjectByType<CampusContactDirector>();var b=director.Bindings.Single(x=>x.Id==bindingId);var motion=director.Library.Find(b.MotionId);
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Art/FinalContacts_v009/AcademicLearner.prefab"));actor.hideFlags=HideFlags.HideAndDontSave;actor.transform.SetPositionAndRotation(b.Stance.position,b.Stance.rotation);
            Vector3 oldP=b.Prop?b.Prop.position:Vector3.zero;Quaternion oldQ=b.Prop?b.Prop.rotation:Quaternion.identity;
            var cameraObject=new GameObject("Temporary contact candidate camera"){hideFlags=HideFlags.HideAndDontSave};var camera=cameraObject.AddComponent<Camera>();var texture=new RenderTexture(1200,900,24,RenderTextureFormat.ARGB32);Texture2D pixels=null;var previous=RenderTexture.active;
            try
            {
                motion.Contact.SampleAnimation(actor,Mathf.Clamp(seconds,0,motion.Duration));var k=motion.Sample(seconds);
                if(b.Prop&&k!=null){b.Prop.position=b.Stance.TransformPoint(ContactMotionLibrary.NativePoint(k.Position));b.Prop.rotation=b.Stance.rotation*ContactMotionLibrary.NativeRotation(k.Rotation)*(b.ImportedNativeGeometry?Quaternion.Euler(-90,0,0):Quaternion.identity);}
                var main=director.Gameplay.CameraRig.GetComponent<Camera>();camera.CopyFrom(main);var source=main.GetUniversalAdditionalCameraData();var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=source.renderPostProcessing;data.volumeLayerMask=source.volumeLayerMask;data.renderShadows=true;
                var look=b.Stance.TransformPoint(new Vector3(close?.12f:0,.95f,.29f));camera.transform.position=look+b.Stance.rotation*new Vector3(close?.6f:1.7f,close?.45f:1.2f,close?.7f:1.9f);camera.transform.LookAt(look);camera.orthographic=true;camera.orthographicSize=close?.36f:.95f;camera.nearClipPlane=.02f;camera.targetTexture=texture;camera.enabled=false;camera.Render();RenderTexture.active=texture;pixels=new Texture2D(1200,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1200,900),0,0);pixels.Apply();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally{if(b.Prop){b.Prop.position=oldP;b.Prop.rotation=oldQ;}RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(actor);if(pixels)UnityEngine.Object.DestroyImmediate(pixels);texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
            return path;
        }
    }
}
