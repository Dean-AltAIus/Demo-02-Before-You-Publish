using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Altaius.Demo02.Authoring;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Altaius.Demo02.Gameplay.Tests
{
    public class ReviewPreservationTests
    {
        static string Hash(string path)
        {using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));}

        [Test] public void CampusPreflightsEveryExistingOutputBeforeAnySceneOrAssetMutation()
        {
            var paths=CampusAuthoring.NewSceneOutputs.ToArray();
            var hashes=paths.ToDictionary(p=>p,Hash);var scene=SceneManager.GetActiveScene();
            var roots=scene.GetRootGameObjects().Select(g=>g.GetInstanceID()).ToArray();
            var error=Assert.Throws<InvalidOperationException>(CampusAuthoring.CreateNewScene);
            foreach(var path in paths){Assert.That(error.Message,Does.Contain(path));Assert.That(Hash(path),Is.EqualTo(hashes[path]));}
            Assert.That(SceneManager.GetActiveScene().handle,Is.EqualTo(scene.handle));
            Assert.That(scene.GetRootGameObjects().Select(g=>g.GetInstanceID()),Is.EqualTo(roots));
        }

        [Test] public void MissingOrMovedSceneDoesNotHideExistingPrefabAndMaterialCollisions()
        {
            // Omit the scene from the checked write set to model a moved scene without moving user assets.
            var remaining=CampusAuthoring.NewSceneOutputs.Where(p=>p!=CampusAuthoring.ScenePath).ToArray();
            var error=Assert.Throws<InvalidOperationException>(()=>AuthoringOutputGuard.RequireAbsent(remaining));
            foreach(var path in remaining)Assert.That(error.Message,Does.Contain(path));
            Assert.That(remaining.Any(p=>p.EndsWith(".mat")),Is.True);
        }

        [Test] public void TablePreflightsAllWrappersBeforeAddingOrDisablingSceneObjects()
        {
            var scene=SceneManager.GetActiveScene();var count=scene.GetRootGameObjects().Length;
            var hashes=CampusTableAuthoring.NewOutputs.ToDictionary(p=>p,Hash);
            var error=Assert.Throws<InvalidOperationException>(CampusTableAuthoring.AddOnce);
            foreach(var path in CampusTableAuthoring.NewOutputs)
            {Assert.That(error.Message,Does.Contain(path));Assert.That(Hash(path),Is.EqualTo(hashes[path]));}
            Assert.That(scene.GetRootGameObjects().Length,Is.EqualTo(count));
        }

        [Test] public void OrphanedMetadataAlsoBlocksAuthoring()
        {
            var output=Path.Combine(Path.GetTempPath(),"demo02-preflight-"+Guid.NewGuid().ToString("N")+".prefab");
            try
            {
                File.WriteAllText(output+".meta","preserve this authored identity");
                Assert.Throws<InvalidOperationException>(()=>AuthoringOutputGuard.RequireAbsent(new[]{output}));
                Assert.That(File.ReadAllText(output+".meta"),Is.EqualTo("preserve this authored identity"));
            }
            finally {File.Delete(output+".meta");}
        }
    }
}
