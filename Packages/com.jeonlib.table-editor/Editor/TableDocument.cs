using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JeonLib.TableEditor
{
    /// <summary>TSV 테이블 한 개의 헤더, 셀, 수정 상태.</summary>
    internal class TableDocument
    {
        private const string NEW_LINE = "\r\n";
        private static readonly char[] s_lineSeparators = { '\r', '\n' };

        private readonly string[] m_headers;
        private readonly List<string[]> m_original;
        private readonly List<string[]> m_cells;

        internal string name { get; }
        internal IReadOnlyList<string> headers => m_headers;
        internal int rowCount => m_cells.Count;
        internal bool hasChanges => modifiedRows.Any();
        internal IEnumerable<int> modifiedRows => Enumerable.Range(0, rowCount).Where(isRowModified);

        private TableDocument(string name, string[] headers, List<string[]> cells)
        {
            this.name = name;
            m_headers = headers;
            m_cells = cells;
            m_original = cells.Select(c => (string[])c.Clone()).ToList();
        }

        internal static TableDocument fromPlainText(string name, string plain)
        {
            var lines = plain.Split(s_lineSeparators, StringSplitOptions.RemoveEmptyEntries);
            var headers = 0 < lines.Length ? lines[0].Split('\t') : Array.Empty<string>();
            var cells = lines.Skip(1).Select(l => l.Split('\t')).ToList();
            return new TableDocument(name, headers, cells);
        }

        /// <summary>원본과 행/셀 수가 같을 때만 현재 값을 덮어쓴다.</summary>
        internal static TableDocument restore(string name, string originalPlain, string currentPlain)
        {
            var doc = fromPlainText(name, originalPlain);
            var current = fromPlainText(name, currentPlain);
            if (current.rowCount != doc.rowCount)
                return doc;

            for (int r = 0; r < doc.rowCount; ++r)
            {
                if (current.getCellCount(r) != doc.getCellCount(r))
                    return fromPlainText(name, originalPlain);

                doc.m_cells[r] = current.m_cells[r];
            }
            return doc;
        }

        internal string toPlainText() => join(m_cells);

        internal string originalPlainText => join(m_original);

        /// <summary>ExcelExporter와 같은 형식(줄마다 \r\n)으로 만든다.</summary>
        private string join(List<string[]> rows)
        {
            var sb = new StringBuilder();
            sb.Append(string.Join("\t", m_headers)).Append(NEW_LINE);
            foreach (var row in rows)
                sb.Append(string.Join("\t", row)).Append(NEW_LINE);
            return sb.ToString();
        }

        internal int getCellCount(int row) => m_cells[row].Length;

        internal string getCell(int row, int column)
        {
            var cells = m_cells[row];
            return column < cells.Length ? cells[column] : null;
        }

        internal List<string> getCells(int row) => new List<string>(m_cells[row]);

        /// <summary>id 열, 없는 셀, 탭/줄바꿈이 들어간 값은 거부한다.</summary>
        internal bool setCell(int row, int column, string value)
        {
            if (row < 0 || rowCount <= row || column <= 0 || getCellCount(row) <= column)
                return false;
            if (value == null || 0 <= value.IndexOfAny(new[] { '\t', '\r', '\n' }))
                return false;

            m_cells[row][column] = value;
            return true;
        }

        internal bool isModified(int row, int column)
        {
            return column < getCellCount(row) && m_cells[row][column] != m_original[row][column];
        }

        internal bool isRowModified(int row)
        {
            for (int c = 0; c < getCellCount(row); ++c)
            {
                if (isModified(row, c))
                    return true;
            }
            return false;
        }

        internal void revert()
        {
            for (int r = 0; r < rowCount; ++r)
                m_cells[r] = (string[])m_original[r].Clone();
        }

        internal void markSaved()
        {
            for (int r = 0; r < rowCount; ++r)
                m_original[r] = (string[])m_cells[r].Clone();
        }
    }
}
