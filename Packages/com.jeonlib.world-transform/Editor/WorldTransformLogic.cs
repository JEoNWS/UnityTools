using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JeonLib.WorldTransform
{
    /// <summary>여러 대상의 축별 값과 축마다 값이 갈리는지 여부.</summary>
    internal readonly struct AxisValues
    {
        public Vector3 value { get; }
        private readonly bool m_mixedX;
        private readonly bool m_mixedY;
        private readonly bool m_mixedZ;

        public AxisValues(Vector3 value, bool mixedX, bool mixedY, bool mixedZ)
        {
            this.value = value;
            m_mixedX = mixedX;
            m_mixedY = mixedY;
            m_mixedZ = mixedZ;
        }

        public bool isMixed(int axis)
        {
            switch (axis)
            {
                case 0: return m_mixedX;
                case 1: return m_mixedY;
                default: return m_mixedZ;
            }
        }
    }

    /// <summary>월드 기준 위치, 회전, 스케일을 읽고 축 하나씩 적용하는 계산.</summary>
    internal static class WorldTransformLogic
    {
        private const float MIXED_TOLERANCE = 1e-4f;
        private const float HINT_ANGLE_TOLERANCE = 0.01f;
        private const float SKEW_TOLERANCE = 1e-4f;
        private const float ZERO_TOLERANCE = 1e-8f;

        public static Vector3 withAxis(Vector3 v, int axis, float value)
        {
            v[axis] = value;
            return v;
        }

        public static void setWorldPositionAxis(Transform t, int axis, float value)
        {
            t.position = withAxis(t.position, axis, value);
        }

        /// <summary>같은 회전을 나타내는 euler 중 힌트와 가장 가까운 것을 돌려준다.</summary>
        public static Vector3 resolveEuler(Quaternion rotation, Vector3? hint)
        {
            Vector3 euler = rotation.eulerAngles;
            if (!hint.HasValue)
                return euler;
            if (Quaternion.Angle(Quaternion.Euler(hint.Value), rotation) < HINT_ANGLE_TOLERANCE)
                return hint.Value;

            // (x, y, z)와 (180 - x, y + 180, z + 180)은 같은 회전이다.
            Vector3 direct = nearestTo(hint.Value, euler);
            Vector3 flipped = nearestTo(hint.Value, new Vector3(180f - euler.x, euler.y + 180f, euler.z + 180f));
            return distance(hint.Value, direct) <= distance(hint.Value, flipped) ? direct : flipped;
        }

        /// <summary>현재 euler에서 한 축만 바꿔 월드 회전에 적용하고, 적용한 euler를 돌려준다.</summary>
        public static Vector3 setWorldEulerAxis(Transform t, Vector3 currentEuler, int axis, float value)
        {
            Vector3 euler = withAxis(currentEuler, axis, value);
            t.rotation = Quaternion.Euler(euler);
            return euler;
        }

        /// <summary>월드 스케일 한 축을 맞추도록 localScale을 바꾼다. 부모 배율이 0이면 false.</summary>
        public static bool setWorldScaleAxis(Transform t, int axis, float value)
        {
            float stretch = parentStretch(t, axis);
            if (Mathf.Abs(stretch) < ZERO_TOLERANCE)
                return false;

            t.localScale = withAxis(t.localScale, axis, value / stretch);
            return true;
        }

        /// <summary>월드 행렬의 축끼리 직교하지 않으면 skew로 본다.</summary>
        public static bool hasSkew(Transform t)
        {
            Matrix4x4 m = t.localToWorldMatrix;
            Vector3 x = m.GetColumn(0);
            Vector3 y = m.GetColumn(1);
            Vector3 z = m.GetColumn(2);
            return isSkewed(x, y) || isSkewed(y, z) || isSkewed(z, x);
        }

        /// <summary>첫 값을 대표값으로 쓰고 축마다 다른 값이 있는지 모은다.</summary>
        public static AxisValues collect(IReadOnlyList<Vector3> values)
        {
            if (values.Count == 0)
                return new AxisValues(Vector3.zero, false, false, false);

            Vector3 first = values[0];
            bool mixedX = false, mixedY = false, mixedZ = false;
            for (int i = 1; i < values.Count; i++)
            {
                mixedX |= Mathf.Abs(values[i].x - first.x) > MIXED_TOLERANCE;
                mixedY |= Mathf.Abs(values[i].y - first.y) > MIXED_TOLERANCE;
                mixedZ |= Mathf.Abs(values[i].z - first.z) > MIXED_TOLERANCE;
            }
            return new AxisValues(first, mixedX, mixedY, mixedZ);
        }

        /// <summary>부모를 먼저 적용해야 자식의 월드 값이 어긋나지 않으므로 깊이 순으로 정렬한다.</summary>
        public static List<Transform> sortByDepth(IEnumerable<Transform> targets)
        {
            return targets.OrderBy(depthOf).ToList();
        }

        // localScale 한 축이 월드에서 몇 배로 늘어나는지 구한다.
        private static float parentStretch(Transform t, int axis)
        {
            float local = t.localScale[axis];
            if (Mathf.Abs(local) > ZERO_TOLERANCE)
                return t.lossyScale[axis] / local;
            if (t.parent == null)
                return 1f;

            Vector3 direction = t.localRotation * withAxis(Vector3.zero, axis, 1f);
            return t.parent.localToWorldMatrix.MultiplyVector(direction).magnitude;
        }

        // 각 축에 360의 배수를 더해 힌트와 가장 가깝게 맞춘다.
        private static Vector3 nearestTo(Vector3 hint, Vector3 euler)
        {
            return new Vector3(
                hint.x + Mathf.DeltaAngle(hint.x, euler.x),
                hint.y + Mathf.DeltaAngle(hint.y, euler.y),
                hint.z + Mathf.DeltaAngle(hint.z, euler.z));
        }

        private static float distance(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
        }

        private static bool isSkewed(Vector3 a, Vector3 b)
        {
            if (a.sqrMagnitude < ZERO_TOLERANCE || b.sqrMagnitude < ZERO_TOLERANCE)
                return false;
            return Mathf.Abs(Vector3.Dot(a.normalized, b.normalized)) > SKEW_TOLERANCE;
        }

        private static int depthOf(Transform t)
        {
            int depth = 0;
            for (Transform p = t.parent; p != null; p = p.parent)
                depth++;
            return depth;
        }
    }
}
