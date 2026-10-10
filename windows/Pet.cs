// SPDX-License-Identifier: MIT
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Ultraman Desktop Pet")]
[assembly: AssemblyDescription("Offline animated desktop companion")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace UltramanPet
{
    public static class PetMath
    {
        public static readonly int[] FrameCounts = { 6, 8, 8, 4, 5, 8, 6, 6, 6 };
        public static int LookIndex(double dx, double dy, double deadzone)
        {
            if (Double.IsNaN(dx) || Double.IsNaN(dy) || Double.IsInfinity(dx) || Double.IsInfinity(dy)
                || Double.IsNaN(deadzone) || Double.IsInfinity(deadzone) || deadzone < 0)
                throw new ArgumentException("Invalid pointer vector");
            if (Math.Sqrt(dx * dx + dy * dy) <= deadzone) return -1;
            double angle = (Math.Atan2(dx, -dy) * 180.0 / Math.PI + 360.0) % 360.0;
            return ((int)Math.Floor((angle + 11.25) / 22.5)) % 16;
        }
        public static Point LookCell(int index)
        {
            if (index < 0 || index >= 16) throw new ArgumentOutOfRangeException("index");
            return new Point(index % 8, 9 + index / 8);
        }
        public static Point Clamp(Point position, Size size, Rectangle area)
        {
            return new Point(Math.Max(area.Left, Math.Min(position.X, Math.Max(area.Left, area.Right - size.Width))),
                Math.Max(area.Top, Math.Min(position.Y, Math.Max(area.Top, area.Bottom - size.Height))));
        }
        public static int Frame(int row, long elapsed)
        {
            if (row < 0 || row >= FrameCounts.Length || elapsed < 0) throw new ArgumentOutOfRangeException();
            return (int)((elapsed / 140) % FrameCounts[row]);
        }
    }

    public sealed class Atlas : IDisposable
    {
        private Bitmap[,] frames = new Bitmap[11, 8];
        public Atlas(int width)
        {
            using (Stream stream = typeof(Atlas).Assembly.GetManifestResourceStream("PetAtlas"))
            using (Bitmap source = new Bitmap(stream))
            {
                if (source.Width != 1536 || source.Height != 2288) throw new InvalidDataException("Unexpected atlas layout");
                for (int row = 0; row < 11; row++)
                {
                    int count = row < 9 ? PetMath.FrameCounts[row] : 8;
                    for (int column = 0; column < count; column++)
                    {
                        Bitmap frame = new Bitmap(width, width * 208 / 192, PixelFormat.Format32bppPArgb);
                        using (Graphics g = Graphics.FromImage(frame))
                        {
                            g.CompositingMode = CompositingMode.SourceCopy;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            using (ImageAttributes attributes = new ImageAttributes())
                            {
                                attributes.SetWrapMode(WrapMode.TileFlipXY);
                                g.DrawImage(source, new Rectangle(0, 0, frame.Width, frame.Height),
                                    column * 192, row * 208, 192, 208, GraphicsUnit.Pixel, attributes);
                            }
                        }
                        frames[row, column] = frame;
                    }
                }
            }
        }
        public Bitmap Get(int row, int column)
        {
            if (row < 0 || row >= 11 || column < 0 || column >= 8 || frames[row, column] == null)
                throw new ArgumentOutOfRangeException("cell");
            return frames[row, column];
        }
        public void Dispose() { foreach (Bitmap frame in frames) if (frame != null) frame.Dispose(); }
    }

    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)] internal struct XY { public int x, y; public XY(int a, int b) { x = a; y = b; } }
        [StructLayout(LayoutKind.Sequential)] internal struct WH { public int w, h; public WH(int a, int b) { w = a; h = b; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] internal struct Blend { public byte op, flags, alpha, format; }
        [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr obj);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool UpdateLayeredWindow(IntPtr window,
            IntPtr screenDC, ref XY position, ref WH size, IntPtr sourceDC, ref XY source, uint key, ref Blend blend, uint flags);
        [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
        internal static void SetDpi()
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); }
            catch (EntryPointNotFoundException) { SetProcessDPIAware(); }
        }
        internal static void Render(Form window, Bitmap frame)
        {
            IntPtr screen = GetDC(IntPtr.Zero), memory = IntPtr.Zero, bitmap = IntPtr.Zero, previous = IntPtr.Zero;
            try
            {
                memory = CreateCompatibleDC(screen);
                bitmap = frame.GetHbitmap(Color.FromArgb(0));
                previous = SelectObject(memory, bitmap);
                XY position = new XY(window.Left, window.Top), origin = new XY(0, 0);
                WH size = new WH(frame.Width, frame.Height);
                Blend blend = new Blend { op = 0, flags = 0, alpha = 255, format = 1 };
                if (!UpdateLayeredWindow(window.Handle, screen, ref position, ref size, memory, ref origin, 0, ref blend, 2))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (previous != IntPtr.Zero) SelectObject(memory, previous);
                if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
            }
        }
    }

    public sealed class PetWindow : Form
    {
        private Atlas atlas;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private readonly Random random = new Random();
        private readonly bool smoke;
        private readonly string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UltramanDesktopPet", "position.txt");
        private int displayWidth = 144, actionRow, lastRow = -1, lastColumn = -1;
        private long actionStart, actionUntil, nextIdle = 16000, nextGreeting, lastClick = -10000;
        private bool nearBefore, dragging, moved, follow = true, autoWalk, recovering;
        private Point downCursor, downPosition, walkStart, lastClickPosition;
        private NotifyIcon tray;
        private ContextMenuStrip menu;
        public PetWindow(bool smokeTest)
        {
            smoke = smokeTest;
            Text = "Ultraman Desktop Pet";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = smoke;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            if (!smoke) LoadSettings();
            atlas = new Atlas(displayWidth);
            ClientSize = atlas.Get(0, 0).Size;
            Rectangle working = Screen.PrimaryScreen.WorkingArea;
            if (!recovering) Location = new Point(working.Right - Width - 32, working.Bottom - Height - 12);
            if (smoke) Location = new Point(working.Left + (working.Width - Width) / 2, working.Top + (working.Height - Height) / 2);
            EnsureVisible();
            BuildMenu();
            tray = new NotifyIcon { Icon = Icon, Text = "奥特曼桌宠 · 右键打开菜单", ContextMenuStrip = menu, Visible = true };
            tray.DoubleClick += delegate { ResetPosition(); Play(3); };
            MouseDown += OnDown;
            MouseMove += OnMove;
            MouseUp += OnUp;
            timer.Interval = 35;
            timer.Tick += Tick;
            Shown += delegate { DrawCell(0, 0); timer.Start(); };
        }
        protected override bool ShowWithoutActivation { get { return !smoke; } }
        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x80000 | (smoke ? 0x40000 : 0x80 | 0x08000000); return cp; }
        }
        protected override void WndProc(ref Message message)
        {
            if (!smoke && message.Msg == 0x21) { message.Result = new IntPtr(3); return; }
            base.WndProc(ref message);
        }
        private void BuildMenu()
        {
            menu = new ContextMenuStrip();
            menu.Items.Add("奥特曼桌宠", null, delegate { Play(3); });
            ToolStripMenuItem actions = new ToolStripMenuItem("播放动作");
            string[] labels = { "待机", "向右跑", "向左跑", "挥手", "跳跃", "失落", "等待", "思考", "检查" };
            for (int row = 0; row < labels.Length; row++)
            {
                int selected = row;
                actions.DropDownItems.Add(labels[row], null, delegate { Play(selected); });
            }
            menu.Items.Add(actions);
            ToolStripMenuItem sizes = new ToolStripMenuItem("大小");
            int[] widths = { 96, 144, 192 };
            string[] names = { "小", "中", "大" };
            for (int i = 0; i < widths.Length; i++)
            {
                int selected = widths[i];
                sizes.DropDownItems.Add(names[i], null, delegate { ChangeSize(selected); });
            }
            menu.Items.Add(sizes);
            ToolStripMenuItem gaze = new ToolStripMenuItem("看向鼠标") { Checked = true, CheckOnClick = true };
            gaze.CheckedChanged += delegate { follow = gaze.Checked; };
            menu.Items.Add(gaze);
            ToolStripMenuItem walking = new ToolStripMenuItem("自动走动") { CheckOnClick = true };
            walking.CheckedChanged += delegate { autoWalk = walking.Checked; };
            menu.Items.Add(walking);
            menu.Items.Add("回到屏幕右下角", null, delegate { ResetPosition(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("使用说明", null, delegate { MessageBox.Show("鼠标靠近：挥手、看向鼠标\n单击：跳跃\n双击：挥手\n按住左键：拖动\n右键：切换动作、大小或退出\n\n程序原创代码采用 MIT；奥特曼同人图片的第三方权利另有说明。\nhttps://github.com/412ccc/ultraman-desktop-pet", "奥特曼桌宠", MessageBoxButtons.OK, MessageBoxIcon.Information); });
            menu.Items.Add("退出桌宠", null, delegate { Close(); });
        }
        private void OnDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            dragging = true; moved = false; Capture = true;
            downCursor = Cursor.Position; downPosition = Location;
            actionUntil = 0;
        }
        private void OnMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            Point cursor = Cursor.Position;
            if (Math.Abs(cursor.X - downCursor.X) + Math.Abs(cursor.Y - downCursor.Y) > 5) moved = true;
            if (moved) Location = new Point(downPosition.X + cursor.X - downCursor.X, downPosition.Y + cursor.Y - downCursor.Y);
        }
        private void OnUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) { menu.Show(Cursor.Position); return; }
            if (e.Button != MouseButtons.Left || !dragging) return;
            dragging = false; Capture = false;
            if (moved) { EnsureVisible(); SaveSettings(); }
            else
            {
                long now = clock.ElapsedMilliseconds; Point cursor = Cursor.Position;
                bool second = now - lastClick <= SystemInformation.DoubleClickTime &&
                    Math.Abs(cursor.X - lastClickPosition.X) <= SystemInformation.DoubleClickSize.Width &&
                    Math.Abs(cursor.Y - lastClickPosition.Y) <= SystemInformation.DoubleClickSize.Height;
                lastClick = now; lastClickPosition = cursor; Play(second ? 3 : 4);
            }
        }
        private void Play(int row)
        {
            actionRow = row; actionStart = clock.ElapsedMilliseconds; walkStart = Location;
            actionUntil = actionStart + PetMath.FrameCounts[row] * 140L * 2;
            nextIdle = actionUntil + 15000 + random.Next(8000);
        }
        private void Tick(object sender, EventArgs args)
        {
            long now = clock.ElapsedMilliseconds;
            if (smoke && now >= 60000) { Close(); return; }
            Point cursor = Cursor.Position;
            double dx = cursor.X - (Left + Width / 2.0), dy = cursor.Y - (Top + Height * 0.30);
            bool nearby = Math.Abs(dx) < Width * 1.7 && Math.Abs(dy) < Height * 1.7;
            bool menuOpen = menu.Visible;
            if (nearby && !nearBefore && now >= nextGreeting && !dragging && !menuOpen)
            {
                Play(3); nextGreeting = now + 7000;
            }
            nearBefore = nearby;
            if (!dragging && !menuOpen && !nearby && now >= nextIdle && now >= actionUntil)
            {
                int[] ambient = autoWalk ? new int[] { 1, 2, 3, 6, 7, 8 } : new int[] { 3, 6, 7, 8 };
                Play(ambient[random.Next(ambient.Length)]);
            }
            int row = 0, column = PetMath.Frame(0, now);
            if (!dragging && now < actionUntil)
            {
                row = actionRow; column = PetMath.Frame(row, now - actionStart);
                if ((row == 1 || row == 2) && !menuOpen)
                {
                    int step = (int)((now - actionStart) * 0.04) * (row == 1 ? 1 : -1);
                    Location = PetMath.Clamp(new Point(walkStart.X + step, walkStart.Y), Size, Screen.FromPoint(walkStart).WorkingArea);
                }
            }
            else if (!dragging && follow && nearby && !menuOpen)
            {
                int index = PetMath.LookIndex(dx, dy, Math.Max(8, Width / 10.0));
                Point cell = index < 0 ? Point.Empty : PetMath.LookCell(index);
                row = cell.Y; column = cell.X;
            }
            DrawCell(row, column);
        }
        private void DrawCell(int row, int column)
        {
            if (row == lastRow && column == lastColumn) return;
            Native.Render(this, atlas.Get(row, column)); lastRow = row; lastColumn = column;
            if (smoke) Text = "Ultraman Desktop Pet Preview - " + row + " / " + column;
        }
        private void ChangeSize(int width)
        {
            Atlas replacement = new Atlas(width);
            Atlas old = atlas; atlas = replacement; displayWidth = width;
            ClientSize = atlas.Get(0, 0).Size; lastRow = -1;
            EnsureVisible(); DrawCell(0, 0); old.Dispose(); SaveSettings();
        }
        private void EnsureVisible()
        {
            bool visible = false;
            foreach (Screen screen in Screen.AllScreens) if (screen.WorkingArea.IntersectsWith(Bounds)) { visible = true; break; }
            Rectangle area = visible ? Screen.FromControl(this).WorkingArea : Screen.PrimaryScreen.WorkingArea;
            Location = PetMath.Clamp(Location, Size, area);
        }
        private void ResetPosition()
        {
            Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(area.Right - Width - 32, area.Bottom - Height - 12);
            actionUntil = 0; SaveSettings();
        }
        private void LoadSettings()
        {
            try
            {
                string[] values = File.ReadAllText(settings).Split(','); int x, y, width;
                if (values.Length == 3 && Int32.TryParse(values[0], out x) && Int32.TryParse(values[1], out y)
                    && Int32.TryParse(values[2], out width) && (width == 96 || width == 144 || width == 192))
                { Location = new Point(x, y); displayWidth = width; recovering = true; }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        private void SaveSettings()
        {
            if (smoke) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(settings)); File.WriteAllText(settings, Left + "," + Top + "," + displayWidth); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timer.Stop(); timer.Dispose(); SaveSettings();
            tray.Visible = false; tray.Dispose(); menu.Dispose(); atlas.Dispose(); base.OnFormClosed(e);
        }
    }
    internal static class Program
    {
        [STAThread] private static void Main(string[] args)
        {
            bool smoke = Array.IndexOf(args, "--smoke-test") >= 0 ||
                Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location).EndsWith("Preview", StringComparison.OrdinalIgnoreCase);
            Native.SetDpi(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            bool created;
            using (Mutex mutex = new Mutex(true, "Local\\UltramanDesktopPet.412ccc" + (smoke ? ".smoke" : ""), out created))
            {
                if (!created) { MessageBox.Show("桌宠已经在运行，请在桌宠或托盘图标上点右键。", "奥特曼桌宠"); return; }
                try { using (PetWindow pet = new PetWindow(smoke)) Application.Run(pet); }
                catch (Exception e) { MessageBox.Show("桌宠启动失败：" + e.Message, "奥特曼桌宠", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }
}
