using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace JeonLib.SceneSwitcher.Tests
{
    /// <summary>시작 씬 선택과 playModeStartScene 적용을 검증한다.</summary>
    internal class PlayFromStartSceneTests
    {
        private const string TEMP_FOLDER = "Assets/__SceneSwitcherTestTemp";
        private const string MISSING_GUID = "0123456789abcdef0123456789abcdef";

        private SceneAsset m_tempScene;

        [OneTimeSetUp]
        public void createTempScene()
        {
            if (!AssetDatabase.IsValidFolder(TEMP_FOLDER))
                AssetDatabase.CreateFolder("Assets", TEMP_FOLDER.Substring("Assets/".Length));

            // 테스트 러너가 연 미저장 씬이 있어 Additive 생성이 막히므로, 열린 씬을 복사본으로만 저장한다.
            string scenePath = TEMP_FOLDER + "/TempStartScene.unity";
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath, true);

            m_tempScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }

        [OneTimeTearDown]
        public void deleteTempScene()
        {
            AssetDatabase.DeleteAsset(TEMP_FOLDER);
        }

        [SetUp]
        public void setUp()
        {
            SettingsFileBackup.takeAndClear();
        }

        [TearDown]
        public void tearDown()
        {
            SettingsFileBackup.restore();
        }

        private static SceneAsset firstBuildScene()
        {
            EditorBuildSettingsScene first = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            return first != null ? AssetDatabase.LoadAssetAtPath<SceneAsset>(first.path) : null;
        }

        [Test]
        public void 임시_씬이_만들어짐()
        {
            Assert.That(m_tempScene, Is.Not.Null);
        }

        [Test]
        public void 켜면_시작_씬이_적용됨()
        {
            PlayFromStartScene.customStartScene = m_tempScene;
            PlayFromStartScene.enabled = true;

            Assert.That(EditorSceneManager.playModeStartScene, Is.EqualTo(m_tempScene));
        }

        [Test]
        public void 끄면_시작_씬이_해제됨()
        {
            PlayFromStartScene.customStartScene = m_tempScene;
            PlayFromStartScene.enabled = true;

            PlayFromStartScene.enabled = false;

            Assert.That(EditorSceneManager.playModeStartScene, Is.Null);
        }

        [Test]
        public void 지정한_시작_씬이_저장됨()
        {
            PlayFromStartScene.customStartScene = m_tempScene;

            string expectedGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_tempScene));
            Assert.That(SceneSwitcherSettings.instance.startSceneGuid, Is.EqualTo(expectedGuid));
            Assert.That(PlayFromStartScene.startScene, Is.EqualTo(m_tempScene));
        }

        [Test]
        public void 지정하지_않으면_Build_Settings_첫_씬()
        {
            PlayFromStartScene.customStartScene = null;

            Assert.That(PlayFromStartScene.startScene, Is.EqualTo(firstBuildScene()));
            Assert.That(PlayFromStartScene.isCustomStartSceneMissing, Is.False);
        }

        [Test]
        public void 지정한_씬이_삭제되면_누락으로_판정하고_첫_씬_사용()
        {
            SceneSwitcherSettings.instance.startSceneGuid = MISSING_GUID;

            Assert.That(PlayFromStartScene.isCustomStartSceneMissing, Is.True);
            Assert.That(PlayFromStartScene.customStartScene, Is.Null);
            Assert.That(PlayFromStartScene.startScene, Is.EqualTo(firstBuildScene()));
        }
    }
}
