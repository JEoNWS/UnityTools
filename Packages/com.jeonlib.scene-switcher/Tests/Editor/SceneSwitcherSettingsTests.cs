using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JeonLib.SceneSwitcher.Tests
{
    /// <summary>설정의 저장, 다시 읽기, 손상 파일 처리를 검증한다.</summary>
    internal class SceneSwitcherSettingsTests
    {
        private const string GUID_A = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string GUID_B = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        private static SceneSwitcherSettings settings => SceneSwitcherSettings.instance;

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

        [Test]
        public void 파일이_없으면_기본값()
        {
            Assert.That(settings.sceneFolderGuids, Is.Empty);
            Assert.That(settings.playFromStartScene, Is.False);
            Assert.That(settings.startSceneGuid, Is.Empty);
        }

        [Test]
        public void 폴더_추가는_다시_읽어도_유지()
        {
            Assert.That(settings.addSceneFolder(GUID_A), Is.True);

            SceneSwitcherSettings.reload();

            Assert.That(settings.sceneFolderGuids, Is.EqualTo(new[] { GUID_A }));
        }

        [Test]
        public void 중복이나_빈_GUID는_추가되지_않음()
        {
            settings.addSceneFolder(GUID_A);

            Assert.That(settings.addSceneFolder(GUID_A), Is.False);
            Assert.That(settings.addSceneFolder(""), Is.False);
            Assert.That(settings.addSceneFolder(null), Is.False);
            Assert.That(settings.sceneFolderGuids.Count, Is.EqualTo(1));
        }

        [Test]
        public void 폴더_삭제는_다시_읽어도_유지()
        {
            settings.addSceneFolder(GUID_A);
            settings.addSceneFolder(GUID_B);

            settings.removeSceneFolder(GUID_A);
            SceneSwitcherSettings.reload();

            Assert.That(settings.sceneFolderGuids, Is.EqualTo(new[] { GUID_B }));
        }

        [Test]
        public void 없는_폴더를_지우면_파일을_쓰지_않음()
        {
            settings.removeSceneFolder(GUID_A);

            Assert.That(File.Exists(SceneSwitcherSettings.FILE_PATH), Is.False);
        }

        [Test]
        public void 토글과_시작_씬은_다시_읽어도_유지()
        {
            settings.playFromStartScene = true;
            settings.startSceneGuid = GUID_A;

            SceneSwitcherSettings.reload();

            Assert.That(settings.playFromStartScene, Is.True);
            Assert.That(settings.startSceneGuid, Is.EqualTo(GUID_A));
        }

        [Test]
        public void 시작_씬에_null을_넣으면_빈_문자열()
        {
            settings.startSceneGuid = GUID_A;
            settings.startSceneGuid = null;

            Assert.That(settings.startSceneGuid, Is.Empty);
        }

        [Test]
        public void 손상된_파일은_경고_후_기본값()
        {
            File.WriteAllText(SceneSwitcherSettings.FILE_PATH, "{ not json");
            LogAssert.Expect(LogType.Warning, new Regex("설정 파일을 읽지 못해"));

            SceneSwitcherSettings.reload();

            Assert.That(settings.sceneFolderGuids, Is.Empty);
            Assert.That(settings.playFromStartScene, Is.False);
        }

        [Test]
        public void 직접_저장한_뒤에는_다시_읽지_않음()
        {
            settings.addSceneFolder(GUID_A);

            Assert.That(SceneSwitcherSettings.reloadIfChanged(), Is.False);
        }

        [Test]
        public void 외부에서_파일을_고치면_다시_읽음()
        {
            settings.addSceneFolder(GUID_A);

            File.WriteAllText(SceneSwitcherSettings.FILE_PATH,
                $"{{\"m_sceneFolderGuids\":[\"{GUID_B}\"],\"m_playFromStartScene\":true,\"m_startSceneGuid\":\"\"}}");
            // 같은 초 안에 다시 쓰면 수정 시각이 같을 수 있어 명시적으로 미룬다.
            File.SetLastWriteTimeUtc(SceneSwitcherSettings.FILE_PATH, DateTime.UtcNow.AddSeconds(10));

            Assert.That(SceneSwitcherSettings.reloadIfChanged(), Is.True);
            Assert.That(settings.sceneFolderGuids, Is.EqualTo(new[] { GUID_B }));
            Assert.That(settings.playFromStartScene, Is.True);
        }
    }
}
