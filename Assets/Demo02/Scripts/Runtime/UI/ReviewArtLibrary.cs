using System;
using UnityEngine;
namespace Altaius.Demo02.UI
{
    [CreateAssetMenu(menuName="Demo02/Review art library")]
    public sealed class ReviewArtLibrary : ScriptableObject
    {
        public Sprite GenericFolio;
        public ReviewArtEntry[] States;
        public Sprite For(ReviewArtState state)=>States==null?null:Array.Find(States,e=>e!=null&&e.State==state)?.Sprite;
    }
    [Serializable]public sealed class ReviewArtEntry
    {public ReviewArtState State;public Sprite Sprite;}
}
