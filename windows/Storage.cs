// SPDX-License-Identifier: MIT
using System;
using System.IO;

namespace UltramanStorage
{
    internal static class StoragePaths
    {
        internal const string DefaultInstall = @"D:\Apps\UltramanDesktopPet";
        internal static bool IsCDrive(string path)
        {
            return String.Equals(Path.GetPathRoot(Path.GetFullPath(path)), @"C:\", StringComparison.OrdinalIgnoreCase);
        }
        internal static string RequireNonC(string path)
        {
            string full = Path.GetFullPath(path);
            if (IsCDrive(full)) throw new InvalidOperationException("请把程序及输出目录放到 D 盘或其他非 C 盘位置。");
            return full;
        }
        internal static string DataFor(string applicationDirectory)
        {
            return Path.Combine(IsCDrive(applicationDirectory) ? DefaultInstall : Path.GetFullPath(applicationDirectory), "data");
        }
        internal static string WorkerFor(string installationDirectory)
        {
            return Path.Combine(Path.GetDirectoryName(RequireNonC(installationDirectory)), ".UltramanPetTemp");
        }
    }
}
