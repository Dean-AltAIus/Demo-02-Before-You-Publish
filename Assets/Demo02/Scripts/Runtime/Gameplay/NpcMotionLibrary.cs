using System;
using System.Linq;
using UnityEngine;
namespace Altaius.Demo02.Gameplay
{
    [Serializable] public sealed class PacketPoseKey
    {public float Time;public Vector3 Position;public Quaternion Rotation=Quaternion.identity,Cover=Quaternion.identity;}
    [Serializable] public sealed class NpcTake
    {
        public string Id;public AnimationClip Clip;public PacketPoseKey[] Packet;
        public PacketPoseKey Sample(float seconds)
        {
            if(Packet==null||Packet.Length==0)return null;
            if(seconds<=Packet[0].Time)return Packet[0];
            for(int i=1;i<Packet.Length;i++)if(seconds<=Packet[i].Time)
            {var a=Packet[i-1];var b=Packet[i];float t=Mathf.InverseLerp(a.Time,b.Time,seconds);return new PacketPoseKey{Time=seconds,Position=Vector3.Lerp(a.Position,b.Position,t),Rotation=Quaternion.Slerp(a.Rotation,b.Rotation,t),Cover=Quaternion.Slerp(a.Cover,b.Cover,t)};}
            return Packet[Packet.Length-1];
        }
    }
    [CreateAssetMenu(menuName="Demo02/Owned NPC motion library")]
    public sealed class NpcMotionLibrary:ScriptableObject
    {public string SourceReceipt;public NpcTake[] Takes;public NpcTake Find(string id)=>Takes?.SingleOrDefault(t=>t.Id==id);public static Quaternion ImportedLocalRotation(Quaternion q)=>new Quaternion(q.x,-q.y,-q.z,q.w);}
}
