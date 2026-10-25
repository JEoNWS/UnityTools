using NUnit.Framework;

namespace JeonLib.TableEditor.Tests
{
    /// <summary>엑셀에 붙여 넣을 TSV 문자열 형식을 검증한다.</summary>
    internal class TableClipboardTests
    {
        private static TableDocument createDocument()
        {
            return TableDocument.fromPlainText("TestTable", "id\tspeed\tname\r\n1\t10\tA\r\n2\t20\tB\r\n3\t30\r\n");
        }

        [Test]
        public void 행_복사는_셀을_탭으로_행을_줄바꿈으로_이음()
        {
            var doc = createDocument();

            Assert.That(TableClipboard.rows(doc, new[] { 0, 2 }), Is.EqualTo("1\t10\tA\n3\t30"));
        }

        [Test]
        public void 열_복사는_헤더_없이_모든_행의_값을_세로로_이음()
        {
            var doc = createDocument();

            Assert.That(TableClipboard.column(doc, 1), Is.EqualTo("10\n20\n30"));
        }

        [Test]
        public void 열_복사에서_셀이_없는_행은_빈_값()
        {
            var doc = createDocument();

            Assert.That(TableClipboard.column(doc, 2), Is.EqualTo("A\nB\n"));
        }

        [Test]
        public void 전체_복사는_헤더를_포함()
        {
            var doc = createDocument();

            Assert.That(TableClipboard.all(doc), Is.EqualTo("id\tspeed\tname\n1\t10\tA\n2\t20\tB\n3\t30"));
        }

        [Test]
        public void 수정한_행_복사는_수정값으로_수정한_행만()
        {
            var doc = createDocument();
            doc.setCell(1, 1, "25");

            Assert.That(TableClipboard.modifiedRows(doc), Is.EqualTo("2\t25\tB"));
        }

        [Test]
        public void 수정한_행이_없으면_빈_문자열()
        {
            Assert.That(TableClipboard.modifiedRows(createDocument()), Is.Empty);
        }
    }
}
