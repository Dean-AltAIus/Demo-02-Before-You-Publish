using System.Collections;
using System.IO;
using System.Linq;
using Altaius.Demo02.Authoring;
using Altaius.Demo02.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Altaius.Demo02.Tests
{
    public sealed class Task6MatrixFocusedTests
    {
        const string ScenePath="Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity";
        const string Evidence="LocalOnly/Validation/Acceptance/Task6MatrixFix_v001";

        static void Capture(string name)
        {
            var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"..",Evidence));
            Directory.CreateDirectory(directory);
            Canvas.ForceUpdateCanvases();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png"));
        }

        [UnityTest,Timeout(240000)]
        public IEnumerator LoadingShowsLocalCopyCancelsWithoutAttemptAndRecoversFromUnavailable()
        {
            EditorSceneManager.OpenScene(ScenePath);
            yield return new EnterPlayMode();yield return null;
            var view=Object.FindFirstObjectByType<PresentationView>();
            CampusDiagnostics.GameSize(1280,720);
            var title=view.Surfaces.Single(x=>x.SurfaceId=="title");
            var brief=view.Surfaces.Single(x=>x.SurfaceId=="brief");
            var loading=view.Surfaces.Single(x=>x.SurfaceId=="loading");
            var originalTable=view.Workbench.English;
            var originalAttempt=view.Host.Session.Snapshot.AttemptId;
            var originalEvidence=view.Host.Session.Snapshot.Evidence.Count;
            var originalScale=view.Applied.TextScale;
            Assert.That(originalTable,Is.Not.Null);
            title.Primary.onClick.Invoke();
            Assert.That(view.SurfaceId,Is.EqualTo("brief"));
            var sizes=new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(1280,720)};
            for(int i=0;i<sizes.Length;i++)
            {
                var scale=i==3?2f:1f;
                view.Applied.TextScale=scale;view.Workbench.SetTextScale(scale);
                CampusDiagnostics.GameSize(sizes[i].x,sizes[i].y);
                yield return null;yield return null;
                brief.Primary.onClick.Invoke();
                Assert.That(view.SurfaceId,Is.EqualTo("loading"));
                Assert.That(loading.Heading.text,Is.EqualTo("Preparing practice"));
                Assert.That(loading.Rows.Find("Loading.State").Body.text,Does.Contain("No internet connection is needed"));
                Assert.That(loading.Secondary.GetComponentInChildren<UnityEngine.UI.Text>().text,Is.EqualTo("Cancel"));
                Assert.That(loading.Composer==null||!loading.Composer.gameObject.activeInHierarchy,Is.True);
                yield return null;
                Assert.That(loading.Frame.rect.height,Is.LessThanOrEqualTo(scale==1f?245f:390f));
                Assert.That(loading.Scroll.verticalScrollbar,Is.Null);
                Capture($"loading-normal-final-{sizes[i].x}-{(int)(scale*100)}");
                yield return null;
                loading.Secondary.onClick.Invoke();
                Assert.That(view.SurfaceId,Is.EqualTo("brief"));
                yield return new WaitForSecondsRealtime(.8f);
                Assert.That(view.Host.Session.Snapshot.AttemptId,Is.EqualTo(originalAttempt));
                Assert.That(view.Host.Session.Snapshot.Evidence.Count,Is.EqualTo(originalEvidence));
            }
            view.Applied.TextScale=1;view.Workbench.SetTextScale(1);
            CampusDiagnostics.GameSize(1280,720);yield return null;yield return null;

            brief.Primary.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.35f);
            loading.Secondary.onClick.Invoke();
            brief.Primary.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(view.SurfaceId,Is.EqualTo("loading"));
            Assert.That(view.Host.Session.Snapshot.AttemptId,Is.EqualTo(originalAttempt));
            loading.Secondary.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.8f);
            Assert.That(view.Host.Session.Snapshot.AttemptId,Is.EqualTo(originalAttempt));

            view.Workbench.English=null;
            for(int i=0;i<sizes.Length;i++)
            {
                var scale=i==3?2f:1f;
                view.Applied.TextScale=scale;view.Workbench.SetTextScale(scale);
                CampusDiagnostics.GameSize(sizes[i].x,sizes[i].y);
                yield return null;yield return null;
                brief.Primary.onClick.Invoke();
                Assert.That(view.SurfaceId,Is.EqualTo("loading"));
                yield return new WaitForSecondsRealtime(.8f);
                Assert.That(view.SurfaceId,Is.EqualTo("loading"));
                Assert.That(loading.Heading.text,Is.EqualTo("Practice unavailable"));
                Assert.That(loading.Rows.Find("Loading.Unavailable").Body.text,Does.Contain("No new attempt was started"));
                Assert.That(loading.Secondary.GetComponentInChildren<UnityEngine.UI.Text>().text,Is.EqualTo("Cancel"));
                Assert.That(view.Host.Session.Snapshot.AttemptId,Is.EqualTo(originalAttempt));
                Assert.That(loading.Frame.rect.height,Is.LessThanOrEqualTo(scale==1f?245f:390f));
                Assert.That(loading.Scroll.verticalScrollbar,Is.Null);
                Capture($"loading-unavailable-final-{sizes[i].x}-{(int)(scale*100)}");
                yield return null;
                loading.Secondary.onClick.Invoke();
                Assert.That(view.SurfaceId,Is.EqualTo("brief"));
            }
            view.Applied.TextScale=originalScale;view.Workbench.SetTextScale(originalScale);
            view.Workbench.English=originalTable;
            brief.Primary.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.8f);yield return null;
            Assert.That(view.SurfaceId,Is.EqualTo("onramp"));
            Assert.That(view.Host.Session.Snapshot.AttemptId,Is.Not.EqualTo(originalAttempt));
            Assert.That(view.Host.Session.Snapshot.Evidence.Count,Is.Zero);
            yield return new ExitPlayMode();
            var saved=Object.FindFirstObjectByType<PresentationView>();
            Assert.That(saved.Workbench.English,Is.SameAs(originalTable));
            Assert.That(EditorSceneManager.GetActiveScene().isDirty,Is.False);
        }

    }
}
