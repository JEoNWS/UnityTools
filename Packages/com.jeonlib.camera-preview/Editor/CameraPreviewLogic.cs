using UnityEngine;

namespace JeonLib.CameraPreview
{
    internal enum eRenderPath
    {
        BuiltIn,
        RenderRequest,
        Unsupported,
    }

    internal enum ePreviewStatus
    {
        Ready,
        Playing,
        NoCamera,
        PrefabAsset,
        Unsupported,
    }

    internal enum eAspectMode
    {
        GameView,
        Ratio16x9,
        Ratio4x3,
        Ratio1x1,
    }

    /// <summary>미리보기 창에서 분리한 판단과 크기 계산.</summary>
    internal static class CameraPreviewLogic
    {
        internal const float FALLBACK_ASPECT = 16f / 9f;
        internal const int MAX_TEXTURE_SIZE = 4096;

        public static eRenderPath chooseRenderPath(bool hasPipeline, bool supportsRequest)
        {
            if (!hasPipeline)
                return eRenderPath.BuiltIn;

            return supportsRequest ? eRenderPath.RenderRequest : eRenderPath.Unsupported;
        }

        /// <summary>isAsset은 대상이 Project 창의 프리팹 에셋 안 카메라인지.</summary>
        public static ePreviewStatus resolveStatus(bool isPlaying, Camera target, bool isAsset, eRenderPath path)
        {
            if (isPlaying)
                return ePreviewStatus.Playing;
            if (target == null)
                return ePreviewStatus.NoCamera;
            if (isAsset)
                return ePreviewStatus.PrefabAsset;

            return path == eRenderPath.Unsupported ? ePreviewStatus.Unsupported : ePreviewStatus.Ready;
        }

        /// <summary>핀이 살아 있으면 핀, 아니면 선택한 오브젝트의 카메라.</summary>
        public static Camera resolveTarget(Camera pinned, GameObject selected)
        {
            if (pinned != null)
                return pinned;

            return selected != null && selected.TryGetComponent(out Camera cam) ? cam : null;
        }

        /// <summary>카메라 메뉴에 보여 줄 카메라인지. 씬뷰나 프리뷰용 내부 카메라는 뺀다.</summary>
        public static bool isListedCamera(Camera cam)
        {
            return cam != null
                && cam.cameraType == CameraType.Game
                && (cam.gameObject.hideFlags & HideFlags.HideInHierarchy) == 0;
        }

        public static float aspectOf(eAspectMode mode, Vector2 gameViewSize)
        {
            switch (mode)
            {
                case eAspectMode.Ratio16x9:
                    return 16f / 9f;
                case eAspectMode.Ratio4x3:
                    return 4f / 3f;
                case eAspectMode.Ratio1x1:
                    return 1f;
                default:
                    return gameViewSize.x > 0 && gameViewSize.y > 0 ? gameViewSize.x / gameViewSize.y : FALLBACK_ASPECT;
            }
        }

        /// <summary>area 안에 비율을 지키며 가장 크게 들어가는 가운데 정렬 영역.</summary>
        public static Rect fitRect(Vector2 area, float aspect)
        {
            if (area.x <= 0 || area.y <= 0 || aspect <= 0)
                return Rect.zero;

            float width = Mathf.Min(area.x, area.y * aspect);
            float height = width / aspect;
            return new Rect((area.x - width) * 0.5f, (area.y - height) * 0.5f, width, height);
        }

        /// <summary>최대 크기를 넘으면 비율을 유지하며 줄인다.</summary>
        public static Vector2Int textureSize(Vector2 size, float pixelsPerPoint)
        {
            if (size.x <= 0 || size.y <= 0 || pixelsPerPoint <= 0)
                return Vector2Int.zero;

            float width = size.x * pixelsPerPoint;
            float height = size.y * pixelsPerPoint;
            float scale = Mathf.Min(1f, MAX_TEXTURE_SIZE / Mathf.Max(width, height));
            return new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(width * scale)),
                Mathf.Max(1, Mathf.RoundToInt(height * scale)));
        }
    }
}
