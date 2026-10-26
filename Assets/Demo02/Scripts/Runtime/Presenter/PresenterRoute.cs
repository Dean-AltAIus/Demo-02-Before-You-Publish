using E=Altaius.Demo02.Domain.EnglishCopy;
using System;
using System.Collections.Generic;
using System.Linq;
using Altaius.Demo02.Gameplay;
using UnityEngine;
namespace Altaius.Demo02.Presenter
{
    /// <summary>Visibility graph over saved, editable route anchors. CharacterController collisions remain authoritative.</summary>
    public sealed class PresenterRoute
    {
        readonly CampusGameplayFacade game;readonly Vector3[] route;int next;
        public PresenterRoute(CampusGameplayFacade game,IEnumerable<Transform> anchors,CampusStation target)
        {
            this.game=game;var points=new[]{game.Motor.transform.position}.Concat(anchors.Where(t=>t).Select(t=>t.position)).Concat(new[]{target.Arrival.position}).ToArray();
            var cost=Enumerable.Repeat(float.PositiveInfinity,points.Length).ToArray();var previous=Enumerable.Repeat(-1,points.Length).ToArray();var used=new bool[points.Length];cost[0]=0;
            for(int n=0;n<points.Length;n++)
            {
                int at=-1;for(int j=0;j<points.Length;j++)if(!used[j]&&(at<0||cost[j]<cost[at]))at=j;
                if(at<0||float.IsPositiveInfinity(cost[at]))break;used[at]=true;
                for(int j=0;j<points.Length;j++)if(!used[j]&&Clear(points[at],points[j]))
                {float candidate=cost[at]+Vector3.Distance(points[at],points[j]);if(candidate<cost[j]){cost[j]=candidate;previous[j]=at;}}
            }
            int end=points.Length-1;if(float.IsPositiveInfinity(cost[end]))throw new InvalidOperationException(E.Format("presenterroute.initialization.initialization.message.no.clear.collision.route.connects.the","No clear collision route connects the saved presenter anchors to {target_stationid}. Inspect the authored route.","target_stationid",target.StationId));
            var path=new List<Vector3>();for(int at=end;at>0;at=previous[at])path.Add(points[at]);path.Reverse();route=path.ToArray();
        }
        bool Clear(Vector3 from,Vector3 to)
        {
            var delta=to-from;delta.y=0;if(delta.magnitude<.08f)return true;
            return !Physics.CapsuleCastAll(from+Vector3.up*.45f,from+Vector3.up*1.5f,.29f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore)
                .Any(h=>!h.collider.transform.IsChildOf(game.Motor.transform));
        }
        public bool Tick(float deltaTime)
        {
            var position=game.Motor.transform.position;
            while(next<route.Length){var distance=route[next]-position;distance.y=0;if(distance.magnitude>.10f)break;next++;}
            if(next==route.Length){game.DriveMovement(Vector2.zero);return true;}
            var desired=route[next]-position;desired.y=0;float magnitude=Mathf.Clamp01(desired.magnitude/Mathf.Max(.02f,game.Motor.MoveSpeed*deltaTime));desired.Normalize();
            var forward=game.Motor.ViewCamera.transform.forward;forward.y=0;forward.Normalize();var right=Vector3.Cross(Vector3.up,forward);
            game.DriveMovement(new Vector2(Vector3.Dot(desired,right),Vector3.Dot(desired,forward))*magnitude);return false;
        }
    }
}
