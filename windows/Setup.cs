// SPDX-License-Identifier: MIT
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Ultraman Desktop Pet Setup")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace UltramanSetup
{
    public sealed class InstallPaths
    {
        public const string ProductId = "UltramanDesktopPet.412ccc";
        public const string Marker = ".pet-install";
        public readonly string DirectoryPath, DesktopLink, MenuDirectory, MenuLink;
        public readonly bool Sandbox;
        public InstallPaths(string sandboxRoot)
        {
            Sandbox = sandboxRoot != null;
            string programs = Sandbox ? Path.Combine(Path.GetFullPath(sandboxRoot), "Programs") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
            DirectoryPath = Path.GetFullPath(Path.Combine(programs, "UltramanDesktopPet"));
            if (!DirectoryPath.StartsWith(Path.GetFullPath(programs).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe installation path");
            string desktop = Sandbox ? Path.Combine(sandboxRoot, "Desktop") : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string menu = Sandbox ? Path.Combine(sandboxRoot, "StartMenu") : Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            DesktopLink = Path.Combine(desktop, "奥特曼桌宠.lnk");
            MenuDirectory = Path.Combine(menu, "Ultraman Desktop Pet");
            MenuLink = Path.Combine(MenuDirectory, "奥特曼桌宠.lnk");
        }
        public string App { get { return Path.Combine(DirectoryPath, "UltramanPet.exe"); } }
        public string Uninstaller { get { return Path.Combine(DirectoryPath, "Uninstall.exe"); } }
        public bool Owned()
        {
            string marker = Path.Combine(DirectoryPath, Marker);
            return File.Exists(marker) && File.ReadAllText(marker).Trim() == ProductId;
        }
    }
    public static class InstallEngine
    {
        public static bool PetRunning()
        {
            try { using (Mutex mutex = Mutex.OpenExisting("Local\\UltramanDesktopPet.412ccc")) return true; }
            catch (WaitHandleCannotBeOpenedException) { return false; }
        }
        public static byte[] Resource(string name)
        {
            using (Stream stream = typeof(InstallEngine).Assembly.GetManifestResourceStream(name))
            {
                if (stream == null) throw new InvalidDataException("Missing installer payload: " + name);
                using (MemoryStream output = new MemoryStream()) { stream.CopyTo(output); return output.ToArray(); }
            }
        }
        private static void Write(string destination, byte[] bytes)
        {
            if (File.Exists(destination) && File.ReadAllBytes(destination).SequenceEqual(bytes)) return;
            string temporary = destination + ".new";
            File.WriteAllBytes(temporary, bytes);
            if (File.Exists(destination)) File.Replace(temporary, destination, null);
            else File.Move(temporary, destination);
        }
        public static void Install(InstallPaths paths, bool desktop)
        {
            if (!paths.Sandbox && PetRunning()) throw new InvalidOperationException("请先在桌宠右键菜单中选择退出，再安装或更新。");
            if (Directory.Exists(paths.DirectoryPath) && !paths.Owned())
                throw new InvalidOperationException("安装目录中已有其他文件，未覆盖。请先选择其他位置保存这些文件。");
            Directory.CreateDirectory(paths.DirectoryPath);
            File.WriteAllText(Path.Combine(paths.DirectoryPath, InstallPaths.Marker), InstallPaths.ProductId);
            Write(paths.App, Resource("PetExe"));
            Write(Path.Combine(paths.DirectoryPath, "LICENSE.txt"), Resource("CodeLicense"));
            Write(Path.Combine(paths.DirectoryPath, "ASSET_TERMS.txt"), Resource("AssetTerms"));
            Write(Path.Combine(paths.DirectoryPath, "README.txt"), Resource("UserGuide"));
            Write(paths.Uninstaller, File.ReadAllBytes(typeof(InstallEngine).Assembly.Location));
            Directory.CreateDirectory(paths.MenuDirectory);
            CreateOwnedShortcut(paths.MenuLink, paths.App);
            if (desktop) { Directory.CreateDirectory(Path.GetDirectoryName(paths.DesktopLink)); CreateOwnedShortcut(paths.DesktopLink, paths.App); }
            if (!paths.Sandbox)
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + InstallPaths.ProductId))
                {
                    key.SetValue("DisplayName", "奥特曼桌宠");
                    key.SetValue("DisplayVersion", "1.0.0");
                    key.SetValue("Publisher", "412ccc / Ultraman Desktop Pet contributors");
                    key.SetValue("InstallLocation", paths.DirectoryPath);
                    key.SetValue("DisplayIcon", paths.App + ",0");
                    key.SetValue("UninstallString", "\"" + paths.Uninstaller + "\" --uninstall");
                    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    key.SetValue("EstimatedSize", 5200, RegistryValueKind.DWord);
                }
            }
        }
        public static void Uninstall(InstallPaths paths)
        {
            if (!paths.Owned()) throw new InvalidOperationException("未找到本程序的安装记录，未删除任何文件。");
            if (!paths.Sandbox && PetRunning()) throw new InvalidOperationException("请先退出桌宠，再卸载。");
            // Delete only known payload files inside the fixed, owned product folder.
            foreach (string file in new string[] { "UltramanPet.exe", "LICENSE.txt", "ASSET_TERMS.txt", "README.txt", "Uninstall.exe" })
                if (File.Exists(Path.Combine(paths.DirectoryPath, file))) File.Delete(Path.Combine(paths.DirectoryPath, file));
            DeleteOwnedShortcut(paths.DesktopLink, paths.App);
            DeleteOwnedShortcut(paths.MenuLink, paths.App);
            if (Directory.Exists(paths.MenuDirectory) && Directory.GetFileSystemEntries(paths.MenuDirectory).Length == 0)
                Directory.Delete(paths.MenuDirectory, false);
            File.Delete(Path.Combine(paths.DirectoryPath, InstallPaths.Marker));
            if (Directory.GetFileSystemEntries(paths.DirectoryPath).Length == 0) Directory.Delete(paths.DirectoryPath, false);
            if (!paths.Sandbox)
            {
                string name = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + InstallPaths.ProductId;
                bool owned;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(name))
                    owned = key != null && String.Equals(key.GetValue("InstallLocation") as string, paths.DirectoryPath, StringComparison.OrdinalIgnoreCase);
                if (owned) Registry.CurrentUser.DeleteSubKey(name, false);
            }
        }
        public static void CreateShortcut(string path, string target)
        {
            object shell = null, link = null;
            try
            {
                shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                link = shell.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
                link.GetType().InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { target });
                link.GetType().InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { Path.GetDirectoryName(target) });
                link.GetType().InvokeMember("IconLocation", BindingFlags.SetProperty, null, link, new object[] { target + ",0" });
                link.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
            }
            finally { if (link != null) Marshal.ReleaseComObject(link); if (shell != null) Marshal.ReleaseComObject(shell); }
        }
        private static void CreateOwnedShortcut(string path, string target)
        {
            if (!File.Exists(path) || String.Equals(ShortcutTarget(path), target, StringComparison.OrdinalIgnoreCase))
                CreateShortcut(path, target);
        }
        public static string ShortcutTarget(string path)
        {
            object shell = null, link = null;
            try
            {
                shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                link = shell.GetType().InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
                return link.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, link, null) as string;
            }
            finally { if (link != null) Marshal.ReleaseComObject(link); if (shell != null) Marshal.ReleaseComObject(shell); }
        }
        private static void DeleteOwnedShortcut(string path, string target)
        {
            if (File.Exists(path) && String.Equals(ShortcutTarget(path), target, StringComparison.OrdinalIgnoreCase)) File.Delete(path);
        }
    }

    internal sealed class SetupWindow : Form
    {
        private readonly CheckBox desktop = new CheckBox(), launch = new CheckBox();
        private readonly Button install = new Button();
        private readonly InstallPaths paths = new InstallPaths(null);
        private readonly bool uninstall;
        public SetupWindow(bool removal)
        {
            uninstall = removal;
            Text = removal ? "卸载奥特曼桌宠" : "安装奥特曼桌宠";
            ClientSize = new Size(620, 340); Font = new Font("Microsoft YaHei UI", 10);
            AutoScaleMode = AutoScaleMode.Font; StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            BackColor = Color.White;
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            Panel artwork = new Panel { Location = Point.Empty, Size = new Size(190, 340), BackColor = Color.FromArgb(230, 241, 255) };
            PictureBox pet = new PictureBox { Location = new Point(8, 64), Size = new Size(174, 190), SizeMode = PictureBoxSizeMode.Zoom };
            using (MemoryStream stream = new MemoryStream(InstallEngine.Resource("Hero")))
            using (Bitmap source = new Bitmap(stream)) pet.Image = new Bitmap(source);
            artwork.Controls.Add(pet); Controls.Add(artwork);
            Label title = new Label { Text = removal ? "卸载奥特曼桌宠" : "把奥特曼带到桌面", AutoSize = true,
                Font = new Font(Font.FontFamily, 18, FontStyle.Bold), Location = new Point(215, 26) };
            Controls.Add(title);
            Controls.Add(new Label { Text = removal ? "移除程序及快捷方式。你的桌宠位置设置会保留。" : "透明背景，鼠标互动，九组动作。\n安装到当前用户，无需管理员权限。",
                Location = new Point(219, 80), Size = new Size(378, 62) });
            desktop.Text = "创建桌面快捷方式"; desktop.Checked = true; desktop.AutoSize = true; desktop.Location = new Point(220, 157);
            launch.Text = "安装后打开桌宠"; launch.Checked = true; launch.AutoSize = true; launch.Location = new Point(220, 190);
            if (!removal) { Controls.Add(desktop); Controls.Add(launch); }
            LinkLabel terms = new LinkLabel { Text = "许可与素材权利说明", AutoSize = true, Location = new Point(220, 239) };
            terms.LinkClicked += delegate { MessageBox.Show(System.Text.Encoding.UTF8.GetString(InstallEngine.Resource("AssetTerms")), "素材权利说明", MessageBoxButtons.OK, MessageBoxIcon.Information); };
            Controls.Add(terms);
            Controls.Add(new Label { Text = "1.0.0 · Windows 10 / 11\n奥特曼同人作品", Size = new Size(245, 38),
                Font = new Font(Font.FontFamily, 8), ForeColor = Color.DimGray, Location = new Point(218, 278) });
            install.Text = removal ? "卸载" : "安装"; install.Location = new Point(480, 275); install.Size = new Size(110, 38);
            install.Click += InstallClicked; Controls.Add(install); AcceptButton = install;
        }
        private void InstallClicked(object sender, EventArgs args)
        {
            install.Enabled = false;
            try
            {
                if (uninstall)
                {
                    if (!paths.Owned()) throw new InvalidOperationException("未找到本程序的安装记录。");
                    if (InstallEngine.PetRunning()) throw new InvalidOperationException("请先在桌宠右键菜单中选择退出，再卸载。");
                    string temporary = Path.Combine(Path.GetTempPath(), "UltramanPet-Uninstall-" + Guid.NewGuid().ToString("N") + ".exe");
                    File.Copy(Assembly.GetExecutingAssembly().Location, temporary);
                    Process.Start(new ProcessStartInfo(temporary, "--uninstall-worker " + Process.GetCurrentProcess().Id)
                        { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
                    Close();
                }
                else
                {
                    InstallEngine.Install(paths, desktop.Checked);
                    if (launch.Checked) Process.Start(new ProcessStartInfo(paths.App) { UseShellExecute = true });
                    MessageBox.Show("安装完成！\n\n鼠标靠近会互动，按住左键可以拖动，右键可以退出。", "奥特曼桌宠", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
            }
            catch (Exception e) { MessageBox.Show(e.Message, "操作未完成", MessageBoxButtons.OK, MessageBoxIcon.Warning); install.Enabled = true; }
        }
    }
    internal static class SetupProgram
    {
        [STAThread] private static void Main(string[] args)
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 2 && args[0] == "--uninstall-worker")
            {
                try
                {
                    int parent;
                    if (!Int32.TryParse(args[1], out parent)) throw new InvalidOperationException("Invalid uninstall request");
                    try { Process.GetProcessById(parent).WaitForExit(15000); } catch (ArgumentException) { }
                    InstallEngine.Uninstall(new InstallPaths(null));
                    MessageBox.Show("桌宠已卸载。", "奥特曼桌宠", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception e) { MessageBox.Show(e.Message, "卸载未完成", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                return;
            }
            bool removal = Array.IndexOf(args, "--uninstall") >= 0 ||
                String.Equals(Path.GetFileName(Assembly.GetExecutingAssembly().Location), "Uninstall.exe", StringComparison.OrdinalIgnoreCase);
            Application.Run(new SetupWindow(removal));
        }
    }
}
