using System.IO;

namespace JeonLib.SceneSwitcher.Tests
{
    /// <summary>테스트가 실제 사용자 설정 파일을 덮어쓰지 않도록 백업하고 복구한다.</summary>
    /// <remarks>플레이 모드 진입 시 도메인 리로드로 객체 상태가 사라지므로 백업은 파일로 남긴다.</remarks>
    internal static class SettingsFileBackup
    {
        private const string BACKUP_PATH = "Temp/SceneSwitcherTests.settings.backup";
        private const string NO_FILE_MARKER = "<no settings file>";

        /// <summary>현재 파일을 백업하고 설정 파일을 지워 빈 상태에서 시작한다.</summary>
        public static void takeAndClear()
        {
            // 이전 실행이 중간에 끊겨 백업이 남아 있으면 그것이 원본이므로 덮어쓰지 않는다.
            if (!File.Exists(BACKUP_PATH))
            {
                string path = SceneSwitcherSettings.FILE_PATH;
                File.WriteAllText(BACKUP_PATH, File.Exists(path) ? File.ReadAllText(path) : NO_FILE_MARKER);
            }

            deleteSettingsFile();
            SceneSwitcherSettings.reload();
        }

        public static void restore()
        {
            if (!File.Exists(BACKUP_PATH))
                return;

            string content = File.ReadAllText(BACKUP_PATH);
            if (content == NO_FILE_MARKER)
                deleteSettingsFile();
            else
                File.WriteAllText(SceneSwitcherSettings.FILE_PATH, content);

            File.Delete(BACKUP_PATH);
            SceneSwitcherSettings.reload();
            PlayFromStartScene.reapply();
        }

        private static void deleteSettingsFile()
        {
            if (File.Exists(SceneSwitcherSettings.FILE_PATH))
                File.Delete(SceneSwitcherSettings.FILE_PATH);
        }
    }
}
