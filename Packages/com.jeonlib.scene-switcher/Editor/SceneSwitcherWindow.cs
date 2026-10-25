using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static JeonLib.SceneSwitcher.SceneSwitcherUI;

namespace JeonLib.SceneSwitcher
{
    /// <summary>지정한 폴더 아래의 씬 목록을 보여 주고 클릭하면 에디터에서 씬을 전환하는 창.</summary>
    internal class SceneSwitcherWindow : EditorWindow
    {
        private const string STYLE_SHEET_NAME = "SceneSwitcherWindow.uss";
        private const string VIEW_DATA_PREFIX = "SceneSwitcher.";

        private static SceneSwitcherSettings settings => SceneSwitcherSettings.instance;
        private static IReadOnlyList<string> sceneFolderGuids => settings.sceneFolderGuids;

        // 검색어는 창 직렬화로 유지하고, 접힘·스크롤은 viewDataKey로 Unity가 저장한다.
        [SerializeField] private string m_search = "";

        // CreateGUI에서 한 번 만들어 두고 상태가 바뀔 때만 갱신하는 요소들.
        private ToolbarToggle m_settingsToggle;
        private ToolbarToggle m_playFromStartToggle;
        private ToolbarSearchField m_searchField;
        private VisualElement m_settingsPanel;
        private ObjectField m_startSceneField;
        private VisualElement m_missingStartWarning;
        private Label m_startSceneHint;
        private VisualElement m_folderList;
        private HelpBox m_emptyHelp;
        private ScrollView m_sceneList;

        private readonly Dictionary<string, Foldout> m_foldouts = new Dictionary<string, Foldout>();
        private readonly Dictionary<string, VisualElement> m_rows = new Dictionary<string, VisualElement>();
        private Texture m_sceneIcon;

        // refresh()에서 계산해 두는 데이터.
        private List<SceneFolderGroup> m_groups = new List<SceneFolderGroup>();
        private readonly HashSet<string> m_buildScenes = new HashSet<string>();
        private SceneAsset m_startScene;
        private string m_startPath = "";
        private SceneAsset m_customStartScene;
        private bool m_isCustomStartMissing;

        private readonly HashSet<string> m_loadedScenes = new HashSet<string>();
        private string m_activePath = "";

        // 행을 누른 뒤 뗄 때까지의 상태.
        private string m_pressedScenePath;
        private Vector2 m_pressPosition;

        private bool isBuilt => m_sceneList != null;

        [MenuItem("Tools/Scene Switcher/Open")]
        private static void open()
        {
            GetWindow<SceneSwitcherWindow>("Scene Switcher");
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += refresh;
            EditorBuildSettings.sceneListChanged += refresh;
            PlayFromStartScene.applied += refresh;
            EditorSceneManager.activeSceneChangedInEditMode += onSceneChanged;
            EditorSceneManager.sceneOpened += onSceneOpened;
            EditorSceneManager.sceneClosed += onSceneClosed;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= refresh;
            EditorBuildSettings.sceneListChanged -= refresh;
            PlayFromStartScene.applied -= refresh;
            EditorSceneManager.activeSceneChangedInEditMode -= onSceneChanged;
            EditorSceneManager.sceneOpened -= onSceneOpened;
            EditorSceneManager.sceneClosed -= onSceneClosed;
        }

        // 다른 곳에서 JSON을 고쳤을 수 있으니 창으로 돌아올 때 확인한다.
        private void OnFocus()
        {
            if (SceneSwitcherSettings.reloadIfChanged())
                PlayFromStartScene.reapply();
        }

        // 씬 콜백 시점에는 목록이 아직 정리되지 않았을 수 있어 다음 틱에 갱신한다.
        private void onSceneChanged(Scene previous, Scene next) => scheduleRowStates();
        private void onSceneOpened(Scene scene, OpenSceneMode mode) => scheduleRowStates();
        private void onSceneClosed(Scene scene) => scheduleRowStates();

        private void scheduleRowStates()
        {
            if (isBuilt)
                rootVisualElement.schedule.Execute(updateRowStates);
        }

        // ---------------------------------------------------------------- 요소 만들기

        /// <summary>UI Toolkit 창은 OnGUI 대신 여기서 요소 트리를 한 번 만든다.</summary>
        private void CreateGUI()
        {
            m_sceneIcon = AssetPreview.GetMiniTypeThumbnail(typeof(SceneAsset));
            loadStyleSheet();

            VisualElement root = rootVisualElement;
            root.Add(buildToolbar());
            root.Add(buildSettingsPanel());

            m_emptyHelp = new HelpBox("씬이 있는 폴더를 추가하세요.", HelpBoxMessageType.Info) { name = EMPTY_HELP };
            root.Add(m_emptyHelp);

            m_sceneList = new ScrollView { name = SCENE_LIST, viewDataKey = VIEW_DATA_PREFIX + SCENE_LIST };
            root.Add(m_sceneList);

            root.Add(new Label("클릭: 전환 | Ctrl+클릭: 추가로 열기 | 드래그: Hierarchy에 추가 | 우클릭: 메뉴") { name = FOOTER });

            registerFolderDrop(root);
            refresh();
        }

        /// <summary>스크립트 옆의 USS를 불러온다. 폴더를 옮겨도 따라간다.</summary>
        private void loadStyleSheet()
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string styleSheetPath = Path.Combine(Path.GetDirectoryName(scriptPath), STYLE_SHEET_NAME).Replace('\\', '/');

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);
            else
                Debug.LogWarning($"[SceneSwitcher] 스타일 시트를 찾지 못했습니다: {styleSheetPath}");
        }

        private Toolbar buildToolbar()
        {
            m_settingsToggle = new ToolbarToggle { name = SETTINGS_TOGGLE, text = "설정" };
            m_settingsToggle.RegisterValueChangedCallback(_ => updateSettingsVisibility());

            var refreshButton = new ToolbarButton(onRefreshClicked) { name = REFRESH_BUTTON, text = "새로고침" };

            m_playFromStartToggle = new ToolbarToggle { name = PLAY_FROM_START_TOGGLE, text = "▶ 시작 씬부터" };
            m_playFromStartToggle.RegisterValueChangedCallback(evt => PlayFromStartScene.enabled = evt.newValue);

            m_searchField = new ToolbarSearchField { name = SEARCH_FIELD };
            m_searchField.SetValueWithoutNotify(m_search);
            m_searchField.RegisterValueChangedCallback(evt =>
            {
                m_search = evt.newValue;
                applySearch();
            });

            var toolbar = new Toolbar();
            toolbar.Add(m_settingsToggle);
            toolbar.Add(refreshButton);
            toolbar.Add(m_playFromStartToggle);
            toolbar.Add(new ToolbarSpacer { flex = true });
            toolbar.Add(m_searchField);
            return toolbar;
        }

        private VisualElement buildSettingsPanel()
        {
            m_settingsPanel = new VisualElement { name = SETTINGS_PANEL };

            m_startSceneField = new ObjectField("시작 씬")
            {
                name = START_SCENE_FIELD,
                objectType = typeof(SceneAsset),
                allowSceneObjects = false,
                tooltip = "비워 두면 Build Settings의 첫 활성 씬을 사용합니다.",
            };
            m_startSceneField.RegisterValueChangedCallback(evt => PlayFromStartScene.customStartScene = evt.newValue as SceneAsset);

            m_missingStartWarning = new VisualElement { name = MISSING_START_WARNING };
            m_missingStartWarning.AddToClassList("warning-row");
            m_missingStartWarning.Add(new HelpBox("지정한 시작 씬이 삭제되어 Build Settings 첫 씬을 사용 중입니다.", HelpBoxMessageType.Warning));
            m_missingStartWarning.Add(new Button(() => PlayFromStartScene.customStartScene = null) { text = "지정 해제" });

            m_startSceneHint = new Label { name = START_SCENE_HINT };

            var folderTitle = new Label("씬 폴더 (Project 창에서 폴더를 끌어다 놓아도 됩니다)");
            folderTitle.AddToClassList("section-title");

            m_folderList = new VisualElement { name = FOLDER_LIST };

            var addFolderField = new ObjectField("폴더 추가")
            {
                name = ADD_FOLDER_FIELD,
                objectType = typeof(DefaultAsset),
                allowSceneObjects = false,
            };
            addFolderField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null)
                    return;

                // 입력칸은 추가용이라 바로 비운다.
                addFolderField.SetValueWithoutNotify(null);
                addSceneFolder(AssetDatabase.GetAssetPath(evt.newValue));
            });

            m_settingsPanel.Add(m_startSceneField);
            m_settingsPanel.Add(m_missingStartWarning);
            m_settingsPanel.Add(m_startSceneHint);
            m_settingsPanel.Add(folderTitle);
            m_settingsPanel.Add(m_folderList);
            m_settingsPanel.Add(addFolderField);
            return m_settingsPanel;
        }

        private void registerFolderDrop(VisualElement target)
        {
            target.RegisterCallback<DragUpdatedEvent>(_ =>
            {
                if (0 < draggedFolders().Length)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            });

            target.RegisterCallback<DragPerformEvent>(_ =>
            {
                string[] folders = draggedFolders();
                if (folders.Length == 0)
                    return;

                DragAndDrop.AcceptDrag();
                foreach (string folder in folders)
                    addSceneFolder(folder);
            });
        }

        private static string[] draggedFolders() => DragAndDrop.paths.Where(AssetDatabase.IsValidFolder).ToArray();

        // ---------------------------------------------------------------- 씬 행

        private VisualElement createRow(string scenePath, string folderPath)
        {
            var row = new VisualElement { name = rowName(scenePath), tooltip = scenePath };
            row.AddToClassList(ROW_CLASS);

            var icon = new Image { image = m_sceneIcon };
            icon.AddToClassList("scene-row__icon");

            bool isStart = scenePath == m_startPath;
            var label = new Label(SceneSwitcherLogic.buildRowLabel(scenePath, folderPath, isStart, m_buildScenes.Contains(scenePath)));
            label.AddToClassList("scene-row__label");

            row.Add(icon);
            row.Add(label);

            registerRowInput(row, scenePath);
            row.AddManipulator(new ContextualMenuManipulator(evt => buildContextMenu(evt.menu, scenePath)));
            return row;
        }

        /// <summary>클릭은 마우스를 뗄 때 전환하고, 누른 채 움직이면 씬 에셋 드래그를 시작한다.</summary>
        private void registerRowInput(VisualElement row, string scenePath)
        {
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                m_pressedScenePath = scenePath;
                m_pressPosition = evt.position;
                // 캡처해 두면 행 밖으로 나가도 이동과 떼기 이벤트가 이 행으로 온다.
                row.CapturePointer(evt.pointerId);
            });

            row.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (m_pressedScenePath != scenePath || !row.HasPointerCapture(evt.pointerId))
                    return;
                if (!SceneSwitcherLogic.isDragStarted(m_pressPosition, evt.position))
                    return;

                m_pressedScenePath = null;
                row.ReleasePointer(evt.pointerId);
                startSceneDrag(scenePath);
            });

            row.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != 0 || m_pressedScenePath != scenePath)
                    return;

                m_pressedScenePath = null;
                row.ReleasePointer(evt.pointerId);

                if (row.ContainsPoint(evt.localPosition))
                    openScene(scenePath, evt.actionKey ? OpenSceneMode.Additive : OpenSceneMode.Single);
            });

            // 창이 포커스를 잃는 등으로 캡처가 풀리면 누름 상태도 버린다.
            row.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (m_pressedScenePath == scenePath)
                    m_pressedScenePath = null;
            });
        }

        /// <summary>실제 OS 드래그를 시작하는 함수. 테스트에서 기록용으로 바꿔 끼운다.</summary>
        internal Action<string> startDrag = DragAndDrop.StartDrag;

        /// <summary>Hierarchy에 놓으면 Unity 기본 동작으로 씬이 추가로 열린다.</summary>
        private void startSceneDrag(string scenePath)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
                return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { sceneAsset };
            DragAndDrop.paths = new[] { scenePath };
            startDrag(sceneAsset.name);
        }

        /// <summary>행의 우클릭 메뉴를 채운다. 테스트가 항목 상태를 확인하고 실행할 수 있게 internal로 둔다.</summary>
        internal void buildContextMenu(DropdownMenu menu, string scenePath)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            bool isLoaded = m_loadedScenes.Contains(scenePath);

            menu.AppendAction(MENU_OPEN, _ => openScene(scenePath, OpenSceneMode.Single));
            menu.AppendAction(MENU_OPEN_ADDITIVE, _ => openScene(scenePath, OpenSceneMode.Additive),
                status(SceneSwitcherLogic.canOpenAdditive(isLoaded)));
            menu.AppendAction(MENU_CLOSE, _ => closeScene(scenePath),
                status(SceneSwitcherLogic.canClose(isLoaded, SceneManager.sceneCount)));
            menu.AppendAction(MENU_PLAY_DIRECT, _ =>
            {
                if (openScene(scenePath, OpenSceneMode.Single))
                    PlayFromStartScene.playDirect();
            });
            menu.AppendSeparator();
            if (m_customStartScene == sceneAsset)
                menu.AppendAction(MENU_CLEAR_START_SCENE, _ => PlayFromStartScene.customStartScene = null);
            else
                menu.AppendAction(MENU_SET_START_SCENE, _ => PlayFromStartScene.customStartScene = sceneAsset);
            menu.AppendAction(MENU_PING, _ => EditorGUIUtility.PingObject(sceneAsset));
        }

        private static DropdownMenuAction.Status status(bool enabled)
        {
            return enabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }

        // ---------------------------------------------------------------- 씬 조작

        private bool openScene(string scenePath, OpenSceneMode mode)
        {
            if (!canEditScenes())
                return false;

            // Single 전환은 열려 있는 씬이 닫히므로 변경 사항 저장 여부를 먼저 묻는다.
            if (mode == OpenSceneMode.Single && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;

            EditorSceneManager.OpenScene(scenePath, mode);
            updateRowStates();
            return true;
        }

        private void closeScene(string scenePath)
        {
            if (!canEditScenes())
                return;

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!SceneSwitcherLogic.canClose(scene.IsValid() && scene.isLoaded, SceneManager.sceneCount))
                return;

            if (EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene }))
                EditorSceneManager.CloseScene(scene, true);
            updateRowStates();
        }

        private static bool canEditScenes()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return true;

            EditorUtility.DisplayDialog("Scene Switcher", "플레이 중에는 씬을 열거나 닫을 수 없습니다.", "확인");
            return false;
        }

        /// <summary>이미 등록된 폴더에 포함되면 막고, 등록된 하위 폴더가 있으면 합칠지 묻는다.</summary>
        private void addSceneFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
                return;

            FolderAddPlan plan = SceneSwitcherLogic.planAddFolder(folderPath, sceneFolderGuids.Select(AssetDatabase.GUIDToAssetPath));
            switch (plan.result)
            {
                case eFolderAddResult.InsideRegistered:
                    ShowNotification(new GUIContent($"이미 등록된 {plan.parentFolder}에 포함된 폴더입니다."));
                    return;

                case eFolderAddResult.MergeChildren:
                    string message = $"이 폴더 아래에 이미 등록된 폴더가 {plan.childFolders.Count}개 있습니다. 이 폴더로 합칠까요?";
                    if (!EditorUtility.DisplayDialog("Scene Switcher", message, "합치기", "취소"))
                        return;

                    foreach (string child in plan.childFolders)
                        settings.removeSceneFolder(AssetDatabase.AssetPathToGUID(child));
                    break;
            }

            settings.addSceneFolder(AssetDatabase.AssetPathToGUID(folderPath));
            refresh();
        }

        private void onRefreshClicked()
        {
            SceneSwitcherSettings.reload();
            // reapply가 applied 이벤트로 refresh를 부른다.
            PlayFromStartScene.reapply();
        }

        // ---------------------------------------------------------------- 갱신

        /// <summary>데이터를 다시 계산하고, 요소가 만들어져 있으면 화면에 반영한다.</summary>
        private void refresh()
        {
            // 삭제된 폴더는 빈 경로로 넘겨 건너뛰게 한다.
            IEnumerable<string> folderPaths = sceneFolderGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(p => AssetDatabase.IsValidFolder(p) ? p : null);
            m_groups = SceneSwitcherLogic.buildGroups(folderPaths, findScenes);

            m_buildScenes.Clear();
            m_buildScenes.UnionWith(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));

            m_customStartScene = PlayFromStartScene.customStartScene;
            m_isCustomStartMissing = PlayFromStartScene.isCustomStartSceneMissing;
            m_startScene = PlayFromStartScene.startScene;
            m_startPath = m_startScene != null ? AssetDatabase.GetAssetPath(m_startScene) : "";

            if (!isBuilt)
                return;

            updatePlayToggle();
            updateStartSceneSettings();
            rebuildFolderList();
            rebuildSceneList();
            updateSettingsVisibility();
            updateRowStates();
        }

        private void updatePlayToggle()
        {
            bool enabled = PlayFromStartScene.enabled;
            // 설정에서 읽어 온 값이므로 다시 저장하지 않도록 알림 없이 넣는다.
            m_playFromStartToggle.SetValueWithoutNotify(enabled);
            m_playFromStartToggle.EnableInClassList(TOGGLE_ON_CLASS, enabled);
            m_playFromStartToggle.tooltip = m_startScene != null ? $"시작 씬: {m_startScene.name}" : "시작 씬 없음";
        }

        private void updateStartSceneSettings()
        {
            m_startSceneField.SetValueWithoutNotify(m_customStartScene);
            m_missingStartWarning.EnableInClassList(HIDDEN_CLASS, !m_isCustomStartMissing);

            bool showHint = !m_isCustomStartMissing && m_customStartScene == null;
            m_startSceneHint.EnableInClassList(HIDDEN_CLASS, !showHint);
            m_startSceneHint.text = m_startScene != null ? $"Build Settings 첫 씬 사용: {m_startScene.name}" : "Build Settings에 활성 씬이 없습니다.";
        }

        private void rebuildFolderList()
        {
            m_folderList.Clear();
            foreach (string guid in sceneFolderGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var row = new VisualElement();
                row.AddToClassList("folder-row");
                row.Add(new Label(string.IsNullOrEmpty(path) ? "(삭제된 폴더)" : path));
                row.Add(new Button(() =>
                {
                    settings.removeSceneFolder(guid);
                    refresh();
                }) { text = "-" });
                m_folderList.Add(row);
            }
        }

        private void rebuildSceneList()
        {
            m_sceneList.Clear();
            m_foldouts.Clear();
            m_rows.Clear();

            foreach (SceneFolderGroup group in m_groups)
            {
                // viewDataKey가 있으면 접힘 상태를 Unity가 저장하고 복원한다.
                var foldout = new Foldout { name = groupName(group.folderPath), viewDataKey = VIEW_DATA_PREFIX + group.folderPath };
                foreach (string scenePath in group.scenePaths)
                {
                    VisualElement row = createRow(scenePath, group.folderPath);
                    foldout.Add(row);
                    m_rows[scenePath] = row;
                }

                m_sceneList.Add(foldout);
                m_foldouts[group.folderPath] = foldout;
            }
            applySearch();
        }

        /// <summary>목록을 다시 만들지 않고 클래스만 바꿔 검색 결과를 보여 준다.</summary>
        private void applySearch()
        {
            foreach (SceneFolderGroup group in m_groups)
            {
                int shown = 0;
                foreach (string scenePath in group.scenePaths)
                {
                    bool match = matchesSearch(scenePath);
                    m_rows[scenePath].EnableInClassList(HIDDEN_CLASS, !match);
                    if (match)
                        ++shown;
                }
                m_foldouts[group.folderPath].text = $"{group.folderPath} ({shown})";
            }
        }

        private void updateSettingsVisibility()
        {
            bool hasFolders = 0 < sceneFolderGuids.Count;
            m_settingsPanel.EnableInClassList(HIDDEN_CLASS, hasFolders && !m_settingsToggle.value);
            m_emptyHelp.EnableInClassList(HIDDEN_CLASS, hasFolders);
        }

        private void updateRowStates()
        {
            m_loadedScenes.Clear();
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    m_loadedScenes.Add(scene.path);
            }
            m_activePath = SceneManager.GetActiveScene().path;

            foreach (KeyValuePair<string, VisualElement> pair in m_rows)
            {
                bool isActive = pair.Key == m_activePath;
                pair.Value.EnableInClassList(ACTIVE_CLASS, isActive);
                pair.Value.EnableInClassList(LOADED_CLASS, !isActive && m_loadedScenes.Contains(pair.Key));
            }
        }

        private bool matchesSearch(string scenePath)
        {
            return string.IsNullOrEmpty(m_search)
                || Path.GetFileNameWithoutExtension(scenePath).IndexOf(m_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<string> findScenes(string folderPath)
        {
            return AssetDatabase.FindAssets("t:SceneAsset", new[] { folderPath }).Select(AssetDatabase.GUIDToAssetPath);
        }
    }
}
