using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace JeonLib.CameraPreview.Tests
{
    /// <summary>창에서 분리한 렌더 경로, 상태, 대상 카메라, 크기 계산을 검증한다.</summary>
    internal class CameraPreviewLogicTests
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

        private GameObject create(string name)
        {
            var go = new GameObject(name);
            m_created.Add(go);
            return go;
        }

        private Camera createCamera(string name)
        {
            return create(name).AddComponent<Camera>();
        }

        private static void assertRect(Rect actual, Rect expected)
        {
            Assert.That(Vector2.Distance(actual.position, expected.position), Is.LessThan(TOLERANCE), $"position {actual.position}, expected {expected.position}");
            Assert.That(Vector2.Distance(actual.size, expected.size), Is.LessThan(TOLERANCE), $"size {actual.size}, expected {expected.size}");
        }

        // ---- 렌더 경로 ----

        [Test]
        public void 파이프라인이_없으면_BuiltIn()
        {
            Assert.That(CameraPreviewLogic.chooseRenderPath(false, false), Is.EqualTo(eRenderPath.BuiltIn));
        }

        [Test]
        public void 렌더_요청을_지원하는_파이프라인이면_RenderRequest()
        {
            Assert.That(CameraPreviewLogic.chooseRenderPath(true, true), Is.EqualTo(eRenderPath.RenderRequest));
        }

        [Test]
        public void 렌더_요청을_지원하지_않는_파이프라인이면_Unsupported()
        {
            Assert.That(CameraPreviewLogic.chooseRenderPath(true, false), Is.EqualTo(eRenderPath.Unsupported));
        }

        // ---- 상태 ----

        [Test]
        public void 플레이_중이면_다른_조건보다_Playing이_우선()
        {
            Camera cam = createCamera("Cam");
            Assert.That(CameraPreviewLogic.resolveStatus(true, null, false, eRenderPath.Unsupported), Is.EqualTo(ePreviewStatus.Playing));
            Assert.That(CameraPreviewLogic.resolveStatus(true, cam, true, eRenderPath.BuiltIn), Is.EqualTo(ePreviewStatus.Playing));
        }

        [Test]
        public void 카메라가_없으면_NoCamera()
        {
            Assert.That(CameraPreviewLogic.resolveStatus(false, null, false, eRenderPath.BuiltIn), Is.EqualTo(ePreviewStatus.NoCamera));
        }

        [Test]
        public void 프리팹_에셋_카메라면_경로와_상관없이_PrefabAsset()
        {
            Camera cam = createCamera("Asset");
            Assert.That(CameraPreviewLogic.resolveStatus(false, cam, true, eRenderPath.BuiltIn), Is.EqualTo(ePreviewStatus.PrefabAsset));
            Assert.That(CameraPreviewLogic.resolveStatus(false, cam, true, eRenderPath.Unsupported), Is.EqualTo(ePreviewStatus.PrefabAsset));
        }

        [Test]
        public void 지원하지_않는_경로면_Unsupported()
        {
            Camera cam = createCamera("Cam");
            Assert.That(CameraPreviewLogic.resolveStatus(false, cam, false, eRenderPath.Unsupported), Is.EqualTo(ePreviewStatus.Unsupported));
        }

        [Test]
        public void 카메라와_지원하는_경로가_있으면_Ready()
        {
            Camera cam = createCamera("Cam");
            Assert.That(CameraPreviewLogic.resolveStatus(false, cam, false, eRenderPath.BuiltIn), Is.EqualTo(ePreviewStatus.Ready));
            Assert.That(CameraPreviewLogic.resolveStatus(false, cam, false, eRenderPath.RenderRequest), Is.EqualTo(ePreviewStatus.Ready));
        }

        // ---- 대상 카메라 ----

        [Test]
        public void 핀이_있으면_선택과_상관없이_핀_카메라()
        {
            Camera pinned = createCamera("Pinned");
            Camera selected = createCamera("Selected");
            Assert.That(CameraPreviewLogic.resolveTarget(pinned, selected.gameObject), Is.SameAs(pinned));
        }

        [Test]
        public void 핀이_없으면_선택한_오브젝트의_카메라()
        {
            Camera selected = createCamera("Selected");
            Assert.That(CameraPreviewLogic.resolveTarget(null, selected.gameObject), Is.SameAs(selected));
        }

        [Test]
        public void 핀_카메라가_삭제되면_선택한_카메라로()
        {
            Camera pinned = createCamera("Pinned");
            Camera selected = createCamera("Selected");
            Object.DestroyImmediate(pinned.gameObject);
            Assert.That(CameraPreviewLogic.resolveTarget(pinned, selected.gameObject), Is.SameAs(selected));
        }

        [Test]
        public void 선택한_오브젝트에_카메라가_없으면_null()
        {
            Assert.That(CameraPreviewLogic.resolveTarget(null, create("Empty")), Is.Null);
        }

        [Test]
        public void 아무것도_선택하지_않으면_null()
        {
            Assert.That(CameraPreviewLogic.resolveTarget(null, null), Is.Null);
        }

        // ---- 카메라 목록 ----

        [Test]
        public void 일반_카메라는_목록에_포함()
        {
            Assert.That(CameraPreviewLogic.isListedCamera(createCamera("Cam")), Is.True);
        }

        [Test]
        public void Hierarchy에_숨긴_카메라는_목록에서_제외()
        {
            Camera cam = createCamera("Hidden");
            cam.gameObject.hideFlags = HideFlags.HideInHierarchy;
            Assert.That(CameraPreviewLogic.isListedCamera(cam), Is.False);
        }

        [Test]
        public void 게임_카메라가_아니면_목록에서_제외()
        {
            Camera cam = createCamera("Preview");
            cam.cameraType = CameraType.Preview;
            Assert.That(CameraPreviewLogic.isListedCamera(cam), Is.False);
        }

        // ---- 비율 ----

        [Test]
        public void 고정_비율_모드는_정해진_비율()
        {
            Vector2 gameView = new Vector2(1000, 1000);
            Assert.That(CameraPreviewLogic.aspectOf(eAspectMode.Ratio16x9, gameView), Is.EqualTo(16f / 9f).Within(TOLERANCE));
            Assert.That(CameraPreviewLogic.aspectOf(eAspectMode.Ratio4x3, gameView), Is.EqualTo(4f / 3f).Within(TOLERANCE));
            Assert.That(CameraPreviewLogic.aspectOf(eAspectMode.Ratio1x1, gameView), Is.EqualTo(1f).Within(TOLERANCE));
        }

        [Test]
        public void GameView_모드는_Game_뷰_크기의_비율()
        {
            Assert.That(CameraPreviewLogic.aspectOf(eAspectMode.GameView, new Vector2(1080, 1920)), Is.EqualTo(1080f / 1920f).Within(TOLERANCE));
        }

        [Test]
        public void Game_뷰_크기가_0이면_기본_비율()
        {
            Assert.That(CameraPreviewLogic.aspectOf(eAspectMode.GameView, Vector2.zero), Is.EqualTo(CameraPreviewLogic.FALLBACK_ASPECT).Within(TOLERANCE));
        }

        // ---- 영역 맞춤 ----

        [Test]
        public void 넓은_영역에서는_좌우에_여백()
        {
            assertRect(CameraPreviewLogic.fitRect(new Vector2(400, 100), 2f), new Rect(100, 0, 200, 100));
        }

        [Test]
        public void 높은_영역에서는_위아래에_여백()
        {
            assertRect(CameraPreviewLogic.fitRect(new Vector2(200, 400), 2f), new Rect(0, 150, 200, 100));
        }

        [Test]
        public void 영역이나_비율이_0이면_빈_영역()
        {
            assertRect(CameraPreviewLogic.fitRect(Vector2.zero, 2f), Rect.zero);
            assertRect(CameraPreviewLogic.fitRect(new Vector2(400, 100), 0f), Rect.zero);
        }

        // ---- 텍스처 크기 ----

        [Test]
        public void 텍스처_크기는_픽셀_배율을_곱해_반올림()
        {
            Assert.That(CameraPreviewLogic.textureSize(new Vector2(100.4f, 50.2f), 1.5f), Is.EqualTo(new Vector2Int(151, 75)));
        }

        [Test]
        public void 최대_크기를_넘으면_비율을_유지하며_축소()
        {
            Assert.That(CameraPreviewLogic.textureSize(new Vector2(8192, 2048), 1f), Is.EqualTo(new Vector2Int(CameraPreviewLogic.MAX_TEXTURE_SIZE, 1024)));
        }

        [Test]
        public void 크기가_0이면_텍스처_크기도_0()
        {
            Assert.That(CameraPreviewLogic.textureSize(new Vector2(0, 100), 1f), Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void 아주_작아도_한_변은_최소_1픽셀()
        {
            Assert.That(CameraPreviewLogic.textureSize(new Vector2(100, 0.1f), 1f), Is.EqualTo(new Vector2Int(100, 1)));
        }
    }
}
