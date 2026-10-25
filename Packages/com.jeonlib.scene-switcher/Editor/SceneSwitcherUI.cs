namespace JeonLib.SceneSwitcher
{
    /// <summary>창 요소의 이름과 USS 클래스. 테스트와 구현이 같은 이름을 쓴다.</summary>
    internal static class SceneSwitcherUI
    {
        public const string SETTINGS_TOGGLE = "settings-toggle";
        public const string REFRESH_BUTTON = "refresh-button";
        public const string PLAY_FROM_START_TOGGLE = "play-from-start-toggle";
        public const string SEARCH_FIELD = "search-field";

        public const string SETTINGS_PANEL = "settings-panel";
        public const string START_SCENE_FIELD = "start-scene-field";
        public const string MISSING_START_WARNING = "missing-start-warning";
        public const string START_SCENE_HINT = "start-scene-hint";
        public const string FOLDER_LIST = "folder-list";
        public const string ADD_FOLDER_FIELD = "add-folder-field";

        public const string SCENE_LIST = "scene-list";
        public const string EMPTY_HELP = "empty-help";
        public const string FOOTER = "footer";

        // 클래스 이름은 블록--상태 형식(BEM)으로 짓는다.
        public const string ROW_CLASS = "scene-row";
        public const string ACTIVE_CLASS = "scene-row--active";
        public const string LOADED_CLASS = "scene-row--loaded";
        public const string TOGGLE_ON_CLASS = "toolbar-toggle--on";
        public const string HIDDEN_CLASS = "hidden";

        // 우클릭 메뉴 항목. 문구를 바꿔도 테스트가 따라가도록 한곳에 둔다.
        public const string MENU_OPEN = "전환";
        public const string MENU_OPEN_ADDITIVE = "추가로 열기 (Additive)";
        public const string MENU_CLOSE = "닫기";
        public const string MENU_PLAY_DIRECT = "이 씬에서 바로 플레이";
        public const string MENU_SET_START_SCENE = "시작 씬으로 지정";
        public const string MENU_CLEAR_START_SCENE = "시작 씬 지정 해제";
        public const string MENU_PING = "Project에서 보기";

        public static string groupName(string folderPath) => "group:" + folderPath;
        public static string rowName(string scenePath) => "scene:" + scenePath;
    }
}
