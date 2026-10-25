using NUnit.Framework;

namespace JeonLib.TableEditor.Tests
{
    /// <summary>TSV 평문 파싱, 셀 수정, 저장 형식을 검증한다.</summary>
    internal class TableDocumentTests
    {
        // ExcelExporter와 같은 형식: 헤더 + 행, AppendLine이라 줄마다 \r\n.
        private const string PLAIN = "id\tspeed\tname\r\n1\t10\tA\r\n2\t20\tB\r\n3\t30\r\n";

        private static TableDocument createDocument()
        {
            return TableDocument.fromPlainText("TestTable", PLAIN);
        }

        // ---- 파싱 ----

        [Test]
        public void 첫_줄은_헤더_나머지는_행으로_나눔()
        {
            var doc = createDocument();

            Assert.That(doc.headers, Is.EqualTo(new[] { "id", "speed", "name" }));
            Assert.That(doc.rowCount, Is.EqualTo(3));
            Assert.That(doc.getCell(1, 1), Is.EqualTo("20"));
        }

        [Test]
        public void 셀이_모자란_행은_없는_셀을_null로_돌려줌()
        {
            var doc = createDocument();

            Assert.That(doc.getCellCount(2), Is.EqualTo(2));
            Assert.That(doc.getCell(2, 2), Is.Null);
        }

        [Test]
        public void 빈_줄은_행으로_치지_않음()
        {
            var doc = TableDocument.fromPlainText("TestTable", "id\tv\n\n1\t10\n\n");

            Assert.That(doc.rowCount, Is.EqualTo(1));
        }

        [Test]
        public void 수정하지_않으면_읽은_평문_그대로_저장()
        {
            Assert.That(createDocument().toPlainText(), Is.EqualTo(PLAIN));
        }

        // ---- 수정 ----

        [Test]
        public void 셀을_수정하면_값과_변경_표시가_바뀜()
        {
            var doc = createDocument();

            Assert.That(doc.setCell(0, 1, "15"), Is.True);

            Assert.That(doc.getCell(0, 1), Is.EqualTo("15"));
            Assert.That(doc.isModified(0, 1), Is.True);
            Assert.That(doc.isModified(0, 2), Is.False);
            Assert.That(doc.isRowModified(0), Is.True);
            Assert.That(doc.isRowModified(1), Is.False);
            Assert.That(doc.hasChanges, Is.True);
        }

        [Test]
        public void 원래_값으로_되돌려_쓰면_변경_표시가_사라짐()
        {
            var doc = createDocument();
            doc.setCell(0, 1, "15");

            doc.setCell(0, 1, "10");

            Assert.That(doc.isModified(0, 1), Is.False);
            Assert.That(doc.hasChanges, Is.False);
        }

        [Test]
        public void id_열은_수정할_수_없음()
        {
            var doc = createDocument();

            Assert.That(doc.setCell(0, 0, "99"), Is.False);
            Assert.That(doc.getCell(0, 0), Is.EqualTo("1"));
        }

        [Test]
        public void 없는_셀은_수정할_수_없음()
        {
            var doc = createDocument();

            Assert.That(doc.setCell(2, 2, "C"), Is.False);
            Assert.That(doc.setCell(9, 1, "1"), Is.False);
        }

        [Test]
        public void 탭이나_줄바꿈이_들어간_값은_거부()
        {
            var doc = createDocument();

            Assert.That(doc.setCell(0, 2, "A\tB"), Is.False);
            Assert.That(doc.setCell(0, 2, "A\nB"), Is.False);
            Assert.That(doc.getCell(0, 2), Is.EqualTo("A"));
        }

        [Test]
        public void 수정한_행_목록을_순서대로_돌려줌()
        {
            var doc = createDocument();
            doc.setCell(2, 1, "31");
            doc.setCell(0, 1, "11");

            Assert.That(doc.modifiedRows, Is.EqualTo(new[] { 0, 2 }));
        }

        [Test]
        public void getCells는_수정된_값을_복사본으로_돌려줌()
        {
            var doc = createDocument();
            doc.setCell(0, 1, "15");

            var cells = doc.getCells(0);
            cells[1] = "99";

            Assert.That(doc.getCells(0), Is.EqualTo(new[] { "1", "15", "A" }));
        }

        [Test]
        public void 수정값이_저장_평문에_반영됨()
        {
            var doc = createDocument();
            doc.setCell(1, 2, "Z");

            Assert.That(doc.toPlainText(), Is.EqualTo("id\tspeed\tname\r\n1\t10\tA\r\n2\t20\tZ\r\n3\t30\r\n"));
        }

        [Test]
        public void 되돌리기는_원본_값으로_복구()
        {
            var doc = createDocument();
            doc.setCell(0, 1, "15");

            doc.revert();

            Assert.That(doc.getCell(0, 1), Is.EqualTo("10"));
            Assert.That(doc.hasChanges, Is.False);
        }

        [Test]
        public void 저장_표시_후에는_현재_값이_새_원본이_됨()
        {
            var doc = createDocument();
            doc.setCell(0, 1, "15");

            doc.markSaved();
            doc.revert();

            Assert.That(doc.getCell(0, 1), Is.EqualTo("15"));
            Assert.That(doc.hasChanges, Is.False);
        }

        // ---- 도메인 리로드 복원 ----

        [Test]
        public void 원본과_현재_평문으로_수정_상태까지_복원()
        {
            var doc = createDocument();
            doc.setCell(1, 1, "25");

            var restored = TableDocument.restore("TestTable", doc.originalPlainText, doc.toPlainText());

            Assert.That(restored.getCell(1, 1), Is.EqualTo("25"));
            Assert.That(restored.isModified(1, 1), Is.True);
            Assert.That(restored.modifiedRows, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void 현재_평문의_모양이_다르면_원본만_복원()
        {
            var restored = TableDocument.restore("TestTable", PLAIN, "id\tspeed\tname\r\n1\t10\tA\r\n");

            Assert.That(restored.rowCount, Is.EqualTo(3));
            Assert.That(restored.hasChanges, Is.False);
        }
    }
}
