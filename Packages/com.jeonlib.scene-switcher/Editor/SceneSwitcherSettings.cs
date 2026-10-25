using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JeonLib.SceneSwitcher
{
    /// <summary>Scene Switcher 설정. UserSettings 폴더에 JSON으로 저장해 사용자별로 유지한다.</summary>
    [Serializable]
    internal class SceneSwitcherSettings
    {
        internal const string FILE_PATH = "UserSettings/SceneSwitcher.json";

        [SerializeField] private List<string> m_sceneFolderGuids = new List<string>();
        [SerializeField] private bool m_playFromStartScene;
        [SerializeField] private string m_startSceneGuid = "";

        private static SceneSwitcherSettings s_instance;
        private static DateTime s_loadedWriteTime;

        public static SceneSwitcherSettings instance => s_instance ??= load();

        /// <summary>파일에서 설정을 다시 읽는다.</summary>
        public static void reload()
        {
            s_instance = load();
        }

        /// <summary>마지막으로 읽거나 쓴 뒤 파일이 바뀌었으면 다시 읽는다.</summary>
        public static bool reloadIfChanged()
        {
            if (s_instance == null || getWriteTime() == s_loadedWriteTime)
                return false;

            reload();
            return true;
        }

        public IReadOnlyList<string> sceneFolderGuids => m_sceneFolderGuids;

        public bool playFromStartScene
        {
            get => m_playFromStartScene;
            set
            {
                if (m_playFromStartScene == value)
                    return;

                m_playFromStartScene = value;
                save();
            }
        }

        /// <summary>비어 있으면 Build Settings 첫 활성 씬을 쓴다.</summary>
        public string startSceneGuid
        {
            get => m_startSceneGuid;
            set
            {
                value ??= "";
                if (m_startSceneGuid == value)
                    return;

                m_startSceneGuid = value;
                save();
            }
        }

        public bool addSceneFolder(string guid)
        {
            if (string.IsNullOrEmpty(guid) || m_sceneFolderGuids.Contains(guid))
                return false;

            m_sceneFolderGuids.Add(guid);
            save();
            return true;
        }

        public void removeSceneFolder(string guid)
        {
            if (m_sceneFolderGuids.Remove(guid))
                save();
        }

        private static DateTime getWriteTime()
        {
            return File.Exists(FILE_PATH) ? File.GetLastWriteTimeUtc(FILE_PATH) : DateTime.MinValue;
        }

        private static SceneSwitcherSettings load()
        {
            s_loadedWriteTime = getWriteTime();
            if (!File.Exists(FILE_PATH))
                return new SceneSwitcherSettings();

            try
            {
                return JsonUtility.FromJson<SceneSwitcherSettings>(File.ReadAllText(FILE_PATH)) ?? new SceneSwitcherSettings();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SceneSwitcher] 설정 파일을 읽지 못해 기본값을 사용합니다: {e.Message}");
                return new SceneSwitcherSettings();
            }
        }

        private void save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FILE_PATH));
            File.WriteAllText(FILE_PATH, JsonUtility.ToJson(this, true));
            s_loadedWriteTime = getWriteTime();
        }
    }
}
