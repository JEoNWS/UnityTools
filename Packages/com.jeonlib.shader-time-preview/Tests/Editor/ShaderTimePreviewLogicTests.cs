using NUnit.Framework;

namespace JeonLib.ShaderTimePreview.Tests
{
    /// <summary>창에서 분리한 미리보기 시작 시각과 재생 진행 계산을 검증한다.</summary>
    internal class ShaderTimePreviewLogicTests
    {
        private const float TOLERANCE = 1e-5f;

        // ---- shaderTime ----

        [Test]
        public void 플레이_중이면_Time_time을_셰이더_시간으로_씀()
        {
            Assert.That(ShaderTimePreviewLogic.shaderTime(true, 3f, 100f), Is.EqualTo(3f));
        }

        [Test]
        public void 에디트_모드면_realtimeSinceStartup을_셰이더_시간으로_씀()
        {
            Assert.That(ShaderTimePreviewLogic.shaderTime(false, 3f, 100f), Is.EqualTo(100f));
        }

        // ---- startTimeFor ----

        [Test]
        public void 셰이더_시간에서_경과_시간을_뺀_시작_시각을_돌려줌()
        {
            Assert.That(ShaderTimePreviewLogic.startTimeFor(10f, 0.25f), Is.EqualTo(9.75f).Within(TOLERANCE));
        }

        // ---- advance ----

        [Test]
        public void 끝에_닿기_전이면_delta만큼_진행하고_계속_재생()
        {
            float elapsed = 0.1f;

            bool keepPlaying = ShaderTimePreviewLogic.advance(ref elapsed, 0.2f, 1f, false);

            Assert.That(keepPlaying, Is.True);
            Assert.That(elapsed, Is.EqualTo(0.3f).Within(TOLERANCE));
        }

        [Test]
        public void 반복이_꺼져_있으면_끝에서_멈추고_재생_종료()
        {
            float elapsed = 0.9f;

            bool keepPlaying = ShaderTimePreviewLogic.advance(ref elapsed, 0.2f, 1f, false);

            Assert.That(keepPlaying, Is.False);
            Assert.That(elapsed, Is.EqualTo(1f).Within(TOLERANCE));
        }

        [Test]
        public void 반복이_켜져_있으면_넘친_만큼_처음부터_이어서_재생()
        {
            float elapsed = 0.9f;

            bool keepPlaying = ShaderTimePreviewLogic.advance(ref elapsed, 0.2f, 1f, true);

            Assert.That(keepPlaying, Is.True);
            Assert.That(elapsed, Is.EqualTo(0.1f).Within(TOLERANCE));
        }

        [Test]
        public void 길이가_0_이하면_처음으로_돌리고_재생_종료()
        {
            float elapsed = 0.5f;

            bool keepPlaying = ShaderTimePreviewLogic.advance(ref elapsed, 0.1f, 0f, true);

            Assert.That(keepPlaying, Is.False);
            Assert.That(elapsed, Is.EqualTo(0f));
        }
    }
}
