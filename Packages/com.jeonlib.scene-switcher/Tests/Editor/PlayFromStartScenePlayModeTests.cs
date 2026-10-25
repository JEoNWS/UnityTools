using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JeonLib.SceneSwitcher.Tests
{
    /// <summary>실제로 플레이 모드에 들어가 시작 씬 적용과 복귀를 검증한다.</summary>
    /// <remarks>
    /// 플레이 진입 때 도메인 리로드가 일어나 지역 변수와 필드가 초기화되고 SetUp이 다시 불린다.
    /// 그래서 경로는 상수로 두고, 플레이 중에 본 값은 SessionState에 담았다가 종료 후 검증한다.
    /// </remarks>
    internal class PlayFromStartScenePlayModeTests
    {
        private const string TEMP_FOLDER = "Assets/__SceneSwitcherPlayTestTemp";
        private const string START_SCENE_PATH = TEMP_FOLDER + "/PlayStartScene.unity";
        private const string PLAY_SCENE_KEY = "SceneSwitcher.Tests.PlaySceneInPlayMode";
        private const string EDIT_SCENE_KEY = "SceneSwitcher.Tests.EditSceneBeforePlay";
        private const string STATE_LOG_KEY = "SceneSwitcher.Tests.StateLog";
        private const string SET_UP_DONE_KEY = "SceneSwitcher.Tests.SetUpDone";
        private const int WAIT_FRAMES = 10;

        private static SceneAsset startScene => AssetDatabase.LoadAssetAtPath<SceneAsset>(START_SCENE_PATH);

        [SetUp]
        public void setUp()
        {
            // 플레이 진입 리로드 뒤 SetUp이 다시 불리므로, 이미 준비한 테스트면 설정을 지우지 않는다.
            if (SessionState.GetBool(SET_UP_DONE_KEY, false))
                return;
            SessionState.SetBool(SET_UP_DONE_KEY, true);

            SessionState.EraseString(STATE_LOG_KEY);
            SettingsFileBackup.takeAndClear();

            if (startScene != null)
                return;

            if (!AssetDatabase.IsValidFolder(TEMP_FOLDER))
                AssetDatabase.CreateFolder("Assets", TEMP_FOLDER.Substring("Assets/".Length));

            // 테스트 러너가 연 미저장 씬을 복사본으로만 저장해 열린 씬 상태는 그대로 둔다.
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), START_SCENE_PATH, true);
        }

        [TearDown]
        public void tearDown()
        {
            SettingsFileBackup.restore();
            SessionState.EraseString(PLAY_SCENE_KEY);
            SessionState.EraseString(EDIT_SCENE_KEY);
            SessionState.EraseString(STATE_LOG_KEY);
            SessionState.EraseBool(SET_UP_DONE_KEY);
            AssetDatabase.DeleteAsset(TEMP_FOLDER);
        }

        private static void recordPlayScene() => SessionState.SetString(PLAY_SCENE_KEY, SceneManager.GetActiveScene().path);
        private static void recordEditScene() => SessionState.SetString(EDIT_SCENE_KEY, SceneManager.GetActiveScene().path);
        private static string recordedPlayScene => SessionState.GetString(PLAY_SCENE_KEY, null);
        private static string recordedEditScene => SessionState.GetString(EDIT_SCENE_KEY, null);

        // 플레이 진입마다 전체 리로드가 일어나 느리므로, 시작 씬 플레이와 복귀를 한 번에 확인한다.
        [UnityTest]
        public IEnumerator 켜면_시작_씬에서_플레이하고_끝내면_원래_씬으로_돌아옴()
        {
            PlayFromStartScene.customStartScene = startScene;
            PlayFromStartScene.enabled = true;
            recordEditScene();

            yield return new EnterPlayMode();
            recordPlayScene();
            yield return new ExitPlayMode();

            Assert.That(recordedPlayScene, Is.EqualTo(START_SCENE_PATH));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(recordedEditScene));
            Assert.That(SceneManager.GetActiveScene().path, Is.Not.EqualTo(START_SCENE_PATH));
        }

        [UnityTest]
        public IEnumerator 바로_플레이는_한_번만_현재_씬에서_시작하고_다시_시작_씬으로_돌아감()
        {
            PlayFromStartScene.customStartScene = startScene;
            PlayFromStartScene.enabled = true;
            recordEditScene();

            PlayFromStartScene.skipStartSceneOnce();
            recordState("예약 직후");
            yield return new EnterPlayMode();
            recordPlayScene();
            recordState("플레이 중");
            yield return new ExitPlayMode();
            recordState("종료 직후");
            // 리로드 후 delayCall로 다시 적용되므로 몇 프레임까지 기다린다.
            for (int frame = 0; frame < WAIT_FRAMES && EditorSceneManager.playModeStartScene != startScene; ++frame)
                yield return null;
            recordState($"{WAIT_FRAMES}프레임 대기 후");

            Assert.That(recordedPlayScene, Is.EqualTo(recordedEditScene));
            Assert.That(EditorSceneManager.playModeStartScene, Is.EqualTo(startScene), SessionState.GetString(STATE_LOG_KEY, ""));
        }

        // 도메인 리로드를 넘어 상태 변화를 남겨 실패 메시지로 보여 준다.
        private static void recordState(string step)
        {
            string line = $"[{step}] playing={EditorApplication.isPlaying}, enabled={PlayFromStartScene.enabled}, "
                          + $"skipOnce={PlayFromStartScene.isSkippingOnce}, startScene={PlayFromStartScene.startScene?.name ?? "null"}, "
                          + $"playModeStartScene={EditorSceneManager.playModeStartScene?.name ?? "null"}";
            SessionState.SetString(STATE_LOG_KEY, SessionState.GetString(STATE_LOG_KEY, "") + "\n" + line);
        }
    }
}
