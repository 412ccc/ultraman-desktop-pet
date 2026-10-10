// SPDX-License-Identifier: MIT
using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using UltramanPet;
using UltramanSetup;

internal static class WindowsTests
{
    private static int checks;
    private static void Check(bool condition, string label)
    { if (!condition) throw new Exception(label); checks++; }
    private static string Digest(byte[] bytes)
    { using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)); }
    [STAThread] private static int Main(string[] args)
    {
        try
        {
            Check(new InstallPaths(null).DirectoryPath == @"D:\Apps\UltramanDesktopPet", "default installation goes to D drive");
            Check(PetSettings.PathForDirectory(@"D:\Apps\UltramanDesktopPet") == @"D:\Apps\UltramanDesktopPet\data\position.txt", "installed settings stay with D drive program");
            Check(PetSettings.PathForDirectory(@"E:\Pets\Ultraman") == @"E:\Pets\Ultraman\data\position.txt", "portable settings stay on its non-C drive");
            Check(PetSettings.PathForDirectory(@"C:\Users\Example\Downloads") == @"D:\Apps\UltramanDesktopPet\data\position.txt", "C drive launch does not save data to C");
            Check(new InstallPaths(null).WorkerDirectory == @"D:\Apps\.UltramanPetTemp", "uninstall temporary worker stays on D drive");
            bool cRejected = false;
            try { new InstallPaths(@"C:\pet-test"); } catch (InvalidOperationException) { cRejected = true; }
            Check(cRejected, "C drive test install rejected before writing");
            for (int i = 0; i < 16; i++)
            {
                double angle = i * Math.PI / 8;
                Check(PetMath.LookIndex(100 * Math.Sin(angle), -100 * Math.Cos(angle), 8) == i, "direction " + i);
                Point cell = PetMath.LookCell(i);
                Check(cell.X == i % 8 && cell.Y == 9 + i / 8, "look cell " + i);
                double below = (i * 22.5 + 11.25 - 0.00001) * Math.PI / 180;
                double above = (i * 22.5 + 11.25 + 0.00001) * Math.PI / 180;
                Check(PetMath.LookIndex(100 * Math.Sin(below), -100 * Math.Cos(below), 0) == i, "sector before " + i);
                Check(PetMath.LookIndex(100 * Math.Sin(above), -100 * Math.Cos(above), 0) == (i + 1) % 16, "sector after " + i);
            }
            Check(PetMath.LookIndex(3, 4, 5) == -1, "neutral deadzone");
            Check(PetMath.Clamp(new Point(-9999, 9999), new Size(144, 156), new Rectangle(-1920, 0, 1920, 1080))
                == new Point(-1920, 924), "negative-coordinate monitor clamp");
            Check(PetMath.Clamp(new Point(99, 99), new Size(500, 500), new Rectangle(0, 0, 100, 100))
                == Point.Empty, "small work area clamp");
            using (Atlas atlas = new Atlas(144))
            {
                int valid = 0;
                for (int row = 0; row < 11; row++)
                    for (int column = 0; column < (row < 9 ? PetMath.FrameCounts[row] : 8); column++)
                    {
                        Bitmap frame = atlas.Get(row, column); int opaque = 0;
                        for (int y = 0; y < frame.Height; y++) for (int x = 0; x < frame.Width; x++)
                            if (frame.GetPixel(x, y).A > 0) opaque++;
                        Check(frame.Width == 144 && frame.Height == 156 && opaque > 100 && opaque < 144 * 156, "transparent occupied frame");
                        valid++;
                    }
                Check(valid == 73, "73 embedded runtime frames");
            }
            for (int row = 0; row < 9; row++)
            {
                Check(PetMath.Frame(row, (PetMath.FrameCounts[row] - 1) * 140) == PetMath.FrameCounts[row] - 1, "last animation frame");
                Check(PetMath.Frame(row, PetMath.FrameCounts[row] * 140) == 0, "animation loops");
            }
            string root = Path.Combine(Path.GetFullPath(args[0]), "sandbox-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            InstallPaths paths = new InstallPaths(root);
            InstallEngine.Install(paths, false);
            Check(!File.Exists(paths.DesktopLink) && !Directory.Exists(paths.MenuDirectory), "no shortcut files created unless requested");
            InstallEngine.Install(paths, true);
            Check(paths.Owned(), "installation ownership marker");
            Check(Digest(File.ReadAllBytes(paths.App)) == Digest(InstallEngine.Resource("PetExe")), "installed executable exactly matches payload");
            Check(File.Exists(paths.Uninstaller), "uninstaller installed");
            Check(String.Equals(InstallEngine.ShortcutTarget(paths.DesktopLink), paths.App, StringComparison.OrdinalIgnoreCase), "desktop shortcut target");
            Check(String.Equals(InstallEngine.ShortcutTarget(paths.MenuLink), paths.App, StringComparison.OrdinalIgnoreCase), "start menu shortcut target");
            File.WriteAllText(Path.Combine(paths.DirectoryPath, "README.txt"), "outdated owned guide");
            InstallEngine.Install(paths, true);
            Check(paths.Owned(), "in-place update of owned installation");
            Check(Digest(File.ReadAllBytes(Path.Combine(paths.DirectoryPath, "README.txt"))) == Digest(InstallEngine.Resource("UserGuide")), "changed owned file repaired");
            File.WriteAllText(Path.Combine(paths.DirectoryPath, "user-file.txt"), "keep");
            string other = Path.Combine(root, "other-app.exe"); File.WriteAllText(other, "other");
            InstallEngine.CreateShortcut(paths.DesktopLink, other);
            InstallEngine.Uninstall(paths);
            Check(!File.Exists(paths.App) && !File.Exists(paths.Uninstaller), "product payload removed");
            Check(File.ReadAllText(Path.Combine(paths.DirectoryPath, "user-file.txt")) == "keep", "unrelated file preserved");
            Check(File.Exists(paths.DesktopLink) && String.Equals(InstallEngine.ShortcutTarget(paths.DesktopLink), other,
                StringComparison.OrdinalIgnoreCase), "unrelated replacement shortcut preserved");
            bool rejected = false;
            try { InstallEngine.Uninstall(paths); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "unowned uninstall rejected");
            rejected = false;
            try { InstallEngine.Install(paths, true); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "unowned directory not overwritten");
            // All cleanup is nonrecursive and confined to files this test created.
            File.Delete(Path.Combine(paths.DirectoryPath, "user-file.txt")); Directory.Delete(paths.DirectoryPath, false);
            File.Delete(paths.DesktopLink); File.Delete(other);
            Directory.Delete(Path.GetDirectoryName(paths.DesktopLink), false);
            Directory.Delete(Path.Combine(root, "Programs"), false);
            Directory.Delete(Path.Combine(root, "StartMenu"), false);
            Directory.Delete(root, false);
            Console.WriteLine("Windows tests passed: " + checks + " checks (frames, pointer sectors, playback, install, update, shortcuts, safe uninstall).");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e.ToString()); return 1; }
    }
}
