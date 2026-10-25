using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace JeonLib.SceneSwitcher.Tests
{
    /// <summary>창에서 분리한 판단 로직(씬 목록, 폴더 추가, 메뉴 조건, 표시 문구)을 검증한다.</summary>
    internal class SceneSwitcherLogicTests
    {
        private static IEnumerable<string> findIn(Dictionary<string, string[]> scenesByFolder, string folder)
        {
            return scenesByFolder.TryGetValue(folder, out var scenes) ? scenes : new string[0];
        }

        // ---- buildGroups ----

        [Test]
        public void 폴더별로_씬을_모으고_대소문자_무시_정렬()
        {
            var scenes = new Dictionary<string, string[]>
            {
                ["Assets/S"] = new[] { "Assets/S/b.unity", "Assets/S/A.unity", "Assets/S/c.unity" },
            };

            var groups = SceneSwitcherLogic.buildGroups(new[] { "Assets/S" }, f => findIn(scenes, f));

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].folderPath, Is.EqualTo("Assets/S"));
            Assert.That(groups[0].scenePaths, Is.EqualTo(new[] { "Assets/S/A.unity", "Assets/S/b.unity", "Assets/S/c.unity" }));
        }

        [Test]
        public void 겹친_폴더의_씬은_먼저_나온_그룹에만()
        {
            var scenes = new Dictionary<string, string[]>
            {
                ["Assets/A"] = new[] { "Assets/A/x.unity", "Assets/A/B/y.unity" },
                ["Assets/A/B"] = new[] { "Assets/A/B/y.unity" },
            };

            var groups = SceneSwitcherLogic.buildGroups(new[] { "Assets/A", "Assets/A/B" }, f => findIn(scenes, f));

            Assert.That(groups[0].scenePaths, Is.EqualTo(new[] { "Assets/A/B/y.unity", "Assets/A/x.unity" }));
            Assert.That(groups[1].scenePaths, Is.Empty);
        }

        [Test]
        public void 빈_폴더_경로는_건너뜀()
        {
            var groups = SceneSwitcherLogic.buildGroups(new[] { null, "", "Assets/S" }, f => new string[0]);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].folderPath, Is.EqualTo("Assets/S"));
        }

        // ---- planAddFolder ----

        [Test]
        public void 새_폴더는_추가()
        {
            var plan = SceneSwitcherLogic.planAddFolder("Assets/Game", new[] { "Assets/Other" });

            Assert.That(plan.result, Is.EqualTo(eFolderAddResult.Add));
            Assert.That(plan.childFolders, Is.Empty);
        }

        [Test]
        public void 등록된_폴더의_하위면_거부()
        {
            var plan = SceneSwitcherLogic.planAddFolder("Assets/Game/Scenes", new[] { "Assets/Game" });

            Assert.That(plan.result, Is.EqualTo(eFolderAddResult.InsideRegistered));
            Assert.That(plan.parentFolder, Is.EqualTo("Assets/Game"));
        }

        [Test]
        public void 이미_등록된_같은_폴더면_거부()
        {
            var plan = SceneSwitcherLogic.planAddFolder("Assets/Game", new[] { "Assets/Game" });

            Assert.That(plan.result, Is.EqualTo(eFolderAddResult.InsideRegistered));
            Assert.That(plan.parentFolder, Is.EqualTo("Assets/Game"));
        }

        [Test]
        public void 등록된_하위_폴더가_있으면_합치기()
        {
            var plan = SceneSwitcherLogic.planAddFolder("Assets/Game", new[] { "Assets/Game/A", "Assets/Other", "Assets/Game/B" });

            Assert.That(plan.result, Is.EqualTo(eFolderAddResult.MergeChildren));
            Assert.That(plan.childFolders, Is.EqualTo(new[] { "Assets/Game/A", "Assets/Game/B" }));
        }

        [Test]
        public void 이름만_비슷한_형제_폴더와_삭제된_폴더는_무관()
        {
            var plan = SceneSwitcherLogic.planAddFolder("Assets/Game", new[] { "Assets/Game2", null, "" });

            Assert.That(plan.result, Is.EqualTo(eFolderAddResult.Add));
        }

        // ---- buildRowLabel ----

        [Test]
        public void 행_이름은_폴더_기준_상대경로와_표시()
        {
            string label = SceneSwitcherLogic.buildRowLabel("Assets/S/Sub/A.unity", "Assets/S", isStart: true, isInBuild: true);

            Assert.That(label, Is.EqualTo("Sub/A  [Start]  [Build]"));
        }

        [Test]
        public void 표시가_없으면_씬_이름만()
        {
            string label = SceneSwitcherLogic.buildRowLabel("Assets/S/A.unity", "Assets/S", isStart: false, isInBuild: false);

            Assert.That(label, Is.EqualTo("A"));
        }

        // ---- 메뉴 조건 ----

        [TestCase(false, ExpectedResult = true)]
        [TestCase(true, ExpectedResult = false)]
        public bool 이미_열린_씬은_추가로_열_수_없음(bool isLoaded)
        {
            return SceneSwitcherLogic.canOpenAdditive(isLoaded);
        }

        [TestCase(true, 2, ExpectedResult = true)]
        [TestCase(true, 1, ExpectedResult = false)]
        [TestCase(false, 3, ExpectedResult = false)]
        public bool 마지막_씬이나_안_열린_씬은_닫을_수_없음(bool isLoaded, int openSceneCount)
        {
            return SceneSwitcherLogic.canClose(isLoaded, openSceneCount);
        }

        // ---- 드래그 판정 ----

        [TestCase(3f, 0f, ExpectedResult = false)]
        [TestCase(4f, 0f, ExpectedResult = true)]
        [TestCase(3f, 3f, ExpectedResult = true)]
        public bool 일정_거리_이상_움직이면_드래그(float dx, float dy)
        {
            return SceneSwitcherLogic.isDragStarted(Vector2.zero, new Vector2(dx, dy));
        }
    }
}
