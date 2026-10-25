using System.Collections.Generic;
using System.Linq;

namespace JeonLib.TableEditor
{
    /// <summary>엑셀에 그대로 붙여 넣을 수 있는 TSV 문자열을 만든다.</summary>
    internal static class TableClipboard
    {
        internal static string rows(TableDocument doc, IEnumerable<int> rows)
        {
            return string.Join("\n", rows.Select(r => string.Join("\t", doc.getCells(r))));
        }

        /// <summary>엑셀에서 같은 열의 첫 데이터 셀에 붙여 넣는 용도라 헤더는 뺀다.</summary>
        internal static string column(TableDocument doc, int column)
        {
            return string.Join("\n", Enumerable.Range(0, doc.rowCount).Select(r => doc.getCell(r, column) ?? ""));
        }

        internal static string all(TableDocument doc)
        {
            var lines = new List<string> { string.Join("\t", doc.headers) };
            if (0 < doc.rowCount)
                lines.Add(rows(doc, Enumerable.Range(0, doc.rowCount)));
            return string.Join("\n", lines);
        }

        internal static string modifiedRows(TableDocument doc)
        {
            return rows(doc, doc.modifiedRows);
        }
    }
}
