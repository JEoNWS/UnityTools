using NUnit.Framework;

namespace JeonLib.SceneSwitcher.Tests
{
    /// <summary>폴더 중복 등록 판정에 쓰는 경로 포함 관계를 검증한다.</summary>
    internal class FolderPathTests
    {
        [TestCase("Assets/Game", "Assets/Game", ExpectedResult = true)]
        [TestCase("Assets/Game/Scenes", "Assets/Game", ExpectedResult = true)]
        [TestCase("Assets/Game/Scenes/Sub", "Assets/Game", ExpectedResult = true)]
        [TestCase("Assets/Game", "Assets/Game/Scenes", ExpectedResult = false)]
        [TestCase("Assets/Game2", "Assets/Game", ExpectedResult = false)]
        [TestCase("Assets/Other", "Assets/Game", ExpectedResult = false)]
        [TestCase("", "Assets/Game", ExpectedResult = false)]
        [TestCase("Assets/Game", "", ExpectedResult = false)]
        [TestCase(null, "Assets/Game", ExpectedResult = false)]
        public bool 같거나_하위_폴더인지(string path, string folder)
        {
            return SceneSwitcherLogic.isSameOrUnder(path, folder);
        }
    }
}
