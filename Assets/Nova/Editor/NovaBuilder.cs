// Usage: /path/to/Unity -quit -batchmode -nographics -logFile Build/build.log -executeMethod NovaBuilder.BuildWindows

using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nova.Editor
{
    public static class NovaBuilder
    {
        private static void Build(BuildTarget target, bool isDev = false)
        {
            var productName = Application.productName;
            string targetName;
            string pathSuffix;
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    targetName = "windows";
                    pathSuffix = $"/{productName}.exe";
                    break;
                case BuildTarget.StandaloneLinux64:
                    targetName = "linux";
                    pathSuffix = $"/{productName}";
                    break;
                case BuildTarget.StandaloneOSX:
                    targetName = "macos";
                    pathSuffix = $"/{productName}.app";
                    break;
                case BuildTarget.Android:
                    targetName = "android";
                    pathSuffix = ".apk";
                    break;
                case BuildTarget.iOS:
                    targetName = "ios";
                    pathSuffix = "";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var productNameShort = productName.Replace(" ", "");
            var version = Application.version.TrimStart('v');
            var date = DateTime.Now.ToString("yyyyMMdd");
            var options = BuildOptions.CompressWithLz4HC | BuildOptions.StrictMode;
            if (isDev)
            {
                options |= BuildOptions.Development;
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                locationPathName = $"Build/{productNameShort}_{targetName}_{version}_{date}{pathSuffix}",
                options = options,
                scenes = new[] {"Assets/Scenes/Main.unity"},
                target = target,
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;
            Debug.Log($"Build result: {summary.result}, size: {summary.totalSize}, time: {summary.totalTime}, " +
                      $"path: {summary.outputPath}");
        }

        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64);
        }

        public static void BuildWindowsDev()
        {
            Build(BuildTarget.StandaloneWindows64, isDev: true);
        }

        public static void BuildLinux()
        {
            Build(BuildTarget.StandaloneLinux64);
        }

        public static void BuildLinuxDev()
        {
            Build(BuildTarget.StandaloneLinux64, isDev: true);
        }

        public static void BuildMacOS()
        {
            Build(BuildTarget.StandaloneOSX);
        }

        public static void BuildMacOSDev()
        {
            Build(BuildTarget.StandaloneOSX, isDev: true);
        }

        public static void BuildAndroid()
        {
            Build(BuildTarget.Android);
        }

        public static void BuildAndroidDev()
        {
            Build(BuildTarget.Android, isDev: true);
        }

        public static void BuildiOS()
        {
            Build(BuildTarget.iOS);
        }

        public static void BuildiOSDev()
        {
            Build(BuildTarget.iOS, isDev: true);
        }
    }
}
