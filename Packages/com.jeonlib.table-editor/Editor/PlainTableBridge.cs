using System.Collections.Generic;
using UnityEditor;

namespace JeonLib.TableEditor
{
    /// <summary>프로젝트 브리지가 없을 때 쓰는 평문 TSV 브리지. 런타임 적용은 하지 않는다.</summary>
    internal class PlainTableBridge : ITableEditorBridge
    {
        private const string FOLDER_PREF_KEY = "JeonLib.TableEditor.Folder";

        public string tableFolder
        {
            get => EditorPrefs.GetString(FOLDER_PREF_KEY, "");
            set => EditorPrefs.SetString(FOLDER_PREF_KEY, value);
        }

        public string decode(string text) => text;

        public string encode(string plain) => plain;

        public bool canApply(string fileName, out string reason)
        {
            reason = "런타임 적용 브리지(ITableEditorBridge)가 없습니다.";
            return false;
        }

        public bool tryApplyRow(string fileName, IReadOnlyList<string> cells, out string error)
        {
            canApply(fileName, out error);
            return false;
        }
    }
}
