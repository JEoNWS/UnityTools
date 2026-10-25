using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JeonLib.TableEditor
{
    /// <summary>TSV 테이블을 보고 고쳐서 브리지를 통해 플레이 중 메모리에 반영하는 창.</summary>
    internal class TableEditorWindow : EditorWindow
    {
        private const float ROW_HEIGHT = 18f;
        private const float LIST_WIDTH = 220f;
        private const float MIN_CELL_WIDTH = 50f;
        private const float MAX_CELL_WIDTH = 220f;
        private const float CHAR_WIDTH = 7f;

        private static readonly Color s_modifiedColor = new Color(1f, 0.75f, 0.1f, 0.35f);
        private static readonly Color s_pendingColor = new Color(1f, 0.45f, 0.1f, 0.6f);
        private static readonly Color s_selectedColor = new Color(0.3f, 0.55f, 1f, 0.25f);

        // 도메인 리로드(플레이 진입) 뒤 수정 내용을 되살리기 위한 스냅샷.
        [SerializeField] private string m_filePath;
        [SerializeField] private string m_originalPlain;
        [SerializeField] private string m_currentPlain;
        [SerializeField] private string m_fileSearch = "";
        [SerializeField] private string m_search = "";
        [SerializeField] private bool m_modifiedOnly;
        [SerializeField] private bool m_autoApply;

        private TableDocument m_doc;
        private ITableEditorBridge m_bridge;
        private string[] m_files = Array.Empty<string>();
        private readonly HashSet<int> m_pendingRows = new HashSet<int>();
        private List<int> m_visibleRows = new List<int>();
        private float[] m_widths = Array.Empty<float>();
        private int m_selectedRow = -1;
        private int m_selectedColumn = -1;
        private string m_status = "";
        private Vector2 m_listScroll;
        private Vector2 m_gridScroll;


        [MenuItem("Tools/Table Editor/Open")]
        private static void open()
        {
            GetWindow<TableEditorWindow>("Table Editor");
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += onPlayModeChanged;
            m_bridge = TableEditorBridgeResolver.resolve();
            refreshFiles();

            if (!string.IsNullOrEmpty(m_filePath) && !string.IsNullOrEmpty(m_originalPlain))
                setDocument(TableDocument.restore(Path.GetFileNameWithoutExtension(m_filePath), m_originalPlain, m_currentPlain ?? m_originalPlain));
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= onPlayModeChanged;
            snapshot();
        }

        /// <summary>플레이를 시작하거나 끝내면 메모리 값이 원본이 되므로 수정한 행 전부를 다시 대기시킨다.</summary>
        private void onPlayModeChanged(PlayModeStateChange state)
        {
            if (m_doc == null || (state != PlayModeStateChange.EnteredPlayMode && state != PlayModeStateChange.EnteredEditMode))
                return;

            m_pendingRows.Clear();
            m_pendingRows.UnionWith(m_doc.modifiedRows);
            Repaint();
        }

        private void snapshot()
        {
            m_originalPlain = m_doc?.originalPlainText;
            m_currentPlain = m_doc?.toPlainText();
        }

        private void refreshFiles()
        {
            string folder = m_bridge.tableFolder;
            m_files = !string.IsNullOrEmpty(folder) && Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*.txt").Select(f => f.Replace('\\', '/')).OrderBy(f => f).ToArray()
                : Array.Empty<string>();
        }

        private void selectPlainFolder(PlainTableBridge plain)
        {
            string folder = EditorUtility.OpenFolderPanel("테이블 폴더", plain.tableFolder, "");
            if (string.IsNullOrEmpty(folder))
                return;

            plain.tableFolder = folder;
            refreshFiles();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                drawFileList();

                using (new EditorGUILayout.VerticalScope())
                {
                    if (m_doc == null)
                    {
                        EditorGUILayout.HelpBox("왼쪽에서 테이블을 고르세요.", MessageType.Info);
                        return;
                    }

                    drawToolbar();
                    drawNotice();
                    drawGrid();
                    EditorGUILayout.LabelField(m_status, EditorStyles.miniLabel);
                }
            }
        }

        // ---- 파일 목록 ----

        private void drawFileList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(LIST_WIDTH)))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    m_fileSearch = EditorGUILayout.TextField(m_fileSearch, EditorStyles.toolbarSearchField);
                    if (m_bridge is PlainTableBridge plain && GUILayout.Button("폴더", EditorStyles.toolbarButton, GUILayout.Width(36f)))
                        selectPlainFolder(plain);
                    if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(56f)))
                        refreshFiles();
                }

                if (0 == m_files.Length)
                    EditorGUILayout.HelpBox($"테이블 txt가 없습니다.\n폴더: {m_bridge.tableFolder}", MessageType.Info);

                m_listScroll = EditorGUILayout.BeginScrollView(m_listScroll);
                foreach (string file in m_files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (!string.IsNullOrEmpty(m_fileSearch) && name.IndexOf(m_fileSearch, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    bool selected = file == m_filePath;
                    string label = selected && m_doc != null && m_doc.hasChanges ? name + " *" : name;
                    if (GUILayout.Toggle(selected, label, EditorStyles.toolbarButton) && !selected)
                        trySelectFile(file);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void trySelectFile(string file)
        {
            if (m_doc != null && m_doc.hasChanges
                && !EditorUtility.DisplayDialog("Table Editor", $"{m_doc.name}의 저장하지 않은 수정 내용을 버릴까요?\n(이미 런타임에 적용한 값은 메모리에 남습니다)", "버리기", "취소"))
                return;

            try
            {
                string plain = m_bridge.decode(File.ReadAllText(file));
                m_filePath = file;
                setDocument(TableDocument.fromPlainText(Path.GetFileNameWithoutExtension(file), plain));
                m_status = $"{m_doc.rowCount:N0}행을 읽었습니다.";
            }
            catch (Exception e)
            {
                m_status = e.Message;
            }
        }

        private void setDocument(TableDocument doc)
        {
            m_doc = doc;
            m_pendingRows.Clear();
            m_pendingRows.UnionWith(doc.modifiedRows);
            m_selectedRow = -1;
            m_selectedColumn = -1;
            m_gridScroll = Vector2.zero;
            m_widths = measureWidths(doc);
            rebuildVisibleRows();
        }

        private static float[] measureWidths(TableDocument doc)
        {
            var widths = new float[doc.headers.Count];
            for (int c = 0; c < widths.Length; ++c)
            {
                int length = doc.headers[c].Length;
                for (int r = 0; r < doc.rowCount; ++r)
                    length = Mathf.Max(length, doc.getCell(r, c)?.Length ?? 0);
                widths[c] = Mathf.Clamp(length * CHAR_WIDTH + 12f, MIN_CELL_WIDTH, MAX_CELL_WIDTH);
            }
            return widths;
        }

        private void rebuildVisibleRows()
        {
            m_visibleRows = Enumerable.Range(0, m_doc.rowCount).Where(isVisible).ToList();
        }

        private bool isVisible(int row)
        {
            if (m_modifiedOnly && !m_doc.isRowModified(row))
                return false;
            if (string.IsNullOrEmpty(m_search))
                return true;

            for (int c = 0; c < m_doc.getCellCount(row); ++c)
            {
                if (0 <= m_doc.getCell(row, c).IndexOf(m_search, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // ---- 툴바 ----

        private void drawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                m_search = EditorGUILayout.TextField(m_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120f));
                m_modifiedOnly = GUILayout.Toggle(m_modifiedOnly, "수정한 행만", EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck())
                    rebuildVisibleRows();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("복사", EditorStyles.toolbarDropDown))
                    showCopyMenu();

                using (new EditorGUI.DisabledScope(!m_doc.hasChanges))
                {
                    if (GUILayout.Button("되돌리기", EditorStyles.toolbarButton))
                        revert();
                    if (GUILayout.Button("txt 저장", EditorStyles.toolbarButton))
                        save();
                }

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    m_autoApply = GUILayout.Toggle(m_autoApply, "수정 즉시 적용", EditorStyles.toolbarButton);
                    using (new EditorGUI.DisabledScope(0 == m_pendingRows.Count))
                    {
                        if (GUILayout.Button($"런타임 적용 ({m_pendingRows.Count})", EditorStyles.toolbarButton))
                            applyPending();
                    }
                }
            }
        }

        private void drawNotice()
        {
            EditorGUILayout.HelpBox(
                "임시 튜닝용: 원본에서 다시 익스포트하면 txt가 덮어써지고, 플레이를 끝내면 메모리 값은 사라집니다. 확정 값은 '복사'로 엑셀에 옮기세요.\n" +
                "분류/키 컬럼 변경, 이미 복사·계산된 값, 리스트 필드는 즉시 반영되지 않습니다(txt 저장 후 재시작).",
                MessageType.None);
        }

        private void showCopyMenu()
        {
            var menu = new GenericMenu();
            addCopyItem(menu, "선택한 행", 0 <= m_selectedRow, () => TableClipboard.rows(m_doc, new[] { m_selectedRow }));
            addCopyItem(menu, "선택한 열 (헤더 제외)", 0 < m_selectedColumn, () => TableClipboard.column(m_doc, m_selectedColumn));
            addCopyItem(menu, "수정한 행", m_doc.hasChanges, () => TableClipboard.modifiedRows(m_doc));
            addCopyItem(menu, "전체 (헤더 포함)", true, () => TableClipboard.all(m_doc));
            menu.ShowAsContext();
        }

        private void addCopyItem(GenericMenu menu, string label, bool enabled, Func<string> build)
        {
            if (!enabled)
            {
                menu.AddDisabledItem(new GUIContent(label));
                return;
            }

            menu.AddItem(new GUIContent(label), false, () =>
            {
                EditorGUIUtility.systemCopyBuffer = build();
                m_status = $"{label}을(를) 클립보드에 복사했습니다.";
            });
        }

        private void revert()
        {
            // 되돌린 행도 메모리에는 수정값이 남아 있을 수 있어 다시 대기시킨다.
            m_pendingRows.UnionWith(m_doc.modifiedRows);
            m_doc.revert();
            rebuildVisibleRows();
            m_status = "원본 값으로 되돌렸습니다.";

            if (m_autoApply && Application.isPlaying)
                applyPending();
        }

        private void save()
        {
            if (!EditorUtility.DisplayDialog("Table Editor", $"{m_doc.name}.txt를 덮어씁니다.\n원본(엑셀 등)에서 다시 익스포트하면 이 수정은 사라집니다.", "저장", "취소"))
                return;

            File.WriteAllText(m_filePath, m_bridge.encode(m_doc.toPlainText()));
            if (m_filePath.StartsWith("Assets/", StringComparison.Ordinal))
                AssetDatabase.ImportAsset(m_filePath);
            m_doc.markSaved();
            rebuildVisibleRows();
            m_status = $"{m_doc.name}.txt에 저장했습니다.";
        }

        private void applyPending()
        {
            if (!m_bridge.canApply(m_doc.name, out string reason))
            {
                m_status = reason;
                return;
            }

            // 한 행이 실패해도 나머지 행은 계속 적용한다.
            var rows = m_pendingRows.OrderBy(r => r).ToList();
            int applied = 0;
            foreach (int row in rows)
            {
                if (m_bridge.tryApplyRow(m_doc.name, m_doc.getCells(row), out string error))
                {
                    m_pendingRows.Remove(row);
                    ++applied;
                }
                else
                {
                    Debug.LogWarning($"[Table Editor] {m_doc.name} id {m_doc.getCell(row, 0)} 적용 실패: {error}");
                }
            }

            m_status = applied == rows.Count ? $"{applied}행을 적용했습니다." : $"{applied}/{rows.Count}행 적용, 실패 내용은 콘솔을 보세요.";
        }

        // ---- 그리드 ----

        private void drawGrid()
        {
            var area = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var headerRect = new Rect(area.x, area.y, area.width, ROW_HEIGHT);
            var bodyRect = new Rect(area.x, area.y + ROW_HEIGHT, area.width, area.height - ROW_HEIGHT);
            var content = new Rect(0f, 0f, m_widths.Sum(), m_visibleRows.Count * ROW_HEIGHT);

            m_gridScroll = GUI.BeginScrollView(bodyRect, m_gridScroll, content);
            int first = Mathf.Max(0, (int)(m_gridScroll.y / ROW_HEIGHT));
            int last = Mathf.Min(m_visibleRows.Count, first + (int)(bodyRect.height / ROW_HEIGHT) + 2);
            for (int v = first; v < last; ++v)
                drawRow(m_visibleRows[v], v * ROW_HEIGHT);
            GUI.EndScrollView();

            GUI.BeginClip(headerRect);
            drawHeader();
            GUI.EndClip();
        }

        private void drawHeader()
        {
            float x = -m_gridScroll.x;
            for (int c = 0; c < m_widths.Length; ++c)
            {
                var rect = new Rect(x, 0f, m_widths[c], ROW_HEIGHT);
                if (c == m_selectedColumn)
                    EditorGUI.DrawRect(rect, s_selectedColor);
                if (GUI.Button(rect, new GUIContent(m_doc.headers[c], $"{c}열: {m_doc.headers[c]}"), EditorStyles.toolbarButton))
                    m_selectedColumn = c == m_selectedColumn ? -1 : c;
                x += m_widths[c];
            }
        }

        private void drawRow(int row, float y)
        {
            float x = 0f;
            for (int c = 0; c < m_widths.Length; ++c)
            {
                var rect = new Rect(x, y, m_widths[c], ROW_HEIGHT);
                x += m_widths[c];

                if (row == m_selectedRow || c == m_selectedColumn)
                    EditorGUI.DrawRect(rect, s_selectedColor);

                string value = m_doc.getCell(row, c);
                if (value == null)
                    continue;

                if (0 == c)
                {
                    drawIdCell(rect, row, value);
                    continue;
                }

                if (m_doc.isModified(row, c))
                    EditorGUI.DrawRect(rect, s_modifiedColor);

                string edited = EditorGUI.DelayedTextField(rect, value);
                if (edited != value)
                    onCellEdited(row, c, edited);
            }
        }

        private void drawIdCell(Rect rect, int row, string id)
        {
            if (m_pendingRows.Contains(row) && Application.isPlaying)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), s_pendingColor);

            if (GUI.Button(rect, id, EditorStyles.label))
                m_selectedRow = row == m_selectedRow ? -1 : row;
        }

        private void onCellEdited(int row, int column, string value)
        {
            if (!m_doc.setCell(row, column, value))
            {
                m_status = "탭이나 줄바꿈은 넣을 수 없습니다.";
                return;
            }

            m_pendingRows.Add(row);
            m_status = $"id {m_doc.getCell(row, 0)} {m_doc.headers[column]} = {value}";

            if (m_autoApply && Application.isPlaying)
                applyPending();
        }
    }
}
