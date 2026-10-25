using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace JeonLib.WorldTransform
{
    /// <summary>선택한 오브젝트의 월드 위치, 회전, 스케일을 보여 주고 수정하는 창.</summary>
    internal class WorldTransformWindow : EditorWindow
    {
        private enum eField
        {
            Position,
            Rotation,
            Scale,
        }

        private const string STYLE_SHEET_NAME = "WorldTransformWindow.uss";
        private const string HIDDEN_CLASS = "hidden";
        private const string ROW_CLASS = "transform-row";
        private const string ROW_LABEL_CLASS = "transform-row__label";
        private const string AXIS_FIELD_CLASS = "axis-field";
        private const string AXIS_NAMES = "XYZ";
        private const long REFRESH_INTERVAL_MS = 100;
        private const float DISPLAY_PRECISION = 100000f;

        // CreateGUI에서 한 번 만들어 두고 값만 갱신하는 요소들.
        private Label m_targetLabel;
        private HelpBox m_emptyHelp;
        private VisualElement m_fieldsPanel;
        private HelpBox m_skewHelp;
        private readonly Dictionary<eField, FloatField[]> m_fields = new Dictionary<eField, FloatField[]>();

        private List<Transform> m_targets = new List<Transform>();
        // 대상마다 마지막으로 보여 준 euler. 같은 회전이 다른 각도로 뒤집혀 보이지 않게 한다.
        private readonly Dictionary<Transform, Vector3> m_eulerHints = new Dictionary<Transform, Vector3>();

        private bool isBuilt => m_fieldsPanel != null;

        [MenuItem("Tools/World Transform/Open")]
        private static void open()
        {
            GetWindow<WorldTransformWindow>("World Transform");
        }

        private void OnEnable()
        {
            Selection.selectionChanged += onSelectionChanged;
            Undo.undoRedoPerformed += refreshValues;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= onSelectionChanged;
            Undo.undoRedoPerformed -= refreshValues;
        }

        private void CreateGUI()
        {
            loadStyleSheet();

            m_emptyHelp = new HelpBox("오브젝트를 선택하세요.", HelpBoxMessageType.Info);
            rootVisualElement.Add(m_emptyHelp);

            m_fieldsPanel = new VisualElement();
            m_targetLabel = new Label { name = "target-label" };
            m_fieldsPanel.Add(m_targetLabel);
            m_fieldsPanel.Add(buildRow("Position", eField.Position));
            m_fieldsPanel.Add(buildRow("Rotation", eField.Rotation));
            m_fieldsPanel.Add(buildRow("Scale", eField.Scale));
            m_skewHelp = new HelpBox("부모의 비균등 스케일과 회전 때문에 찌그러진(skew) 대상이 있습니다. Scale은 입력한 값으로 맞춰지지만, 찌그러짐은 Scale로 표현되지 않아 실제 모양과 다를 수 있습니다.", HelpBoxMessageType.Warning);
            m_fieldsPanel.Add(m_skewHelp);
            rootVisualElement.Add(m_fieldsPanel);

            // 기즈모 이동처럼 이벤트가 없는 변경도 따라가도록 주기적으로 다시 읽는다.
            rootVisualElement.schedule.Execute(refreshValues).Every(REFRESH_INTERVAL_MS);
            onSelectionChanged();
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
                Debug.LogWarning($"[WorldTransform] 스타일 시트를 찾지 못했습니다: {styleSheetPath}");
        }

        private VisualElement buildRow(string label, eField field)
        {
            var row = new VisualElement();
            row.AddToClassList(ROW_CLASS);

            var rowLabel = new Label(label);
            rowLabel.AddToClassList(ROW_LABEL_CLASS);
            row.Add(rowLabel);

            var fields = new FloatField[AXIS_NAMES.Length];
            for (int axis = 0; axis < fields.Length; axis++)
            {
                int capturedAxis = axis;
                var axisField = new FloatField(AXIS_NAMES[axis].ToString()) { isDelayed = true };
                axisField.AddToClassList(AXIS_FIELD_CLASS);
                axisField.RegisterValueChangedCallback(evt => apply(field, capturedAxis, evt.newValue));
                row.Add(axisField);
                fields[axis] = axisField;
            }
            m_fields[field] = fields;
            return row;
        }

        private void onSelectionChanged()
        {
            m_targets = WorldTransformLogic.sortByDepth(Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab));
            m_eulerHints.Clear();
            refreshValues();
        }

        private void apply(eField field, int axis, float value)
        {
            m_targets.RemoveAll(t => t == null);
            if (m_targets.Count == 0)
                return;

            Undo.RecordObjects(m_targets.ToArray(), $"Set World {field}");
            foreach (var t in m_targets)
            {
                switch (field)
                {
                    case eField.Position:
                        WorldTransformLogic.setWorldPositionAxis(t, axis, value);
                        break;
                    case eField.Rotation:
                        m_eulerHints[t] = WorldTransformLogic.setWorldEulerAxis(t, currentEuler(t), axis, value);
                        break;
                    case eField.Scale:
                        WorldTransformLogic.setWorldScaleAxis(t, axis, value);
                        break;
                }
            }
            refreshValues();
        }

        private void refreshValues()
        {
            if (!isBuilt)
                return;

            m_targets.RemoveAll(t => t == null);
            bool hasTarget = m_targets.Count > 0;
            m_emptyHelp.EnableInClassList(HIDDEN_CLASS, hasTarget);
            m_fieldsPanel.EnableInClassList(HIDDEN_CLASS, !hasTarget);
            if (!hasTarget)
                return;

            m_targetLabel.text = m_targets.Count == 1 ? m_targets[0].name : $"{m_targets.Count}개 선택";
            showValues(eField.Position, m_targets.Select(t => t.position));
            showValues(eField.Rotation, m_targets.Select(currentEuler));
            showValues(eField.Scale, m_targets.Select(t => t.lossyScale));
            m_skewHelp.EnableInClassList(HIDDEN_CLASS, !m_targets.Any(WorldTransformLogic.hasSkew));
        }

        private void showValues(eField field, IEnumerable<Vector3> values)
        {
            AxisValues collected = WorldTransformLogic.collect(values.ToList());
            FloatField[] fields = m_fields[field];
            for (int axis = 0; axis < fields.Length; axis++)
            {
                // 입력 중인 칸은 덮어쓰지 않는다.
                if (isEditing(fields[axis]))
                    continue;

                bool mixed = collected.isMixed(axis);
                fields[axis].showMixedValue = mixed;
                if (!mixed)
                    fields[axis].SetValueWithoutNotify(Mathf.Round(collected.value[axis] * DISPLAY_PRECISION) / DISPLAY_PRECISION);
            }
        }

        private Vector3 currentEuler(Transform t)
        {
            Vector3? hint = m_eulerHints.TryGetValue(t, out Vector3 stored) ? stored : (Vector3?)null;
            Vector3 euler = WorldTransformLogic.resolveEuler(t.rotation, hint);
            m_eulerHints[t] = euler;
            return euler;
        }

        private static bool isEditing(VisualElement field)
        {
            var focused = field.focusController?.focusedElement as VisualElement;
            return focused != null && (focused == field || field.Contains(focused));
        }
    }
}
