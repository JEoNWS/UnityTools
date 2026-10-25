using System.Collections.Generic;

namespace JeonLib.TableEditor
{
    /// <summary>프로젝트마다 다른 테이블 위치, 암호화, 런타임 적용을 Table Editor에 연결한다.</summary>
    public interface ITableEditorBridge
    {
        string tableFolder { get; }

        /// <summary>파일 내용을 TSV 평문으로 바꾼다. 실패하면 예외를 던진다.</summary>
        string decode(string text);

        string encode(string plain);

        /// <summary>지금 이 파일의 행을 메모리에 적용할 수 있는지. 못하면 이유를 돌려준다.</summary>
        bool canApply(string fileName, out string reason);

        /// <summary>cells[0]은 id. 메모리의 해당 row에 값을 반영한다.</summary>
        bool tryApplyRow(string fileName, IReadOnlyList<string> cells, out string error);
    }
}
