using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace JeonLib.ShaderTimePreview
{
    /// <summary>`_Time.y - 시작 시각`으로 진행하는 셰이더를 창 안의 쿼드에 그려, 원하는 시점에 멈춰 보거나 재생하는 창.</summary>
    internal class ShaderTimePreviewWindow : EditorWindow
    {
        private const string STYLE_SHEET_NAME = "ShaderTimePreviewWindow.uss";
        private const string BUTTON_ROW_CLASS = "button-row";
        private const string PREVIEW_CLASS = "preview";
        private const float MIN_DURATION = 0.01f;
        // 쿼드(1x1) 둘레에 남길 여백. 1이면 쿼드가 미리보기를 꽉 채운다.
        private const float VIEW_PADDING = 1.1f;
        private const float CAMERA_DISTANCE = 5f;

        // 도메인 리로드 뒤에도 설정이 남도록 직렬화한다.
        [SerializeField] private Material m_material;
        [SerializeField] private string m_startTimeProperty = "_StartTime";
        [SerializeField] private string m_colorProperty = "";
        [SerializeField] private Color m_color = Color.white;
        [SerializeField] private Color m_background = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private float m_duration = 1f;
        [SerializeField] private float m_elapsed;
        [SerializeField] private bool m_loop = true;

        private Slider m_elapsedSlider;
        private Button m_playButton;
        private IMGUIContainer m_previewArea;

        private PreviewRenderUtility m_preview;
        private Mesh m_quad;
        private MaterialPropertyBlock m_propertyBlock;

        private bool m_playing;
        private double m_lastUpdateTime;

        [MenuItem("Tools/Shader Time Preview/Open")]
        private static void open()
        {
            GetWindow<ShaderTimePreviewWindow>("Shader Time Preview");
        }

        private void OnEnable()
        {
            m_preview = new PreviewRenderUtility();
            Camera camera = m_preview.camera;
            camera.orthographic = true;
            camera.orthographicSize = 0.5f * VIEW_PADDING;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -CAMERA_DISTANCE), Quaternion.identity);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = CAMERA_DISTANCE * 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;

            m_quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            m_propertyBlock = new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            stop();
            m_preview?.Cleanup();
            m_preview = null;
        }

        private void CreateGUI()
        {
            loadStyleSheet();

            var materialField = new ObjectField("Material") { objectType = typeof(Material), allowSceneObjects = false, value = m_material };
            materialField.RegisterValueChangedCallback(evt =>
            {
                m_material = evt.newValue as Material;
                repaintPreview();
            });
            rootVisualElement.Add(materialField);

            var startTimeField = new TextField("Start Time Property") { isDelayed = true, value = m_startTimeProperty };
            startTimeField.RegisterValueChangedCallback(evt =>
            {
                m_startTimeProperty = evt.newValue;
                repaintPreview();
            });
            rootVisualElement.Add(startTimeField);

            var colorPropertyField = new TextField("Color Property") { isDelayed = true, value = m_colorProperty, tooltip = "비워 두면 색을 넘기지 않고 머티리얼 값을 씁니다." };
            colorPropertyField.RegisterValueChangedCallback(evt =>
            {
                m_colorProperty = evt.newValue;
                repaintPreview();
            });
            rootVisualElement.Add(colorPropertyField);

            var colorField = new ColorField("Color") { hdr = true, value = m_color };
            colorField.RegisterValueChangedCallback(evt =>
            {
                m_color = evt.newValue;
                repaintPreview();
            });
            rootVisualElement.Add(colorField);

            var backgroundField = new ColorField("Background") { showAlpha = false, value = m_background };
            backgroundField.RegisterValueChangedCallback(evt =>
            {
                m_background = evt.newValue;
                repaintPreview();
            });
            rootVisualElement.Add(backgroundField);

            var durationField = new FloatField("Duration") { isDelayed = true, value = m_duration };
            durationField.RegisterValueChangedCallback(evt =>
            {
                m_duration = Mathf.Max(MIN_DURATION, evt.newValue);
                m_elapsed = Mathf.Min(m_elapsed, m_duration);
                durationField.SetValueWithoutNotify(m_duration);
                m_elapsedSlider.highValue = m_duration;
                m_elapsedSlider.SetValueWithoutNotify(m_elapsed);
                repaintPreview();
            });
            rootVisualElement.Add(durationField);

            m_elapsedSlider = new Slider("Elapsed", 0f, m_duration) { showInputField = true, value = m_elapsed };
            m_elapsedSlider.RegisterValueChangedCallback(evt =>
            {
                // 직접 움직이면 재생을 멈추고 그 시점에 고정한다.
                stop();
                m_elapsed = evt.newValue;
                repaintPreview();
            });
            rootVisualElement.Add(m_elapsedSlider);

            var buttonRow = new VisualElement();
            buttonRow.AddToClassList(BUTTON_ROW_CLASS);
            m_playButton = new Button(togglePlay) { text = "Play" };
            buttonRow.Add(m_playButton);
            var loopToggle = new Toggle { text = "Loop", value = m_loop };
            loopToggle.RegisterValueChangedCallback(evt => m_loop = evt.newValue);
            buttonRow.Add(loopToggle);
            rootVisualElement.Add(buttonRow);

            m_previewArea = new IMGUIContainer(drawPreview);
            m_previewArea.AddToClassList(PREVIEW_CLASS);
            rootVisualElement.Add(m_previewArea);
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
                Debug.LogWarning($"[ShaderTimePreview] 스타일 시트를 찾지 못했습니다: {styleSheetPath}");
        }

        private void drawPreview()
        {
            Rect area = m_previewArea.contentRect;
            if (m_material == null)
            {
                EditorGUI.LabelField(area, "Material을 지정하세요.", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            if (Event.current.type != EventType.Repaint || m_preview == null)
                return;

            // 쿼드가 찌그러지지 않게 가운데 정사각형에 그린다.
            float side = Mathf.Min(area.width, area.height);
            if (side <= 0f)
                return;
            var rect = new Rect(area.x + (area.width - side) * 0.5f, area.y + (area.height - side) * 0.5f, side, side);

            // MPB로 넘기면 머티리얼 에셋을 건드리지 않고, SRP Batcher를 거치지 않아 CBUFFER 밖의 프로퍼티에도 값이 들어간다.
            m_propertyBlock.Clear();
            float shaderTime = ShaderTimePreviewLogic.shaderTime(EditorApplication.isPlaying, Time.time, Time.realtimeSinceStartup);
            if (!string.IsNullOrEmpty(m_startTimeProperty))
                m_propertyBlock.SetFloat(m_startTimeProperty, ShaderTimePreviewLogic.startTimeFor(shaderTime, m_elapsed));
            if (!string.IsNullOrEmpty(m_colorProperty))
                m_propertyBlock.SetColor(m_colorProperty, m_color);

            m_preview.camera.backgroundColor = m_background;
            m_preview.BeginPreview(rect, GUIStyle.none);
            m_preview.DrawMesh(m_quad, Matrix4x4.identity, m_material, 0, m_propertyBlock);
            m_preview.camera.Render();
            m_preview.EndAndDrawPreview(rect);
        }

        private void togglePlay()
        {
            if (m_playing)
            {
                stop();
                return;
            }

            if (m_elapsed >= m_duration)
                m_elapsed = 0f;

            m_playing = true;
            m_lastUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += onUpdate;
            m_playButton.text = "Stop";
        }

        private void stop()
        {
            if (!m_playing)
                return;

            m_playing = false;
            EditorApplication.update -= onUpdate;
            if (m_playButton != null)
                m_playButton.text = "Play";
        }

        private void onUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - m_lastUpdateTime);
            m_lastUpdateTime = now;

            bool keepPlaying = ShaderTimePreviewLogic.advance(ref m_elapsed, delta, m_duration, m_loop);
            m_elapsedSlider?.SetValueWithoutNotify(m_elapsed);
            repaintPreview();

            if (!keepPlaying)
                stop();
        }

        private void repaintPreview()
        {
            m_previewArea?.MarkDirtyRepaint();
        }
    }
}
