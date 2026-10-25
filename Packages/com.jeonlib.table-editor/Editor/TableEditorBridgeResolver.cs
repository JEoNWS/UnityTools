using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace JeonLib.TableEditor
{
    /// <summary>프로젝트에 구현된 ITableEditorBridge를 찾고, 없으면 평문 브리지를 쓴다.</summary>
    internal static class TableEditorBridgeResolver
    {
        /// <summary>테스트 어셈블리의 가짜 브리지는 고르지 않는다.</summary>
        internal static ITableEditorBridge resolve()
        {
            return resolve(TypeCache.GetTypesDerivedFrom<ITableEditorBridge>()
                .Where(t => !t.Assembly.GetName().Name.EndsWith(".Tests", StringComparison.Ordinal)));
        }

        internal static ITableEditorBridge resolve(IEnumerable<Type> candidates)
        {
            var type = candidates.FirstOrDefault(t => !t.IsAbstract && !t.IsInterface && t != typeof(PlainTableBridge));
            return type != null ? (ITableEditorBridge)Activator.CreateInstance(type, true) : new PlainTableBridge();
        }
    }
}
