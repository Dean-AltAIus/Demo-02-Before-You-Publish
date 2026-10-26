using System;
using System.Collections;
using System.IO;
using Altaius.Demo02.Authoring;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
namespace Altaius.Demo02.Tests
{
    public sealed class NativeAcceptanceTests
    {
        [UnityTest,Timeout(120000),Explicit("Arrow-key world movement, literal typing and UI focus ownership.")]
        public IEnumerator ArrowMovementAndFocus()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            yield return new EnterPlayMode();yield return null;
            string output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../LocalOnly/Validation/Acceptance/ArrowInput_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")));
            try{yield return Task6RuntimeAcceptance.ArrowInput(output);Assert.That(Task6RuntimeAcceptance.Result,Does.StartWith("PASS"));}finally{Task6RuntimeAcceptance.Cleanup();}
            yield return new ExitPlayMode();
        }
        [UnityTest,Timeout(120000),Explicit("Bounded supported EditorLoop and render-wait attribution.")]
        public IEnumerator EditorLoopAttribution()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            yield return new EnterPlayMode();yield return null;
            string output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../LocalOnly/Validation/Acceptance/LoopAttribution_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")));
            try{yield return Task6RuntimeAcceptance.LoopAttribution(output);Assert.That(Task6RuntimeAcceptance.Result,Does.StartWith("PASS"));}finally{Task6RuntimeAcceptance.Cleanup();}
            yield return new ExitPlayMode();
        }
        [UnityTest,Timeout(600000),Explicit("Bounded matched shadow/SSAO render comparison with restoration.")]
        public IEnumerator MatchedRenderComparison()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            yield return new EnterPlayMode();yield return null;
            string output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../LocalOnly/Validation/Acceptance/RenderAB_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")));
            try{yield return Task6RuntimeAcceptance.RenderComparison(output);Assert.That(Task6RuntimeAcceptance.Result,Does.StartWith("PASS"));}finally{Task6RuntimeAcceptance.Cleanup();}
            yield return new ExitPlayMode();
        }
        [UnityTest,Timeout(300000),Explicit("Final uncaptured performance states, caption/details and reset stability.")]
        public IEnumerator FinalFocusedDelta()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            yield return new EnterPlayMode();yield return null;
            string output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../LocalOnly/Validation/Acceptance/FinalDelta_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")));
            try{yield return Task6RuntimeAcceptance.FinalDelta(output);Assert.That(Task6RuntimeAcceptance.Result,Does.StartWith("PASS"));}finally{Task6RuntimeAcceptance.Cleanup();}
            yield return new ExitPlayMode();
        }

        [UnityTest,Explicit("Focused repeated reset/replay pool and memory stability.")]
        public IEnumerator ResetMemoryStability()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            yield return new EnterPlayMode();yield return null;
            string output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../LocalOnly/Validation/Acceptance/ResetMemory_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")));
            try{yield return Task6RuntimeAcceptance.ResetMemory(output);Assert.That(Task6RuntimeAcceptance.Result,Does.StartWith("PASS"));}finally{Task6RuntimeAcceptance.Cleanup();}
            yield return new ExitPlayMode();
        }
        [UnityTest,Timeout(360000),Explicit("Native final review states, all required layouts, accessibility and reset memory.")]
        public IEnumerator ReviewStatesAndAccessibility()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            yield return new EnterPlayMode();yield return null;
            string output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../LocalOnly/Validation/Acceptance/ReviewAccessibility_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")));
            try{yield return Task6RuntimeAcceptance.ReviewAccessibility(output);Assert.That(Task6RuntimeAcceptance.Result,Does.StartWith("PASS"));}finally{Task6RuntimeAcceptance.Cleanup();}
            yield return new ExitPlayMode();
        }
        [UnityTest,Timeout(1200000),Explicit("Finite real-time spatial presenter and rendered benchmark.")]
        public IEnumerator SpatialPresenter()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            yield return new EnterPlayMode();yield return null;
            string output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../LocalOnly/Validation/Acceptance/SpatialPresenter_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")));
            try{yield return Task6RuntimeAcceptance.Presenter(output,false);Assert.That(Task6RuntimeAcceptance.Result,Does.StartWith("PASS"));}finally{Task6RuntimeAcceptance.Cleanup();}
            yield return new ExitPlayMode();
        }
        [UnityTest,Explicit("Active presenter Play-exit lifecycle regression.")]
        public IEnumerator ActivePresenterPlayExit()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            yield return new EnterPlayMode();yield return null;
            var runner=UnityEngine.Object.FindFirstObjectByType<Altaius.Demo02.Presenter.PresenterRunner>();
            runner.StartSequence(runner.Sequence,false);yield return null;Assert.That(runner.Active,Is.True);
            yield return new ExitPlayMode();LogAssert.NoUnexpectedReceived();
        }
        [UnityTest,Explicit("Finite actual-runtime acceptance, invoked by exact name only.")]
        public IEnumerator LearnerAndAccessibility()
        {
            Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,Is.False);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Demo02/Scenes/BeforeYouPublish_Campus.unity");
            yield return new EnterPlayMode();
            string output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../LocalOnly/Validation/Acceptance/NativeLearner_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")));
            yield return null;
            try{yield return Task6RuntimeAcceptance.Learner(output);Assert.That(Task6RuntimeAcceptance.Result,Does.StartWith("PASS"));}
            finally{Task6RuntimeAcceptance.Cleanup();}
            yield return new ExitPlayMode();
        }
    }
}
