using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace JeonLib.SceneSwitcher
{
    /// <summary>폴더 하나와 그 아래 씬 경로 목록.</summary>
    internal class SceneFolderGroup
    {
        public string folderPath { get; }
        public List<string> scenePaths { get; }

        public SceneFolderGroup(string folderPath, List<string> scenePaths)
        {
            this.folderPath = folderPath;
            this.scenePaths = scenePaths;
        }
    }

    /// <summary>폴더 추가 판정 결과.</summary>
    internal enum eFolderAddResult
    {
        Add,
        InsideRegistered,
        MergeChildren,
    }

    /// <summary>폴더를 추가할 때 할 일. 거부 사유가 된 상위 폴더나 합칠 하위 폴더를 담는다.</summary>
    internal class FolderAddPlan
    {
        public eFolderAddResult result { get; }
        public string parentFolder { get; }
        public IReadOnlyList<string> childFolders { get; }

        public FolderAddPlan(eFolderAddResult result, string parentFolder, IReadOnlyList<string> childFolders)
        {
            this.result = result;
            this.parentFolder = parentFolder;
            this.childFolders = childFolders;
        }
    }

    /// <summary>창에서 쓰는 판단 로직. UI와 분리해 테스트할 수 있게 둔다.</summary>
    internal static class SceneSwitcherLogic
    {
        internal const float DRAG_THRESHOLD = 4f;

        /// <summary>폴더 순서대로 씬을 모은다. 앞 그룹에 이미 나온 씬은 뒤 그룹에서 뺀다.</summary>
        public static List<SceneFolderGroup> buildGroups(IEnumerable<string> folderPaths, Func<string, IEnumerable<string>> findScenes)
        {
            var groups = new List<SceneFolderGroup>();
            var shown = new HashSet<string>();

            foreach (string folderPath in folderPaths)
            {
                if (string.IsNullOrEmpty(folderPath))
                    continue;

                List<string> scenes = findScenes(folderPath)
                    .Where(shown.Add)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                groups.Add(new SceneFolderGroup(folderPath, scenes));
            }
            return groups;
        }

        /// <summary>등록된 폴더 안에 들어가면 거부하고, 등록된 하위 폴더가 있으면 합칠 대상으로 돌려준다.</summary>
        public static FolderAddPlan planAddFolder(string folderPath, IEnumerable<string> registeredFolderPaths)
        {
            List<string> registered = registeredFolderPaths.Where(p => !string.IsNullOrEmpty(p)).ToList();

            string parent = registered.FirstOrDefault(p => isSameOrUnder(folderPath, p));
            if (parent != null)
                return new FolderAddPlan(eFolderAddResult.InsideRegistered, parent, Array.Empty<string>());

            List<string> children = registered.Where(p => isSameOrUnder(p, folderPath)).ToList();
            return 0 < children.Count
                ? new FolderAddPlan(eFolderAddResult.MergeChildren, null, children)
                : new FolderAddPlan(eFolderAddResult.Add, null, Array.Empty<string>());
        }

        public static bool isSameOrUnder(string path, string folder)
        {
            return !string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(folder)
                && (path == folder || path.StartsWith(folder + "/", StringComparison.Ordinal));
        }

        public static string buildRowLabel(string scenePath, string folderPath, bool isStart, bool isInBuild)
        {
            string relative = scenePath.Substring(folderPath.Length).TrimStart('/');
            return Path.ChangeExtension(relative, null)
                   + (isStart ? "  [Start]" : "")
                   + (isInBuild ? "  [Build]" : "");
        }

        public static bool canOpenAdditive(bool isLoaded) => !isLoaded;

        public static bool canClose(bool isLoaded, int openSceneCount) => isLoaded && 1 < openSceneCount;

        public static bool isDragStarted(Vector2 pressPosition, Vector2 currentPosition)
        {
            return DRAG_THRESHOLD <= Vector2.Distance(pressPosition, currentPosition);
        }
    }
}
