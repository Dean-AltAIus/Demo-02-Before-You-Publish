using System.Collections;
using System.IO;
using System.Linq;
using Altaius.Demo02.Authoring;
using Altaius.Demo02.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Altaius.Demo02.Tests
{
    public sealed class TitleMenuFocusedTests
    {
        const string ScenePath = "Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity";
        const string PrefabPath = "Assets/Demo02/Prefabs/UI/Presentation/title.prefab";

        [Test]
        public void SavedTitleHasOneEditableShotAndCompleteNativeControls()
        {
            Assert.That(EditorApplication.isPlaying, Is.False);
            EditorSceneManager.OpenScene(ScenePath);
            Assert.That(EditorSceneManager.GetActiveScene().path, Is.EqualTo(ScenePath));
            var view = Object.FindFirstObjectByType<PresentationView>();
            var stage = view.TitleStage;
            var title = view.Surfaces.Single(x => x.SurfaceId == "title");
            Assert.That(stage, Is.Not.Null);
            Assert.That(stage.Shot.gameObject.scene.path, Is.EqualTo(ScenePath));
            Assert.That(stage.Rig, Is.SameAs(view.Gameplay.CameraRig));
            Assert.That(stage.HiddenLearnerRenderers, Is.EquivalentTo(stage.Rig.LocalBodyRenderers));
            Assert.That(Object.FindObjectsByType<TitleScenePresentation>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(t => t.name.StartsWith("Title workroom shot")), Is.EqualTo(1));
            Assert.That(title.TitleLayout, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(title.gameObject), Is.EqualTo(PrefabPath));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<PresentationSurface>();
            Assert.That(prefab.TitleLayout.Plaque && prefab.TitleLayout.Crest && prefab.TitleLayout.ViewBorder && prefab.TitleLayout.CompanionPortrait, Is.True);
            Assert.That(prefab.TitleLayout.PresenterRow.ControlId, Is.EqualTo("Title.Presenter"));
            Assert.That(prefab.TitleLayout.CompanionHelp, Is.Not.Null);
            Assert.That(prefab.Scroll.gameObject.activeSelf, Is.False);
            Assert.That(view.Workbench.English.Get("title.disclosure"), Does.Contain("No real student data"));
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TitleRoutesRestoreCameraAndLeaveNoLearnerEvidence()
        {
            EditorSceneManager.OpenScene(ScenePath);
            yield return new EnterPlayMode();
            yield return null;
            var view = Object.FindFirstObjectByType<PresentationView>();
            var title = view.Surfaces.Single(x => x.SurfaceId == "title");
            var settings = view.Surfaces.Single(x => x.SurfaceId == "settings");
            var stage = view.TitleStage;
            var originalScale = view.Applied.TextScale;
            var evidence = view.Host.Session.Snapshot.Evidence.Count;
            Assert.That(view.SurfaceId, Is.EqualTo("title"));
            Assert.That(stage.Owned, Is.True);
            Assert.That(stage.Rig.enabled, Is.False);
            Assert.That(view.TalkLauncher.gameObject.activeInHierarchy, Is.False);
            Assert.That(view.Workbench.LeenaDock.gameObject.activeInHierarchy, Is.False);
            Assert.That(title.Focus.FocusRoot, Is.SameAs(title.Frame));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(title.Primary.gameObject));

            title.Secondary.onClick.Invoke();
            Assert.That(view.SurfaceId, Is.EqualTo("settings"));
            Assert.That(stage.Owned, Is.False);
            view.Staged.TextScale = originalScale == 2f ? 1f : 2f;
            settings.Secondary.onClick.Invoke();
            Assert.That(view.SurfaceId, Is.EqualTo("title"));
            Assert.That(view.Applied.TextScale, Is.EqualTo(originalScale));
            Assert.That(stage.Owned, Is.True);

            title.TitleLayout.PresenterRow.Action.onClick.Invoke();
            Assert.That(view.SurfaceId, Is.EqualTo("brief"));
            Assert.That(stage.Owned, Is.False);
            var brief = view.Surfaces.Single(x => x.SurfaceId == "brief");
            Assert.That(brief.Heading.text, Does.Contain("walkthrough"));
            brief.Tertiary.onClick.Invoke();
            Assert.That(view.SurfaceId, Is.EqualTo("title"));
            Assert.That(stage.Owned, Is.True);

            title.Tertiary.onClick.Invoke();
            Assert.That(view.SurfaceId, Is.EqualTo("confirmation"));
            Assert.That(stage.Owned, Is.False);
            view.Back();
            Assert.That(view.SurfaceId, Is.EqualTo("title"));
            Assert.That(stage.Owned, Is.True);

            title.TitleLayout.CompanionHelp.onClick.Invoke();
            Assert.That(view.SurfaceId, Is.EqualTo("coach"));
            view.Back();
            Assert.That(view.SurfaceId, Is.EqualTo("title"));

            title.Primary.onClick.Invoke();
            Assert.That(view.SurfaceId, Is.EqualTo("brief"));
            Assert.That(stage.Owned, Is.False);
            brief.Secondary.onClick.Invoke();
            Assert.That(view.SurfaceId, Is.EqualTo("title"));
            Assert.That(stage.Owned, Is.True);
            Assert.That(view.Host.Session.Snapshot.Evidence.Count, Is.EqualTo(evidence));
            yield return new ExitPlayMode();
            Assert.That(EditorSceneManager.GetActiveScene().isDirty, Is.False);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TitleFirstFramesAndNativeKeyboardOwnership()
        {
            EditorSceneManager.OpenScene(ScenePath);
            CampusDiagnostics.GameSize(1280, 720);
            yield return new EnterPlayMode();
            yield return null;
            var view = Object.FindFirstObjectByType<PresentationView>();
            var title = view.Surfaces.Single(x => x.SurfaceId == "title");
            var firstTitleFrame = Time.frameCount;
            ScreenCapture.CaptureScreenshot("LocalOnly/Validation/Acceptance/TitleMenu_v002/title-first-test-visible.png");
            for (var i = 0; i < 12; i++) yield return null;
            var settledTitleFrame = Time.frameCount;
            ScreenCapture.CaptureScreenshot("LocalOnly/Validation/Acceptance/TitleMenu_v002/title-settled-test.png");
            Assert.That(view.SurfaceId, Is.EqualTo("title"));
            Assert.That(view.TitleStage.Owned, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(title.Primary.gameObject));

            var probe = view.Gameplay.gameObject.AddComponent<CampusProbeInput>();
            probe.Begin();
            try
            {
                var motorOrigin = view.Gameplay.Motor.transform.position;
                SetKeys(probe, "UpArrow");
                for (var i = 0; i < 12; i++) yield return null;
                SetKeys(probe);
                yield return null;
                Assert.That(Vector3.Distance(view.Gameplay.Motor.transform.position, motorOrigin), Is.LessThan(.01f), "Title must block world movement.");

                SetKeys(probe, "Tab");
                yield return null;
                SetKeys(probe);
                yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.SameAs(title.Primary.gameObject), "Tab must advance native title focus.");
                SetKeys(probe, "LeftShift", "Tab");
                yield return null;
                SetKeys(probe);
                yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(title.Primary.gameObject), "Shift+Tab must return to the first title action.");

                EventSystem.current.SetSelectedGameObject(title.Secondary.gameObject);
                SetKeys(probe, "Enter");
                yield return null;
                SetKeys(probe);
                yield return null;
                Assert.That(view.SurfaceId, Is.EqualTo("settings"), "Enter must activate Settings.");
                var settings = view.Surfaces.Single(x => x.SurfaceId == "settings");
                EventSystem.current.SetSelectedGameObject(settings.Secondary.gameObject);
                SetKeys(probe, "Space");
                yield return null;
                SetKeys(probe);
                yield return null;
                Assert.That(view.SurfaceId, Is.EqualTo("title"), "Space must activate Cancel and return to title.");

                title.Primary.onClick.Invoke();
                var brief = view.Surfaces.Single(x => x.SurfaceId == "brief");
                Assert.That(view.SurfaceId, Is.EqualTo("brief"));
                brief.Primary.onClick.Invoke();
                for (var i = 0; i < 3; i++) yield return null;
                Assert.That(view.SurfaceId, Is.EqualTo("onramp"));
                view.Surfaces.Single(x => x.SurfaceId == "onramp").Primary.onClick.Invoke();
                var firstWorldFrame = Time.frameCount;
                ScreenCapture.CaptureScreenshot("LocalOnly/Validation/Acceptance/TitleMenu_v002/world-first-test-visible.png");
                for (var i = 0; i < 12; i++) yield return null;
                var settledWorldFrame = Time.frameCount;
                ScreenCapture.CaptureScreenshot("LocalOnly/Validation/Acceptance/TitleMenu_v002/world-settled-test.png");
                Assert.That(view.SurfaceId, Is.EqualTo("world"));
                Assert.That(view.TitleStage.Owned, Is.False);
                Assert.That(view.Gameplay.CameraRig.enabled, Is.True);
                File.WriteAllText("LocalOnly/Validation/Acceptance/TitleMenu_v002/first-frame-frames.txt",
                    "Title first test-visible frame " + firstTitleFrame + ", settled " + settledTitleFrame + "\nWorld first test-visible frame " + firstWorldFrame + ", settled " + settledWorldFrame + "\n");
            }
            finally { Object.DestroyImmediate(probe); }
            yield return new ExitPlayMode();
        }

        static void SetKeys(CampusProbeInput probe, params string[] names)
        {
            var field = typeof(CampusProbeInput).GetField("Keys");
            var keyType = field.FieldType.GetElementType();
            var values = System.Array.CreateInstance(keyType, names.Length);
            for (var i = 0; i < names.Length; i++) values.SetValue(System.Enum.Parse(keyType, names[i]), i);
            field.SetValue(probe, values);
        }
    }
}
