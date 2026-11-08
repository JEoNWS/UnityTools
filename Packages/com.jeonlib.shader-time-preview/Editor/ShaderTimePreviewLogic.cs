namespace JeonLib.ShaderTimePreview
{
    /// <summary>미리보기 시작 시각과 재생 진행을 계산한다.</summary>
    internal static class ShaderTimePreviewLogic
    {
        /// <summary>URP가 셰이더 `_Time.y`에 넣는 값. 에디터에서 플레이 중이면 Time.time, 아니면 realtimeSinceStartup.</summary>
        public static float shaderTime(bool isPlaying, float time, float realtimeSinceStartup)
        {
            return isPlaying ? time : realtimeSinceStartup;
        }

        /// <summary>셰이더가 `_Time.y - 시작 시각`으로 경과 시간을 구할 때, 그 값이 elapsed가 되는 시작 시각.</summary>
        public static float startTimeFor(float shaderTime, float elapsed)
        {
            return shaderTime - elapsed;
        }

        /// <summary>경과 시간을 delta만큼 진행한다. 끝에 닿으면 loop일 때 처음으로 돌아가고, 아니면 끝에서 멈추고 false를 돌려준다.</summary>
        public static bool advance(ref float elapsed, float delta, float duration, bool loop)
        {
            if (duration <= 0f)
            {
                elapsed = 0f;
                return false;
            }

            elapsed += delta;
            if (elapsed < duration)
                return true;

            if (loop)
            {
                elapsed %= duration;
                return true;
            }

            elapsed = duration;
            return false;
        }
    }
}
