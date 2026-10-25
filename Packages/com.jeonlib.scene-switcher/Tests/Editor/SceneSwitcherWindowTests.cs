using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace JeonLib.SceneSwitcher.Tests
{
    /// <summary>창을 실제로 띄워 요소 구성과 조작(검색, 설정 토글, 클릭 전환)을 검증한다.</summary>
    internal class SceneSwitcherWindowTests
    {
        private const string TEMP_FOLDER = "Assets/__SceneSwitcherWindowTestTemp";
        private const string SCENE_PATH_A = TEMP_FOLDER + "/Alpha.unity";
        private const string SCENE_PATH_B = TEMP_FOLDER + "/Bravo.unity";
        private const string SUB_FOLDER_NAME = "Sub";
        private const string SUB_FOLDER = TEMP_FOLDER + "/" + SUB_FOLDER_NAME;

        private SceneSwitcherWindow m_window;

        private VisualElement root => m_window.rootVisualElement;

        // 에셋 저장과 삭제는 import가 느려서 픽스처 전체에서 한 번만 한다.
        [OneTimeSetUp]
        public void createTempScenes()
        {
            if (!AssetDatabase.IsValidFolder(TEMP_FOLDER))
                AssetDatabase.CreateFolder("Assets", TEMP_FOLDER.Substring("Assets/".Length));

            // 테스트 러너가 연 미저장 씬을 복사본으로만 저장해 열린 씬 상태는 그대로 둔다.
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCENE_PATH_A, true);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCENE_PATH_B, true);
        }

        [OneTimeTearDown]
        public void deleteTempScenes()
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
            // 테스트가 남긴 드래그 내용을 비운다.
            DragAndDrop.PrepareStartDrag();

            if (m_window != null)
                m_window.Close();

            // 테스트가 연 임시 씬을 닫아 다음 테스트가 같은 상태에서 시작하게 한다.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (AssetDatabase.IsValidFolder(SUB_FOLDER))
                AssetDatabase.DeleteAsset(SUB_FOLDER);
            SettingsFileBackup.restore();
        }

        /// <summary>설정을 준비한 뒤 새 창을 띄운다. 사용자가 도킹해 둔 창은 건드리지 않는다.</summary>
        private IEnumerator openWindow(bool registerFolder = true)
        {
            if (registerFolder)
                SceneSwitcherSettings.instance.addSceneFolder(AssetDatabase.AssetPathToGUID(TEMP_FOLDER));

            m_window = EditorWindow.CreateWindow<SceneSwitcherWindow>();
            yield return waitFrames();
        }

        private static IEnumerator waitFrames(int count = 3)
        {
            for (int i = 0; i < count; ++i)
                yield return null;
        }

        private VisualElement row(string scenePath) => root.Q(SceneSwitcherUI.rowName(scenePath));

        private static bool isShown(VisualElement element)
        {
            return element != null && element.resolvedStyle.display != DisplayStyle.None;
        }

        private static void click(VisualElement target, EventModifiers modifiers = EventModifiers.None)
        {
            Vector2 position = target.worldBound.center;
            send(target, new Event { type = EventType.MouseDown, button = 0, mousePosition = position, modifiers = modifiers });
            send(target, new Event { type = EventType.MouseUp, button = 0, mousePosition = position, modifiers = modifiers });
        }

        private static void send(VisualElement target, Event imguiEvent)
        {
            EventBase pointerEvent;
            switch (imguiEvent.type)
            {
                case EventType.MouseDown:
                    pointerEvent = PointerDownEvent.GetPooled(imguiEvent);
                    break;
                case EventType.MouseDrag:
                    pointerEvent = PointerMoveEvent.GetPooled(imguiEvent);
                    break;
                default:
                    pointerEvent = PointerUpEvent.GetPooled(imguiEvent);
                    break;
            }

            using (pointerEvent)
                target.SendEvent(pointerEvent);
        }

        /// <summary>누른 채 offset만큼 움직인다. 떼지는 않는다.</summary>
        private static void pressAndMove(VisualElement target, Vector2 offset)
        {
            Vector2 start = target.worldBound.center;
            send(target, new Event { type = EventType.MouseDown, button = 0, mousePosition = start });
            send(target, new Event { type = EventType.MouseDrag, button = 0, mousePosition = start + offset });
        }

        private static void release(VisualElement target, Vector2 offset)
        {
            send(target, new Event { type = EventType.MouseUp, button = 0, mousePosition = target.worldBound.center + offset });
        }

        /// <summary>Project 창에서 폴더를 끌어와 창에 놓은 것처럼 보낸다.</summary>
        /// <remarks>요소에 직접 보낸 드래그 이벤트는 UI Toolkit이 걸러서, 창에 IMGUI 이벤트로 보내 일반 입력 경로를 탄다.</remarks>
        private void dropFolder(string folderPath)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = new[] { folderPath };
            DragAndDrop.objectReferences = new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath) };

            Vector2 center = root.worldBound.center;
            m_window.SendEvent(new Event { type = EventType.DragUpdated, mousePosition = center });
            m_window.SendEvent(new Event { type = EventType.DragPerform, mousePosition = center });
        }

        private DropdownMenuAction menuAction(string scenePath, string itemName)
        {
            var menu = new DropdownMenu();
            m_window.buildContextMenu(menu, scenePath);
            menu.PrepareForDisplay(null);

            foreach (DropdownMenuItem item in menu.MenuItems())
            {
                if (item is DropdownMenuAction action && action.name == itemName)
                    return action;
            }
            Assert.Fail($"메뉴 항목을 찾지 못함: {itemName}");
            return null;
        }

        // ---- 구성 ----

        [UnityTest]
        public IEnumerator 폴더_그룹과_씬_행이_만들어짐()
        {
            yield return openWindow();

            Assert.That(root.Q(SceneSwitcherUI.groupName(TEMP_FOLDER)), Is.Not.Null);
            Assert.That(row(SCENE_PATH_A), Is.Not.Null);
            Assert.That(row(SCENE_PATH_B), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator 폴더가_없으면_설정_패널과_안내가_보임()
        {
            yield return openWindow(registerFolder: false);

            Assert.That(isShown(root.Q(SceneSwitcherUI.SETTINGS_PANEL)), Is.True);
            Assert.That(isShown(root.Q(SceneSwitcherUI.EMPTY_HELP)), Is.True);
        }

        [UnityTest]
        public IEnumerator 시작_씬으로_지정한_행에_Start_표시()
        {
            PlayFromStartScene.customStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH_A);
            yield return openWindow();

            Assert.That(row(SCENE_PATH_A).Q<Label>().text, Does.Contain("[Start]"));
            Assert.That(row(SCENE_PATH_B).Q<Label>().text, Does.Not.Contain("[Start]"));
        }

        // ---- 툴바 ----

        [UnityTest]
        public IEnumerator 설정_토글로_설정_패널을_보이고_숨김()
        {
            yield return openWindow();
            var toggle = root.Q<ToolbarToggle>(SceneSwitcherUI.SETTINGS_TOGGLE);
            var panel = root.Q(SceneSwitcherUI.SETTINGS_PANEL);
            Assert.That(isShown(panel), Is.False);

            toggle.value = true;
            yield return waitFrames();
            Assert.That(isShown(panel), Is.True);

            toggle.value = false;
            yield return waitFrames();
            Assert.That(isShown(panel), Is.False);
        }

        [UnityTest]
        public IEnumerator 검색하면_이름이_맞는_행만_보임()
        {
            yield return openWindow();

            root.Q<ToolbarSearchField>(SceneSwitcherUI.SEARCH_FIELD).value = "brav";
            yield return waitFrames();

            Assert.That(isShown(row(SCENE_PATH_A)), Is.False);
            Assert.That(isShown(row(SCENE_PATH_B)), Is.True);
        }

        [UnityTest]
        public IEnumerator 시작_씬부터_토글이_설정에_반영됨()
        {
            yield return openWindow();

            root.Q<ToolbarToggle>(SceneSwitcherUI.PLAY_FROM_START_TOGGLE).value = true;

            Assert.That(PlayFromStartScene.enabled, Is.True);
        }

        // ---- 씬 행 조작 ----

        [UnityTest]
        public IEnumerator 클릭하면_씬이_전환되고_활성_표시()
        {
            yield return openWindow();

            click(row(SCENE_PATH_A));
            yield return waitFrames();

            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(SCENE_PATH_A));
            Assert.That(row(SCENE_PATH_A).ClassListContains(SceneSwitcherUI.ACTIVE_CLASS), Is.True);
        }

        [UnityTest]
        public IEnumerator Ctrl_클릭하면_추가로_열리고_열림_표시()
        {
            yield return openWindow();
            click(row(SCENE_PATH_A));
            yield return waitFrames();

            click(row(SCENE_PATH_B), EventModifiers.Control);
            yield return waitFrames();

            Assert.That(SceneManager.sceneCount, Is.EqualTo(2));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(SCENE_PATH_A));
            Assert.That(row(SCENE_PATH_B).ClassListContains(SceneSwitcherUI.LOADED_CLASS), Is.True);
        }

        // ---- 행 드래그 ----

        /// <summary>실제 OS 드래그는 마우스를 뗄 때까지 에디터를 붙잡을 수 있어, 시작 함수를 기록용으로 바꿔 끼운다.</summary>
        private List<string> fakeStartDrag()
        {
            var startedTitles = new List<string>();
            m_window.startDrag = title => startedTitles.Add(title);
            return startedTitles;
        }

        [UnityTest]
        public IEnumerator 누른_채_기준_거리를_넘게_움직이면_씬_에셋_드래그가_시작됨()
        {
            yield return openWindow();
            List<string> startedTitles = fakeStartDrag();

            pressAndMove(row(SCENE_PATH_A), new Vector2(SceneSwitcherLogic.DRAG_THRESHOLD + 1f, 0f));

            var sceneA = AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH_A);
            Assert.That(startedTitles, Is.EqualTo(new[] { sceneA.name }));
            Assert.That(DragAndDrop.objectReferences, Is.EqualTo(new Object[] { sceneA }));
            Assert.That(DragAndDrop.paths, Is.EqualTo(new[] { SCENE_PATH_A }));
        }

        [UnityTest]
        public IEnumerator 기준_거리_안에서_떼면_드래그_없이_전환됨()
        {
            yield return openWindow();
            List<string> startedTitles = fakeStartDrag();
            var offset = new Vector2(SceneSwitcherLogic.DRAG_THRESHOLD - 1f, 0f);

            pressAndMove(row(SCENE_PATH_A), offset);
            release(row(SCENE_PATH_A), offset);
            yield return waitFrames();

            Assert.That(startedTitles, Is.Empty);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(SCENE_PATH_A));
        }

        // ---- 폴더 끌어다 놓기 ----

        [UnityTest]
        public IEnumerator 폴더를_창에_놓으면_등록되고_씬이_보임()
        {
            yield return openWindow(registerFolder: false);

            dropFolder(TEMP_FOLDER);
            yield return waitFrames();

            Assert.That(SceneSwitcherSettings.instance.sceneFolderGuids, Does.Contain(AssetDatabase.AssetPathToGUID(TEMP_FOLDER)));
            Assert.That(row(SCENE_PATH_A), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator 등록된_폴더의_하위_폴더를_놓으면_추가되지_않음()
        {
            string subFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder(TEMP_FOLDER, SUB_FOLDER_NAME));
            yield return openWindow();

            dropFolder(subFolder);
            yield return waitFrames();

            Assert.That(SceneSwitcherSettings.instance.sceneFolderGuids, Is.EqualTo(new[] { AssetDatabase.AssetPathToGUID(TEMP_FOLDER) }));
        }

        // ---- 우클릭 메뉴 ----

        [UnityTest]
        public IEnumerator 열린_씬이_하나뿐이면_닫기와_추가로_열기가_비활성()
        {
            yield return openWindow();
            click(row(SCENE_PATH_A));
            yield return waitFrames();

            Assert.That(menuAction(SCENE_PATH_A, SceneSwitcherUI.MENU_CLOSE).status, Is.EqualTo(DropdownMenuAction.Status.Disabled));
            Assert.That(menuAction(SCENE_PATH_A, SceneSwitcherUI.MENU_OPEN_ADDITIVE).status, Is.EqualTo(DropdownMenuAction.Status.Disabled));
            Assert.That(menuAction(SCENE_PATH_B, SceneSwitcherUI.MENU_OPEN_ADDITIVE).status, Is.EqualTo(DropdownMenuAction.Status.Normal));
        }

        [UnityTest]
        public IEnumerator 메뉴로_추가로_연_씬을_닫을_수_있음()
        {
            yield return openWindow();
            click(row(SCENE_PATH_A));
            yield return waitFrames();
            menuAction(SCENE_PATH_B, SceneSwitcherUI.MENU_OPEN_ADDITIVE).Execute();
            yield return waitFrames();
            Assert.That(SceneManager.sceneCount, Is.EqualTo(2));

            menuAction(SCENE_PATH_B, SceneSwitcherUI.MENU_CLOSE).Execute();
            yield return waitFrames();

            Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
            Assert.That(row(SCENE_PATH_B).ClassListContains(SceneSwitcherUI.LOADED_CLASS), Is.False);
        }

        [UnityTest]
        public IEnumerator 메뉴로_시작_씬을_지정하면_설정과_표시에_반영됨()
        {
            yield return openWindow();

            menuAction(SCENE_PATH_B, SceneSwitcherUI.MENU_SET_START_SCENE).Execute();
            yield return waitFrames();

            Assert.That(PlayFromStartScene.customStartScene, Is.EqualTo(AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH_B)));
            Assert.That(row(SCENE_PATH_B).Q<Label>().text, Does.Contain("[Start]"));
        }
    }
}
