using System;
using System.Linq;
using UnityEngine;
namespace Altaius.Demo02.Gameplay
{
    [Serializable] public sealed class NativePropKey
    {
        public float Time;public Vector3 Position;public Quaternion Rotation=Quaternion.identity;
    }
    [Serializable] public sealed class ContactMotion
    {
        public string Id;public AnimationClip Approach,Contact,Recovery;
        public string ApproachState,ContactState,RecoveryState;
        public NativePropKey[] PropKeys;
        public float Duration=>Contact?Contact.length:0;
        public NativePropKey Sample(float seconds)
        {
            if(PropKeys==null||PropKeys.Length==0)return null;
            if(seconds<=PropKeys[0].Time)return PropKeys[0];
            for(int i=1;i<PropKeys.Length;i++)if(seconds<=PropKeys[i].Time)
            {var a=PropKeys[i-1];var b=PropKeys[i];var t=Mathf.InverseLerp(a.Time,b.Time,seconds);return new NativePropKey{Time=seconds,Position=Vector3.Lerp(a.Position,b.Position,t),Rotation=Quaternion.Slerp(a.Rotation,b.Rotation,t)};}
            return PropKeys[PropKeys.Length-1];
        }
    }
    [CreateAssetMenu(menuName="Demo02/Owned contact motion library")]
    public sealed class ContactMotionLibrary:ScriptableObject
    {
        public string SourceReceipt;public ContactMotion[] Motions;
        public ContactMotion Find(string id)=>Motions?.SingleOrDefault(m=>m.Id==id);
        public static Vector3 NativePoint(Vector3 point)=>new Vector3(-point.x,point.z,-point.y);
        public static Quaternion NativeRotation(Quaternion q)=>new Quaternion(q.x,-q.z,q.y,q.w);
    }
    [Serializable] public sealed class ContactBinding
    {
        public string Id,MotionId;public Transform Stance,Prop,CameraPose;
        public bool ImportedNativeGeometry;
    }
}
