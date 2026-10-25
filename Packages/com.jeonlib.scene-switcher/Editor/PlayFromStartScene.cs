using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace JeonLib.SceneSwitcher
{
    /// <summary>Play를 누르면 지정한 시작 씬부터 플레이하게 한다. 창이 닫혀 있어도 동작한다.</summary>
    [InitializeOnLoad]
    internal static class PlayFromStartScene
    {
        private const string MENU_PATH = "Tools/Scene Switcher/Play From Start Scene";
        private const string SKIP_ONCE_KEY = "SceneSwitcher.PlayFromStartScene.SkipOnce";

        private static SceneSwitcherSettings settings => SceneSwitcherSettings.instance;

        public static bool enabled
        {
            get => settings.playFromStartScene;
            set
            {
                settings.playFromStartScene = value;
                apply();
            }
        }

        /// <summary>직접 지정한 시작 씬. null이면 Build Settings 첫 활성 씬을 쓴다.</summary>
        public static SceneAsset customStartScene
        {
            get => string.IsNullOrEmpty(settings.startSceneGuid)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(settings.startSceneGuid));
            set
            {
                settings.startSceneGuid = value != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) : "";
                apply();
            }
        }

        /// <summary>지정했던 시작 씬이 삭제되어 찾을 수 없는 상태.</summary>
        public static bool isCustomStartSceneMissing => !string.IsNullOrEmpty(settings.startSceneGuid) && customStartScene == null;

        /// <summary>실제로 플레이에 쓰일 시작 씬.</summary>
        public static SceneAsset startScene
        {
            get
            {
                SceneAsset custom = customStartScene;
                return custom != null ? custom : findFirstBuildScene();
            }
        }

        static PlayFromStartScene()
        {
            EditorApplication.playModeStateChanged += onPlayModeStateChanged;
            EditorBuildSettings.sceneListChanged += apply;

            // 정적 생성자 시점에는 AssetDatabase가 준비되지 않았을 수 있어 한 프레임 미룬다.
            EditorApplication.delayCall += apply;
        }

        /// <summary>설정과 상관없이 이번 한 번만 현재 씬에서 바로 플레이한다.</summary>
        public static void playDirect()
        {
            skipStartSceneOnce();
            EditorApplication.EnterPlaymode();
        }

        /// <summary>다음 플레이 한 번만 시작 씬을 쓰지 않는다. 플레이를 마치면 원래 설정으로 돌아간다.</summary>
        public static void skipStartSceneOnce()
        {
            // 플레이 진입 중 도메인 리로드가 일어나도 유지되도록 SessionState에 둔다.
            SessionState.SetBool(SKIP_ONCE_KEY, true);
            apply();
        }

        private static void onPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            SessionState.EraseBool(SKIP_ONCE_KEY);
            apply();
        }

        /// <summary>다음 플레이 한 번을 건너뛰도록 예약된 상태.</summary>
        internal static bool isSkippingOnce => SessionState.GetBool(SKIP_ONCE_KEY, false);

        /// <summary>설정 파일을 다시 읽은 뒤 시작 씬을 다시 적용한다.</summary>
        public static void reapply() => apply();

        /// <summary>설정이 다시 적용될 때 알린다. Tools 메뉴에서 바꿔도 창이 따라가게 한다.</summary>
        public static event Action applied;

        private static void apply()
        {
            bool useStartScene = enabled && !SessionState.GetBool(SKIP_ONCE_KEY, false);
            EditorSceneManager.playModeStartScene = useStartScene ? startScene : null;
            applied?.Invoke();
        }

        private static SceneAsset findFirstBuildScene()
        {
            EditorBuildSettingsScene first = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            return first != null ? AssetDatabase.LoadAssetAtPath<SceneAsset>(first.path) : null;
        }

        [MenuItem(MENU_PATH)]
        private static void toggleMenu() => enabled = !enabled;

        [MenuItem(MENU_PATH, true)]
        private static bool validateMenu()
        {
            Menu.SetChecked(MENU_PATH, enabled);
            return true;
        }
    }
}
