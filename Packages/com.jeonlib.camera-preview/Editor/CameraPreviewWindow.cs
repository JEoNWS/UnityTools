using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JeonLib.CameraPreview
{
    /// <summary>선택하거나 핀으로 지정한 카메라의 화면을 보여 주는 창.</summary>
    internal class CameraPreviewWindow : EditorWindow
    {
        private const string STYLE_SHEET_NAME = "CameraPreviewWindow.uss";
        private const string HIDDEN_CLASS = "hidden";
        private const long RENDER_INTERVAL_MS = 50;
        private const int DEPTH_BITS = 24;

        private static readonly string[] s_aspectLabels = { "Game 뷰", "16:9", "4:3", "1:1" };

        // 도메인 리로드 뒤에도 핀과 비율을 유지한다.
        [SerializeField] private Camera m_pinned;
        [SerializeField] private eAspectMode m_aspectMode;

        private ToolbarToggle m_pinToggle;
        private Label m_targetLabel;
        private HelpBox m_statusHelp;
        private VisualElement m_previewArea;
        private IMGUIContainer m_previewDrawer;

        private RenderTexture m_texture;
        private readonly RenderPipeline.StandardRequest m_request = new RenderPipeline.StandardRequest();

        private bool isBuilt => m_previewArea != null;

        [MenuItem("Tools/Camera Preview/Open")]
        private static void open()
        {
            GetWindow<CameraPreviewWindow>("Camera Preview");
        }

        private void OnEnable()
        {
            Selection.selectionChanged += renderPreview;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= renderPreview;
            releaseTexture();
        }

        private void CreateGUI()
        {
            loadStyleSheet();

            rootVisualElement.Add(buildToolbar());

            m_statusHelp = new HelpBox("", HelpBoxMessageType.Info);
            rootVisualElement.Add(m_statusHelp);

            m_previewArea = new VisualElement { name = "preview-area" };
            m_previewDrawer = new IMGUIContainer(drawPreview) { name = "preview-drawer" };
            m_previewArea.Add(m_previewDrawer);
            rootVisualElement.Add(m_previewArea);

            // 씬 편집은 이벤트가 없어서 주기적으로 다시 렌더한다.
            rootVisualElement.schedule.Execute(renderPreview).Every(RENDER_INTERVAL_MS);
            renderPreview();
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
                Debug.LogWarning($"[CameraPreview] 스타일 시트를 찾지 못했습니다: {styleSheetPath}");
        }

        private VisualElement buildToolbar()
        {
            var toolbar = new Toolbar();

            m_pinToggle = new ToolbarToggle { text = "핀", tooltip = "선택이 바뀌어도 지금 카메라를 계속 보여 줍니다." };
            m_pinToggle.SetValueWithoutNotify(m_pinned != null);
            m_pinToggle.RegisterValueChangedCallback(evt => setPinned(evt.newValue ? currentTarget() : null));
            toolbar.Add(m_pinToggle);

            var cameraButton = new ToolbarButton { text = "카메라 ▾", tooltip = "씬의 카메라를 골라 핀으로 지정합니다." };
            cameraButton.clicked += () => showCameraMenu(cameraButton);
            toolbar.Add(cameraButton);

            m_targetLabel = new Label { name = "target-label" };
            toolbar.Add(m_targetLabel);

            toolbar.Add(new ToolbarSpacer { flex = true });

            var aspectField = new DropdownField(s_aspectLabels.ToList(), (int)m_aspectMode) { name = "aspect-field" };
            aspectField.RegisterValueChangedCallback(_ =>
            {
                m_aspectMode = (eAspectMode)aspectField.index;
                renderPreview();
            });
            toolbar.Add(aspectField);

            return toolbar;
        }

        private void showCameraMenu(VisualElement anchor)
        {
            // 씬이든 Prefab Mode든 지금 열린 화면의 카메라만 보여 준다.
            var cameras = StageUtility.GetCurrentStageHandle().FindComponentsOfType<Camera>()
                .Where(CameraPreviewLogic.isListedCamera)
                .OrderBy(cam => cam.name)
                .ToList();

            var menu = new GenericMenu();
            if (cameras.Count == 0)
                menu.AddDisabledItem(new GUIContent("씬에 카메라가 없습니다"));

            foreach (var cam in cameras)
            {
                Camera captured = cam;
                menu.AddItem(new GUIContent(cam.name), cam == m_pinned, () => setPinned(captured));
            }
            menu.DropDown(anchor.worldBound);
        }

        private void setPinned(Camera cam)
        {
            m_pinned = cam;
            m_pinToggle.SetValueWithoutNotify(cam != null);
            renderPreview();
        }

        private Camera currentTarget()
        {
            return CameraPreviewLogic.resolveTarget(m_pinned, Selection.activeGameObject);
        }

        private void renderPreview()
        {
            if (!isBuilt)
                return;

            // 핀 카메라가 삭제되면 토글도 풀어 준다.
            m_pinToggle.SetValueWithoutNotify(m_pinned != null);

            Camera target = currentTarget();
            bool isAsset = target != null && EditorUtility.IsPersistent(target);
            eRenderPath path = target != null && !isAsset ? renderPathOf(target) : eRenderPath.BuiltIn;
            ePreviewStatus status = CameraPreviewLogic.resolveStatus(EditorApplication.isPlaying, target, isAsset, path);

            m_targetLabel.text = target != null ? target.name : "";
            showStatus(status);
            if (status != ePreviewStatus.Ready)
                return;

            float aspect = CameraPreviewLogic.aspectOf(m_aspectMode, Handles.GetMainGameViewSize());
            Rect fit = CameraPreviewLogic.fitRect(m_previewArea.contentRect.size, aspect);
            Vector2Int size = CameraPreviewLogic.textureSize(fit.size, EditorGUIUtility.pixelsPerPoint);
            if (size == Vector2Int.zero)
                return;

            ensureTexture(size);

            // Prefab Mode의 카메라는 메인 씬이 아니라 프리팹 편집 씬을 찍게 잠깐 바꾼다.
            Scene originalScene = target.scene;
            if (StageUtility.GetStage(target.gameObject) is PreviewSceneStage previewStage)
                target.scene = previewStage.scene;
            try
            {
                if (path == eRenderPath.BuiltIn)
                {
                    renderBuiltIn(target);
                }
                else
                {
                    m_request.destination = m_texture;
                    RenderPipeline.SubmitRenderRequest(target, m_request);
                }
            }
            finally
            {
                target.scene = originalScene;
            }
            m_previewDrawer.MarkDirtyRepaint();
        }

        /// <summary>Game 뷰처럼 알파를 무시하고 그린다. 투명 배경 카메라가 검게 보이지 않게.</summary>
        private void drawPreview()
        {
            if (m_texture == null)
                return;

            GUI.DrawTexture(m_previewDrawer.contentRect, m_texture, ScaleMode.ScaleToFit, false);
        }

        private eRenderPath renderPathOf(Camera cam)
        {
            bool hasPipeline = GraphicsSettings.currentRenderPipeline != null;
            bool supportsRequest = hasPipeline && RenderPipeline.SupportsRenderRequest(cam, m_request);
            return CameraPreviewLogic.chooseRenderPath(hasPipeline, supportsRequest);
        }

        private void renderBuiltIn(Camera cam)
        {
            RenderTexture original = cam.targetTexture;
            cam.targetTexture = m_texture;
            try
            {
                cam.Render();
            }
            finally
            {
                cam.targetTexture = original;
            }
        }

        private void showStatus(ePreviewStatus status)
        {
            bool ready = status == ePreviewStatus.Ready;
            m_statusHelp.EnableInClassList(HIDDEN_CLASS, ready);
            m_previewArea.EnableInClassList(HIDDEN_CLASS, !ready);
            if (!ready)
                m_statusHelp.text = statusMessage(status);
        }

        private static string statusMessage(ePreviewStatus status)
        {
            switch (status)
            {
                case ePreviewStatus.Playing:
                    return "플레이 중에는 미리보기를 멈춥니다. Game 뷰를 사용하세요.";
                case ePreviewStatus.NoCamera:
                    return "Camera가 있는 오브젝트를 선택하거나 카메라 메뉴에서 고르세요.";
                case ePreviewStatus.PrefabAsset:
                    return "프리팹 에셋의 카메라는 미리볼 수 없습니다. 프리팹을 Prefab Mode로 열거나 씬에 놓고 선택하세요.";
                case ePreviewStatus.Unsupported:
                    return "지금 렌더 파이프라인은 렌더 요청(StandardRequest)을 지원하지 않아 미리볼 수 없습니다.";
                default:
                    return "";
            }
        }

        private void ensureTexture(Vector2Int size)
        {
            if (m_texture != null && m_texture.width == size.x && m_texture.height == size.y)
                return;

            releaseTexture();
            m_texture = new RenderTexture(size.x, size.y, DEPTH_BITS)
            {
                name = "CameraPreview",
                hideFlags = HideFlags.HideAndDontSave,
            };
            m_texture.Create();
        }

        private void releaseTexture()
        {
            if (m_texture == null)
                return;

            m_texture.Release();
            DestroyImmediate(m_texture);
            m_texture = null;
        }
    }
}
