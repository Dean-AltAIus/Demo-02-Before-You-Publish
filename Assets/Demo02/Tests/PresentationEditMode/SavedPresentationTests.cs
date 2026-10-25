using System.Linq;
using Altaius.Demo02.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Altaius.Demo02.Tests
{
    public sealed class SavedPresentationTests
    {
        [Test]public void SavedNativeGraphicsHaveCanvasRenderers()
        {
            var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Demo02/Prefabs/UI/Presentation"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.That(paths.Length,Is.GreaterThanOrEqualTo(17));
            var missing=paths.SelectMany(path=>AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Graphic>(true).Where(g=>!g.GetComponent<CanvasRenderer>()).Select(g=>path+" / "+g.name)).ToArray();
            Assert.That(missing,Is.Empty,"Every authored vector icon and frame must render in the actual saved prefab.");
        }
        [Test]public void SavedComposersUseNativeDraftProtectionAndDisconnectedConfig()
        {
            foreach(var id in new[]{"conversation","coach"})
            {var surface=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo02/Prefabs/UI/Presentation/"+id+".prefab").GetComponent<PresentationSurface>();Assert.That(surface.ComposerDraft,Is.Not.Null);Assert.That(surface.ComposerDraft.Input,Is.SameAs(surface.Composer));Assert.That(surface.Composer.textComponent.supportRichText,Is.False);}
            var config=AssetDatabase.LoadAssetAtPath<DisconnectedIntegrationSettings>("Assets/Demo02/Settings/Presentation/DisabledIntegration.asset");Assert.That(config.ExternalTransportEnabled,Is.False);Assert.That(config.SieWritesEnabled,Is.False);Assert.That(config.BootstrapEndpoint,Is.Empty);Assert.That(config.SimulationId,Is.Empty);
        }
    }
}
