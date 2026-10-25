using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace JeonLib.WorldTransform.Tests
{
    /// <summary>창에서 분리한 월드 Transform 계산(읽기, 축 적용, skew, 다중 선택)을 검증한다.</summary>
    internal class WorldTransformLogicTests
    {
        private const float TOLERANCE = 1e-4f;

        private readonly List<GameObject> m_created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in m_created)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            m_created.Clear();
        }

        private Transform create(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            m_created.Add(go);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void assertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(TOLERANCE), $"actual {actual:F5}, expected {expected:F5}");
        }

        // ---- withAxis ----

        [Test]
        public void 지정한_축만_바꾼_벡터를_돌려줌()
        {
            assertVector(WorldTransformLogic.withAxis(new Vector3(1, 2, 3), 1, 9), new Vector3(1, 9, 3));
        }

        // ---- Position ----

        [Test]
        public void 회전_스케일된_부모_아래에서_월드_위치_축_설정()
        {
            var parent = create("Parent");
            parent.SetPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(0, 90, 0));
            parent.localScale = new Vector3(2, 2, 2);
            var child = create("Child", parent);
            child.localPosition = new Vector3(1, 0, 0);
            Vector3 before = child.position;

            WorldTransformLogic.setWorldPositionAxis(child, 0, 10);

            assertVector(child.position, new Vector3(10, before.y, before.z));
        }

        // ---- Rotation ----

        [Test]
        public void 회전된_부모_아래에서_월드_회전_축_설정()
        {
            var parent = create("Parent");
            parent.rotation = Quaternion.Euler(0, 45, 0);
            var child = create("Child", parent);
            child.localRotation = Quaternion.Euler(10, 0, 0);
            Vector3 current = WorldTransformLogic.resolveEuler(child.rotation, null);

            Vector3 result = WorldTransformLogic.setWorldEulerAxis(child, current, 0, 30);

            Assert.That(result.x, Is.EqualTo(30).Within(TOLERANCE));
            Assert.That(result.y, Is.EqualTo(current.y).Within(TOLERANCE));
            Assert.That(result.z, Is.EqualTo(current.z).Within(TOLERANCE));
            Assert.That(Quaternion.Angle(child.rotation, Quaternion.Euler(result)), Is.LessThan(0.1f));
        }

        [Test]
        public void 같은_회전을_가리키는_힌트가_있으면_힌트를_표시()
        {
            var hint = new Vector3(100, 0, 0);

            Vector3 result = WorldTransformLogic.resolveEuler(Quaternion.Euler(hint), hint);

            assertVector(result, hint);
        }

        [Test]
        public void 힌트가_없으면_eulerAngles를_표시()
        {
            Quaternion rotation = Quaternion.Euler(0, -30, 0);

            Vector3 result = WorldTransformLogic.resolveEuler(rotation, null);

            assertVector(result, rotation.eulerAngles);
        }

        [Test]
        public void 회전이_크게_다르면_기본_표현을_표시()
        {
            Quaternion rotation = Quaternion.Euler(0, 30, 0);

            Vector3 result = WorldTransformLogic.resolveEuler(rotation, new Vector3(100, 0, 0));

            assertVector(result, rotation.eulerAngles);
        }

        [Test]
        public void 음수_힌트에서_조금_돌면_음수로_이어서_표시()
        {
            var expected = new Vector3(0, -30.5f, 0);

            Vector3 result = WorldTransformLogic.resolveEuler(Quaternion.Euler(expected), new Vector3(0, -30, 0));

            assertVector(result, expected);
        }

        [Test]
        public void X가_90을_넘는_힌트에서_조금_돌면_같은_형태로_이어서_표시()
        {
            var expected = new Vector3(100.5f, 0, 0);

            Vector3 result = WorldTransformLogic.resolveEuler(Quaternion.Euler(expected), new Vector3(100, 0, 0));

            assertVector(result, expected);
        }

        [Test]
        public void 한_바퀴_넘게_감긴_힌트에서_조금_돌면_감긴_값으로_이어서_표시()
        {
            var expected = new Vector3(0, 371, 0);

            Vector3 result = WorldTransformLogic.resolveEuler(Quaternion.Euler(expected), new Vector3(0, 370, 0));

            assertVector(result, expected);
        }

        [Test]
        public void 힌트로_고른_euler는_항상_같은_회전()
        {
            Quaternion rotation = Quaternion.Euler(35, 120, -60);

            Vector3 result = WorldTransformLogic.resolveEuler(rotation, new Vector3(140, -50, 100));

            Assert.That(Quaternion.Angle(Quaternion.Euler(result), rotation), Is.LessThan(0.1f));
        }

        // ---- Scale ----

        [Test]
        public void 균등_스케일_부모에서_월드_스케일_정확히_설정()
        {
            var parent = create("Parent");
            parent.rotation = Quaternion.Euler(10, 20, 30);
            parent.localScale = new Vector3(2, 2, 2);
            var child = create("Child", parent);
            child.localRotation = Quaternion.Euler(5, 40, 0);

            bool applied = WorldTransformLogic.setWorldScaleAxis(child, 1, 3);

            Assert.That(applied, Is.True);
            assertVector(child.lossyScale, new Vector3(2, 3, 2));
        }

        [Test]
        public void 축이_나란한_비균등_부모에서_월드_스케일_정확히_설정()
        {
            var parent = create("Parent");
            parent.localScale = new Vector3(2, 1, 1);
            var child = create("Child", parent);
            child.localRotation = Quaternion.Euler(0, 0, 90);

            WorldTransformLogic.setWorldScaleAxis(child, 0, 4);

            // 자식 X축은 부모 Y축(배율 1)과 나란하다.
            Assert.That(child.localScale.x, Is.EqualTo(4).Within(TOLERANCE));
            Assert.That(child.lossyScale.x, Is.EqualTo(4).Within(TOLERANCE));
        }

        [Test]
        public void skew_상태에서도_표시되는_월드_스케일은_입력값과_같음()
        {
            var parent = create("Parent");
            parent.localScale = new Vector3(2, 1, 1);
            var child = create("Child", parent);
            child.localRotation = Quaternion.Euler(0, 0, 45);
            float beforeY = child.lossyScale.y;

            WorldTransformLogic.setWorldScaleAxis(child, 0, 3);

            Assert.That(child.lossyScale.x, Is.EqualTo(3).Within(TOLERANCE));
            Assert.That(child.lossyScale.y, Is.EqualTo(beforeY).Within(TOLERANCE));
        }

        [Test]
        public void 로컬_스케일이_0이어도_부모_배율로_계산()
        {
            var parent = create("Parent");
            parent.localScale = new Vector3(2, 2, 2);
            var child = create("Child", parent);
            child.localScale = new Vector3(0, 1, 1);

            bool applied = WorldTransformLogic.setWorldScaleAxis(child, 0, 4);

            Assert.That(applied, Is.True);
            Assert.That(child.localScale.x, Is.EqualTo(2).Within(TOLERANCE));
        }

        [Test]
        public void 부모_스케일이_0이면_적용하지_않음()
        {
            var parent = create("Parent");
            parent.localScale = new Vector3(0, 1, 1);
            var child = create("Child", parent);

            bool applied = WorldTransformLogic.setWorldScaleAxis(child, 0, 5);

            Assert.That(applied, Is.False);
            assertVector(child.localScale, Vector3.one);
        }

        [Test]
        public void 부모가_없으면_로컬_스케일에_그대로_설정()
        {
            var root = create("Root");
            root.localScale = new Vector3(0, 1, 1);

            WorldTransformLogic.setWorldScaleAxis(root, 0, 3);

            assertVector(root.localScale, new Vector3(3, 1, 1));
        }

        // ---- hasSkew ----

        [Test]
        public void 비균등_부모_아래_비스듬히_회전하면_skew()
        {
            var parent = create("Parent");
            parent.localScale = new Vector3(2, 1, 1);
            var child = create("Child", parent);
            child.localRotation = Quaternion.Euler(0, 0, 45);

            Assert.That(WorldTransformLogic.hasSkew(child), Is.True);
        }

        [Test]
        public void 비균등_부모라도_축이_나란하면_skew_아님()
        {
            var parent = create("Parent");
            parent.localScale = new Vector3(2, 1, 1);
            var child = create("Child", parent);
            child.localRotation = Quaternion.Euler(0, 0, 90);

            Assert.That(WorldTransformLogic.hasSkew(child), Is.False);
        }

        [Test]
        public void 균등_부모_아래_회전은_skew_아님()
        {
            var parent = create("Parent");
            parent.localScale = new Vector3(3, 3, 3);
            var child = create("Child", parent);
            child.localRotation = Quaternion.Euler(30, 45, 60);
            child.localScale = new Vector3(1, 2, 3);

            Assert.That(WorldTransformLogic.hasSkew(child), Is.False);
        }

        [Test]
        public void 스케일이_0인_축은_skew_판정에서_제외()
        {
            var parent = create("Parent");
            parent.localScale = new Vector3(0, 1, 1);
            var child = create("Child", parent);

            Assert.That(WorldTransformLogic.hasSkew(child), Is.False);
        }

        // ---- collect ----

        [Test]
        public void 축별로_값이_다르면_mixed()
        {
            var values = new[] { new Vector3(1, 2, 3), new Vector3(1, 5, 3) };

            AxisValues result = WorldTransformLogic.collect(values);

            Assert.That(result.isMixed(0), Is.False);
            Assert.That(result.isMixed(1), Is.True);
            Assert.That(result.isMixed(2), Is.False);
            assertVector(result.value, new Vector3(1, 2, 3));
        }

        [Test]
        public void 오차_범위_안의_차이는_mixed_아님()
        {
            var values = new[] { new Vector3(1, 1, 1), new Vector3(1.000001f, 1, 1) };

            AxisValues result = WorldTransformLogic.collect(values);

            Assert.That(result.isMixed(0), Is.False);
        }

        // ---- sortByDepth ----

        [Test]
        public void 부모가_자식보다_먼저_오도록_정렬()
        {
            var parent = create("Parent");
            var child = create("Child", parent);
            var grandChild = create("GrandChild", child);

            List<Transform> sorted = WorldTransformLogic.sortByDepth(new[] { grandChild, parent, child });

            Assert.That(sorted, Is.EqualTo(new[] { parent, child, grandChild }));
        }

        [Test]
        public void 부모와_자식을_함께_선택해도_둘_다_목표_위치로()
        {
            var parent = create("Parent");
            var child = create("Child", parent);
            child.localPosition = new Vector3(3, 0, 0);

            foreach (var t in WorldTransformLogic.sortByDepth(new[] { child, parent }))
                WorldTransformLogic.setWorldPositionAxis(t, 0, 5);

            Assert.That(parent.position.x, Is.EqualTo(5).Within(TOLERANCE));
            Assert.That(child.position.x, Is.EqualTo(5).Within(TOLERANCE));
        }
    }
}
