using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace JeonLib.TableEditor.Tests
{
    /// <summary>프로젝트 브리지 선택과 평문 기본 브리지 동작을 검증한다.</summary>
    internal class TableEditorBridgeResolverTests
    {
        private class ProjectBridge : ITableEditorBridge
        {
            public string tableFolder => "Assets/Table";
            public string decode(string text) => text;
            public string encode(string plain) => plain;
            public bool canApply(string fileName, out string reason) { reason = null; return true; }
            public bool tryApplyRow(string fileName, IReadOnlyList<string> cells, out string error) { error = null; return true; }
        }

        private abstract class AbstractBridge : ProjectBridge { }

        [Test]
        public void 프로젝트_브리지가_있으면_그걸_만든다()
        {
            var bridge = TableEditorBridgeResolver.resolve(new[] { typeof(AbstractBridge), typeof(PlainTableBridge), typeof(ProjectBridge) });

            Assert.That(bridge, Is.TypeOf<ProjectBridge>());
        }

        [Test]
        public void 프로젝트_브리지가_없으면_평문_브리지()
        {
            var bridge = TableEditorBridgeResolver.resolve(Array.Empty<Type>());

            Assert.That(bridge, Is.TypeOf<PlainTableBridge>());
        }

        [Test]
        public void 평문_브리지는_그대로_읽고_쓰고_적용은_못함()
        {
            var bridge = new PlainTableBridge();

            Assert.That(bridge.decode("a\tb"), Is.EqualTo("a\tb"));
            Assert.That(bridge.encode("a\tb"), Is.EqualTo("a\tb"));
            Assert.That(bridge.canApply("Any", out string reason), Is.False);
            Assert.That(reason, Is.Not.Empty);
        }
    }
}
