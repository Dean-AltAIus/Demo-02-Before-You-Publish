using Altaius.Demo02.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Altaius.Demo02.Tests
{
    public sealed class CameraOcclusionTests
    {
        [TestCase(.48f)]
        [TestCase(3f)]
        public void FollowCameraStaysOnPlayerSideOfNearbySolidWall(float wallDistance)
        {
            var target = new GameObject("Camera collision test target");
            var cameraObject = new GameObject("Camera collision test rig");
            var wall = new GameObject("Camera collision test wall");
            try
            {
                target.transform.position = new Vector3(5000, 20, 5000);
                wall.transform.position = target.transform.position + new Vector3(0, 1, wallDistance);
                var collider = wall.AddComponent<BoxCollider>();
                collider.size = new Vector3(8, 4, .2f);
                var rig = cameraObject.AddComponent<CampusCamera>();
                rig.FollowTarget = target.transform;
                rig.Height = 1;
                rig.Pitch = 0;
                rig.Yaw = 180;
                rig.Distance = 4.5f;
                Physics.SyncTransforms();

                rig.SnapFollow();

                // Test actual clearance, including a wall closer than the previous
                // minimum follow distance. A hit must never push the camera through it.
                Assert.That(cameraObject.transform.position.z + .18f,
                    Is.LessThan(wall.transform.position.z - .1f));
                Assert.That(cameraObject.transform.position.z, Is.GreaterThanOrEqualTo(target.transform.position.z));
            }
            finally
            {
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
            }
        }
    }
}
