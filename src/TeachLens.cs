using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("TeachLens")]
[assembly: AssemblyDescription("Pointer highlight, spotlight and local magnifier for teaching")]
[assembly: AssemblyCompany("Vũ Hữu Thành")]
[assembly: AssemblyProduct("TeachLens")]
[assembly: AssemblyCopyright("Copyright © Vũ Hữu Thành 2026")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace TeachLens
{
    internal enum LensShapeMode
    {
        Circle,
        RoundedRectangle
    }

    internal enum UiLanguage
    {
        English,
        Vietnamese
    }

    internal static class Program
    {
        private static Mutex _singleInstance;

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                NativeMethods.SetProcessDpiAwarenessContext(new IntPtr(-4));
            }
            catch
            {
                // The manifest provides the same setting on supported Windows versions.
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0 && String.Equals(args[0], "--self-test", StringComparison.OrdinalIgnoreCase))
            {
                string reportPath = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "TeachLens-self-test.txt");
                return SelfTest.Run(reportPath);
            }

            bool createdNew;
            _singleInstance = new Mutex(true, "Local\\TeachLens.Singleton.v1", out createdNew);
            if (!createdNew)
            {
                UiLanguage language = AppSettings.Load().Language;
                MessageBox.Show(
                    UiText.Get(language, "singleInstance"),
                    "TeachLens",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return 0;
            }

            try
            {
                Application.Run(new MainForm());
                return 0;
            }
            finally
            {
                if (_singleInstance != null)
                {
                    _singleInstance.ReleaseMutex();
                    _singleInstance.Dispose();
                    _singleInstance = null;
                }
            }
        }
    }

    internal sealed class AppSettings
    {
        public UiLanguage Language = UiLanguage.English;
        public float ZoomFactor = 2.0f;
        public int LensWidth = 400;
        public int LensHeight = 400;
        public LensShapeMode LensShape = LensShapeMode.Circle;
        public int HighlightRadius = 34;
        public int SpotlightRadius = 155;
        public double SpotlightOpacity = 0.58;

        public static string SettingsDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TeachLens"); }
        }

        public static string SettingsPath
        {
            get { return Path.Combine(SettingsDirectory, "settings.ini"); }
        }

        public static AppSettings Load()
        {
            AppSettings result = new AppSettings();
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return result;
                }

                string[] lines = File.ReadAllLines(SettingsPath, Encoding.UTF8);
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int equals = line.IndexOf('=');
                    if (equals < 1)
                    {
                        continue;
                    }

                    string key = line.Substring(0, equals).Trim();
                    string value = line.Substring(equals + 1).Trim();
                    float floatValue;
                    int intValue;
                    double doubleValue;

                    if (key == "Language")
                    {
                        result.Language = String.Equals(value, "vi", StringComparison.OrdinalIgnoreCase)
                            || String.Equals(value, "Vietnamese", StringComparison.OrdinalIgnoreCase)
                            ? UiLanguage.Vietnamese
                            : UiLanguage.English;
                    }
                    else if (key == "ZoomFactor" && Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                    {
                        result.ZoomFactor = Clamp(floatValue, 1.25f, 4.0f);
                    }
                    else if (key == "LensWidth" && Int32.TryParse(value, out intValue))
                    {
                        result.LensWidth = Clamp(intValue, 280, 800);
                    }
                    else if (key == "LensHeight" && Int32.TryParse(value, out intValue))
                    {
                        result.LensHeight = Clamp(intValue, 180, 600);
                    }
                    else if (key == "LensShape")
                    {
                        result.LensShape = String.Equals(value, "RoundedRectangle", StringComparison.OrdinalIgnoreCase)
                            ? LensShapeMode.RoundedRectangle
                            : LensShapeMode.Circle;
                    }
                    else if (key == "HighlightRadius" && Int32.TryParse(value, out intValue))
                    {
                        result.HighlightRadius = Clamp(intValue, 18, 80);
                    }
                    else if (key == "SpotlightRadius" && Int32.TryParse(value, out intValue))
                    {
                        result.SpotlightRadius = Clamp(intValue, 70, 360);
                    }
                    else if (key == "SpotlightOpacity" && Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue))
                    {
                        result.SpotlightOpacity = Clamp(doubleValue, 0.25, 0.85);
                    }
                }
            }
            catch
            {
                // Corrupt or inaccessible settings should never prevent the utility from starting.
            }

            if (result.LensShape == LensShapeMode.Circle && result.LensWidth != result.LensHeight)
            {
                result.LensWidth = 400;
                result.LensHeight = 400;
            }
            result.HighlightRadius = 34;

            return result;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                string[] lines = new string[]
                {
                    "# TeachLens settings",
                    "Language=" + (Language == UiLanguage.Vietnamese ? "vi" : "en"),
                    "ZoomFactor=" + ZoomFactor.ToString("0.00", CultureInfo.InvariantCulture),
                    "LensWidth=" + LensWidth.ToString(CultureInfo.InvariantCulture),
                    "LensHeight=" + LensHeight.ToString(CultureInfo.InvariantCulture),
                    "LensShape=" + LensShape.ToString(),
                    "HighlightRadius=" + HighlightRadius.ToString(CultureInfo.InvariantCulture),
                    "SpotlightRadius=" + SpotlightRadius.ToString(CultureInfo.InvariantCulture),
                    "SpotlightOpacity=" + SpotlightOpacity.ToString("0.00", CultureInfo.InvariantCulture)
                };
                File.WriteAllLines(SettingsPath, lines, Encoding.UTF8);
            }
            catch
            {
                // Settings persistence is best-effort.
            }
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }

    internal static class UiText
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            { "singleInstance", "TeachLens is already running. Look for its icon in the system tray." },
            { "language", "Language" },
            { "slogan", "Make every pixel teach." },
            { "actions", "Quick actions" },
            { "highlight", "Pointer highlight" },
            { "spotlight", "Spotlight" },
            { "magnifier", "Magnifier" },
            { "off", "Turn off all  ·  Ctrl + Alt + 0" },
            { "settings", "Settings" },
            { "shape", "Lens shape" },
            { "shapeCircle", "Circle · recommended" },
            { "shapeRounded", "Rounded rectangle" },
            { "zoom", "Zoom" },
            { "zoomHint", "Ctrl + Alt + ↑ / ↓ to change zoom while teaching" },
            { "lensSize", "Lens size" },
            { "spotlightArea", "Spotlight area" },
            { "darkness", "Background dimness" },
            { "small", "Small" },
            { "medium", "Medium" },
            { "large", "Large" },
            { "about", "About" },
            { "hide", "Hide to tray" },
            { "credit", "Vũ Hữu Thành  ·  Engineering assistance: OpenAI Codex" },
            { "trayOpen", "Open TeachLens" },
            { "trayHighlight", "Toggle pointer highlight   Ctrl+Alt+1" },
            { "traySpotlight", "Toggle spotlight   Ctrl+Alt+2" },
            { "trayMagnifier", "Toggle magnifier   {0}" },
            { "trayOff", "Turn everything off   Ctrl+Alt+0" },
            { "trayAbout", "About TeachLens" },
            { "trayExit", "Exit" },
            { "idle", "Ready — use shortcuts from any app." },
            { "active", "Active: " },
            { "pressEsc", "Press Esc to turn off the most recently enabled feature." },
            { "warning", "Note: " },
            { "magnifierFallback", "Ctrl+Alt+M is busy; magnifier moved to Ctrl+Alt+L" },
            { "magnifierBothBusy", "Ctrl+Alt+M and Ctrl+Alt+L are busy" },
            { "hotkeyBusy", "{0} is busy" },
            { "escapeBusy", "Esc could not be registered" },
            { "magnifierError", "The magnifier could not start on this Windows configuration.\n\n{0}" },
            { "magControlCreateFailed", "Could not create the Windows magnifier control. Windows error: {0}" },
            { "magTransformFailed", "Could not apply the magnifier zoom transform." },
            { "aboutTitle", "About TeachLens" },
            { "trayBalloonTitle", "TeachLens is still running" },
            { "trayBalloonBody", "Use shortcuts to enable features; press Esc to turn off the most recently enabled feature." }
        };

        private static readonly Dictionary<string, string> Vietnamese = new Dictionary<string, string>
        {
            { "singleInstance", "TeachLens đang chạy. Hãy tìm biểu tượng TeachLens ở khay hệ thống." },
            { "language", "Ngôn ngữ" },
            { "slogan", "Biến từng điểm ảnh thành bài giảng." },
            { "actions", "Chức năng nhanh" },
            { "highlight", "Làm nổi bật con trỏ" },
            { "spotlight", "Spotlight" },
            { "magnifier", "Kính lúp" },
            { "off", "Tắt tất cả  ·  Ctrl + Alt + 0" },
            { "settings", "Thiết lập" },
            { "shape", "Hình dạng kính" },
            { "shapeCircle", "Hình tròn · khuyên dùng" },
            { "shapeRounded", "Chữ nhật bo góc" },
            { "zoom", "Mức phóng" },
            { "zoomHint", "Ctrl + Alt + ↑ / ↓ để đổi nhanh khi đang dạy" },
            { "lensSize", "Kích thước kính" },
            { "spotlightArea", "Vùng spotlight" },
            { "darkness", "Độ tối nền" },
            { "small", "Nhỏ" },
            { "medium", "Vừa" },
            { "large", "Lớn" },
            { "about", "Giới thiệu" },
            { "hide", "Ẩn xuống khay" },
            { "credit", "Vũ Hữu Thành  ·  Hỗ trợ kỹ thuật: OpenAI Codex" },
            { "trayOpen", "Mở TeachLens" },
            { "trayHighlight", "Bật/tắt highlight   Ctrl+Alt+1" },
            { "traySpotlight", "Bật/tắt spotlight   Ctrl+Alt+2" },
            { "trayMagnifier", "Bật/tắt kính lúp   {0}" },
            { "trayOff", "Tắt tất cả   Ctrl+Alt+0" },
            { "trayAbout", "Giới thiệu TeachLens" },
            { "trayExit", "Thoát" },
            { "idle", "Đang chờ — dùng phím tắt ở bất kỳ ứng dụng nào." },
            { "active", "Đang bật: " },
            { "pressEsc", "Nhấn Esc để tắt chức năng vừa bật." },
            { "warning", "Lưu ý: " },
            { "magnifierFallback", "Ctrl+Alt+M đang bận; kính lúp tự chuyển sang Ctrl+Alt+L" },
            { "magnifierBothBusy", "Ctrl+Alt+M và Ctrl+Alt+L đang bận" },
            { "hotkeyBusy", "{0} đang bận" },
            { "escapeBusy", "Không thể đăng ký phím Esc" },
            { "magnifierError", "Không thể bật kính lúp trên cấu hình Windows này.\n\n{0}" },
            { "magControlCreateFailed", "Không thể tạo bộ điều khiển kính lúp của Windows. Mã lỗi Windows: {0}" },
            { "magTransformFailed", "Không thể áp dụng hệ số phóng đại cho kính lúp." },
            { "aboutTitle", "Giới thiệu TeachLens" },
            { "trayBalloonTitle", "TeachLens vẫn đang chạy" },
            { "trayBalloonBody", "Dùng phím tắt để bật tính năng; nhấn Esc để tắt chức năng vừa bật." }
        };

        public static string Get(UiLanguage language, string key)
        {
            Dictionary<string, string> source = language == UiLanguage.Vietnamese ? Vietnamese : English;
            string value;
            return source.TryGetValue(key, out value) ? value : key;
        }
    }

    internal sealed class LensPlacement
    {
        public Rectangle Destination;
        public NativeMethods.RECT Source;
    }

    internal static class LensGeometry
    {
        public static LensPlacement Compute(Rectangle monitorBounds, Point cursor, Size lensSize, int border, float factor)
        {
            int destinationWidth = Math.Min(lensSize.Width, monitorBounds.Width);
            int destinationHeight = Math.Min(lensSize.Height, monitorBounds.Height);
            int contentWidth = Math.Max(1, destinationWidth - border * 2);
            int contentHeight = Math.Max(1, destinationHeight - border * 2);
            int sourceWidth = Math.Min(monitorBounds.Width, Math.Max(1, (int)Math.Ceiling(contentWidth / factor)));
            int sourceHeight = Math.Min(monitorBounds.Height, Math.Max(1, (int)Math.Ceiling(contentHeight / factor)));

            int destinationLeft = Clamp(cursor.X - destinationWidth / 2, monitorBounds.Left, monitorBounds.Right - destinationWidth);
            int destinationTop = Clamp(cursor.Y - destinationHeight / 2, monitorBounds.Top, monitorBounds.Bottom - destinationHeight);
            int sourceLeft = Clamp(cursor.X - sourceWidth / 2, monitorBounds.Left, monitorBounds.Right - sourceWidth);
            int sourceTop = Clamp(cursor.Y - sourceHeight / 2, monitorBounds.Top, monitorBounds.Bottom - sourceHeight);

            LensPlacement placement = new LensPlacement();
            placement.Destination = new Rectangle(destinationLeft, destinationTop, destinationWidth, destinationHeight);
            placement.Source = new NativeMethods.RECT(
                sourceLeft,
                sourceTop,
                sourceLeft + sourceWidth,
                sourceTop + sourceHeight);
            return placement;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (maximum < minimum)
            {
                return minimum;
            }
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }

    internal abstract class ClickThroughForm : Form
    {
        protected ClickThroughForm()
        {
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW;
                cp.ExStyle |= NativeMethods.WS_EX_NOACTIVATE;
                cp.ExStyle |= NativeMethods.WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_NCHITTEST)
            {
                m.Result = new IntPtr(NativeMethods.HTTRANSPARENT);
                return;
            }
            base.WndProc(ref m);
        }

        public void KeepOnTop()
        {
            if (IsHandleCreated && Visible)
            {
                NativeMethods.SetWindowPos(
                    Handle,
                    NativeMethods.HWND_TOPMOST,
                    0,
                    0,
                    0,
                    0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
            }
        }
    }

    internal sealed class HighlightForm : ClickThroughForm
    {
        private int _clickStartedAt;
        private bool _clickAnimation;

        public HighlightForm()
        {
            BackColor = Color.Black;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_LAYERED;
                return cp;
            }
        }

        public void UpdateAt(Point cursor, int radius)
        {
            int animationPadding = _clickAnimation ? 28 : 12;
            int diameter = radius * 2 + animationPadding * 2;
            using (Bitmap bitmap = new Bitmap(diameter, diameter, PixelFormat.Format32bppPArgb))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.CompositingMode = CompositingMode.SourceCopy;

                float cx = diameter / 2.0f;
                float cy = diameter / 2.0f;
                float ringThickness = Math.Max(12.0f, radius * 0.42f);
                RectangleF ring = new RectangleF(cx - radius, cy - radius, radius * 2.0f, radius * 2.0f);

                using (Pen softEdge = new Pen(Color.FromArgb(70, 16, 92, 52), ringThickness + 5.0f))
                using (Pen greenRing = new Pen(Color.FromArgb(155, 48, 214, 105), ringThickness))
                using (Pen innerEdge = new Pen(Color.FromArgb(205, 23, 126, 68), 2.5f))
                {
                    graphics.DrawEllipse(softEdge, ring);
                    graphics.DrawEllipse(greenRing, ring);
                    RectangleF inner = RectangleF.Inflate(ring, -ringThickness / 2.0f, -ringThickness / 2.0f);
                    graphics.DrawEllipse(innerEdge, inner);
                }

                if (_clickAnimation)
                {
                    int elapsed = Math.Max(0, unchecked(Environment.TickCount - _clickStartedAt));
                    float progress = Math.Min(1.0f, elapsed / 300.0f);
                    float extra = 7.0f + progress * 23.0f;
                    int alpha = (int)(220.0f * (1.0f - progress));
                    RectangleF pulse = new RectangleF(
                        cx - radius - extra,
                        cy - radius - extra,
                        (radius + extra) * 2.0f,
                        (radius + extra) * 2.0f);
                    using (Pen clickPen = new Pen(Color.FromArgb(alpha, 126, 245, 165), 4.0f))
                    {
                        graphics.DrawEllipse(clickPen, pulse);
                    }
                }

                NativeMethods.UpdateLayeredBitmap(
                    Handle,
                    bitmap,
                    new Point(cursor.X - diameter / 2, cursor.Y - diameter / 2));
            }
        }

        public void TriggerClick()
        {
            _clickStartedAt = Environment.TickCount;
            _clickAnimation = true;
        }

        public void AdvanceAnimation()
        {
            if (_clickAnimation && unchecked(Environment.TickCount - _clickStartedAt) > 280)
            {
                _clickAnimation = false;
            }
        }
    }

    internal sealed class SpotlightForm : ClickThroughForm
    {
        private Rectangle _monitorBounds;
        private Point _lastLocalPoint = new Point(Int32.MinValue, Int32.MinValue);
        private int _lastRadius = -1;

        public SpotlightForm(double opacity)
        {
            BackColor = Color.Black;
            Opacity = opacity;
        }

        public void SetOpacity(double opacity)
        {
            Opacity = Math.Max(0.25, Math.Min(0.85, opacity));
        }

        public void UpdateAt(Rectangle monitorBounds, Point cursor, int radius)
        {
            if (_monitorBounds != monitorBounds)
            {
                _monitorBounds = monitorBounds;
                Bounds = monitorBounds;
                _lastLocalPoint = new Point(Int32.MinValue, Int32.MinValue);
            }

            Point local = new Point(cursor.X - monitorBounds.Left, cursor.Y - monitorBounds.Top);
            if (local == _lastLocalPoint && radius == _lastRadius)
            {
                return;
            }

            _lastLocalPoint = local;
            _lastRadius = radius;
            Region nextRegion = new Region(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)));
            using (GraphicsPath opening = new GraphicsPath())
            {
                opening.AddEllipse(local.X - radius, local.Y - radius, radius * 2, radius * 2);
                nextRegion.Exclude(opening);
            }
            Region previous = Region;
            Region = nextRegion;
            if (previous != null)
            {
                previous.Dispose();
            }
        }
    }

    internal sealed class MagnifierForm : ClickThroughForm
    {
        private const int BorderSize = 5;
        private IntPtr _magnifierHandle = IntPtr.Zero;
        private float _factor = 2.0f;
        private Size _requestedSize = new Size(400, 400);
        private LensShapeMode _shape = LensShapeMode.Circle;

        public bool IsReady
        {
            get { return _magnifierHandle != IntPtr.Zero; }
        }

        public MagnifierForm()
        {
            BackColor = Color.FromArgb(48, 203, 105);
            ClientSize = _requestedSize;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_LAYERED;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.SetLayeredWindowAttributes(Handle, 0, 255, NativeMethods.LWA_ALPHA);
            _magnifierHandle = NativeMethods.CreateWindowEx(
                0,
                "Magnifier",
                "TeachLensMagnifierControl",
                NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE,
                BorderSize,
                BorderSize,
                Math.Max(1, ClientSize.Width - BorderSize * 2),
                Math.Max(1, ClientSize.Height - BorderSize * 2),
                Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (_magnifierHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("MAG_CONTROL_CREATE:" + Marshal.GetLastWin32Error().ToString(CultureInfo.InvariantCulture));
            }

            ApplyFactor();
            ApplyShapeRegion();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_magnifierHandle != IntPtr.Zero)
            {
                NativeMethods.DestroyWindow(_magnifierHandle);
                _magnifierHandle = IntPtr.Zero;
            }
            base.OnHandleDestroyed(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_magnifierHandle != IntPtr.Zero)
            {
                NativeMethods.MoveWindow(
                    _magnifierHandle,
                    BorderSize,
                    BorderSize,
                    Math.Max(1, ClientSize.Width - BorderSize * 2),
                    Math.Max(1, ClientSize.Height - BorderSize * 2),
                    true);
            }
            ApplyShapeRegion();
        }

        public void SetLensSize(Size size)
        {
            _requestedSize = size;
            ClientSize = size;
        }

        public void SetShape(LensShapeMode shape)
        {
            _shape = shape;
            ApplyShapeRegion();
        }

        public void SetFactor(float factor)
        {
            _factor = Math.Max(1.25f, Math.Min(4.0f, factor));
            ApplyFactor();
        }

        public void SetExcludedWindows(IntPtr[] windows)
        {
            if (_magnifierHandle == IntPtr.Zero || windows == null || windows.Length == 0)
            {
                return;
            }
            NativeMethods.MagSetWindowFilterList(_magnifierHandle, NativeMethods.MW_FILTERMODE_EXCLUDE, windows.Length, windows);
        }

        public LensPlacement UpdateAt(Rectangle monitorBounds, Point cursor)
        {
            LensPlacement placement = LensGeometry.Compute(monitorBounds, cursor, _requestedSize, BorderSize, _factor);
            if (Bounds != placement.Destination)
            {
                Bounds = placement.Destination;
            }
            if (_magnifierHandle != IntPtr.Zero)
            {
                NativeMethods.MagSetWindowSource(_magnifierHandle, placement.Source);
                NativeMethods.InvalidateRect(_magnifierHandle, IntPtr.Zero, true);
            }
            return placement;
        }

        private void ApplyFactor()
        {
            if (_magnifierHandle == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.MAGTRANSFORM transform = new NativeMethods.MAGTRANSFORM();
            transform.v = new float[9];
            transform.v[0] = _factor;
            transform.v[4] = _factor;
            transform.v[8] = 1.0f;
            if (!NativeMethods.MagSetWindowTransform(_magnifierHandle, ref transform))
            {
                throw new InvalidOperationException("MAG_TRANSFORM_FAILED");
            }
        }

        private void ApplyShapeRegion()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            GraphicsPath path = _shape == LensShapeMode.Circle
                ? EllipsePath(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height))
                : RoundedRectangle(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), 24);
            using (path)
            {
                Region previous = Region;
                Region = new Region(path);
                if (previous != null)
                {
                    previous.Dispose();
                }
            }

            if (_magnifierHandle != IntPtr.Zero)
            {
                int contentWidth = Math.Max(1, ClientSize.Width - BorderSize * 2);
                int contentHeight = Math.Max(1, ClientSize.Height - BorderSize * 2);
                IntPtr region = _shape == LensShapeMode.Circle
                    ? NativeMethods.CreateEllipticRgn(0, 0, contentWidth + 1, contentHeight + 1)
                    : NativeMethods.CreateRoundRectRgn(0, 0, contentWidth + 1, contentHeight + 1, 38, 38);
                if (region != IntPtr.Zero && NativeMethods.SetWindowRgn(_magnifierHandle, region, true) == 0)
                {
                    NativeMethods.DeleteObject(region);
                }
            }
        }

        private static GraphicsPath EllipsePath(Rectangle bounds)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(bounds);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Left, bounds.Top, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class OverlayController : IDisposable
    {
        private enum OverlayFeature
        {
            Highlight,
            Spotlight,
            Magnifier
        }

        private readonly AppSettings _settings;
        private readonly System.Windows.Forms.Timer _timer;
        private HighlightForm _highlight;
        private SpotlightForm _spotlight;
        private MagnifierForm _magnifier;
        private bool _highlightEnabled;
        private bool _spotlightEnabled;
        private bool _magnifierEnabled;
        private bool _lastLeftDown;
        private bool _lastRightDown;
        private bool _magnificationInitialized;
        private IntPtr _mainWindowHandle;
        private readonly List<OverlayFeature> _activeOrder = new List<OverlayFeature>();

        public event EventHandler StateChanged;

        public bool HighlightEnabled { get { return _highlightEnabled; } }
        public bool SpotlightEnabled { get { return _spotlightEnabled; } }
        public bool MagnifierEnabled { get { return _magnifierEnabled; } }
        public bool AnyEnabled { get { return _highlightEnabled || _spotlightEnabled || _magnifierEnabled; } }

        public OverlayController(AppSettings settings, IntPtr mainWindowHandle)
        {
            _settings = settings;
            _mainWindowHandle = mainWindowHandle;
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 16;
            _timer.Tick += OnTick;
            _timer.Start();
        }

        public void ToggleHighlight()
        {
            SetHighlight(!_highlightEnabled);
        }

        public void ToggleSpotlight()
        {
            SetSpotlight(!_spotlightEnabled);
        }

        public void ToggleMagnifier()
        {
            SetMagnifier(!_magnifierEnabled);
        }

        public void SetHighlight(bool enabled)
        {
            if (_highlightEnabled == enabled)
            {
                return;
            }
            _highlightEnabled = enabled;
            if (enabled)
            {
                EnsureHighlight();
                _highlight.Show();
                if (_magnifierEnabled) RefreshMagnifierFilter();
            }
            else if (_highlight != null)
            {
                _highlight.Hide();
            }
            UpdateActiveOrder(OverlayFeature.Highlight, enabled);
            RefreshZOrder();
            RaiseStateChanged();
        }

        public void SetSpotlight(bool enabled)
        {
            if (_spotlightEnabled == enabled)
            {
                return;
            }
            _spotlightEnabled = enabled;
            if (enabled)
            {
                EnsureSpotlight();
                _spotlight.Show();
                if (_magnifierEnabled) RefreshMagnifierFilter();
            }
            else if (_spotlight != null)
            {
                _spotlight.Hide();
            }
            UpdateActiveOrder(OverlayFeature.Spotlight, enabled);
            RefreshZOrder();
            RaiseStateChanged();
        }

        public void SetMagnifier(bool enabled)
        {
            if (_magnifierEnabled == enabled)
            {
                return;
            }

            if (enabled)
            {
                EnsureMagnifier();
                _magnifier.Show();
                _magnifierEnabled = true;
                RefreshMagnifierFilter();
            }
            else
            {
                _magnifierEnabled = false;
                if (_magnifier != null)
                {
                    _magnifier.Hide();
                }
            }
            UpdateActiveOrder(OverlayFeature.Magnifier, enabled);
            RefreshZOrder();
            RaiseStateChanged();
        }

        public void DismissLastActive()
        {
            if (_activeOrder.Count == 0)
            {
                return;
            }
            OverlayFeature feature = _activeOrder[_activeOrder.Count - 1];
            if (feature == OverlayFeature.Magnifier) SetMagnifier(false);
            else if (feature == OverlayFeature.Spotlight) SetSpotlight(false);
            else SetHighlight(false);
        }

        public void TurnOffAll()
        {
            _highlightEnabled = false;
            _spotlightEnabled = false;
            _magnifierEnabled = false;
            _activeOrder.Clear();
            if (_highlight != null) _highlight.Hide();
            if (_spotlight != null) _spotlight.Hide();
            if (_magnifier != null) _magnifier.Hide();
            RaiseStateChanged();
        }

        public void ApplySettings()
        {
            if (_spotlight != null)
            {
                _spotlight.SetOpacity(_settings.SpotlightOpacity);
            }
            if (_magnifier != null)
            {
                _magnifier.SetShape(_settings.LensShape);
                _magnifier.SetLensSize(new Size(_settings.LensWidth, _settings.LensHeight));
                _magnifier.SetFactor(_settings.ZoomFactor);
            }
            RefreshZOrder();
        }

        private void EnsureHighlight()
        {
            if (_highlight == null)
            {
                _highlight = new HighlightForm();
            }
        }

        private void EnsureSpotlight()
        {
            if (_spotlight == null)
            {
                _spotlight = new SpotlightForm(_settings.SpotlightOpacity);
            }
        }

        private void EnsureMagnifier()
        {
            if (!_magnificationInitialized)
            {
                if (!NativeMethods.MagInitialize())
                {
                    throw new InvalidOperationException("Windows Magnification API không khởi tạo được.");
                }
                _magnificationInitialized = true;
            }
            if (_magnifier == null)
            {
                _magnifier = new MagnifierForm();
                _magnifier.SetShape(_settings.LensShape);
                _magnifier.SetLensSize(new Size(_settings.LensWidth, _settings.LensHeight));
                _magnifier.SetFactor(_settings.ZoomFactor);
            }
        }

        private void UpdateActiveOrder(OverlayFeature feature, bool enabled)
        {
            _activeOrder.Remove(feature);
            if (enabled)
            {
                _activeOrder.Add(feature);
            }
        }

        private void RefreshZOrder()
        {
            if (_spotlightEnabled && _spotlight != null) _spotlight.KeepOnTop();
            if (_magnifierEnabled && _magnifier != null) _magnifier.KeepOnTop();
            if (_highlightEnabled && _highlight != null) _highlight.KeepOnTop();
        }

        private void RefreshMagnifierFilter()
        {
            if (_magnifier == null || !_magnifier.IsReady)
            {
                return;
            }

            List<IntPtr> handles = new List<IntPtr>();
            handles.Add(_magnifier.Handle);
            if (_highlight != null && _highlight.IsHandleCreated) handles.Add(_highlight.Handle);
            if (_spotlight != null && _spotlight.IsHandleCreated) handles.Add(_spotlight.Handle);
            if (_mainWindowHandle != IntPtr.Zero) handles.Add(_mainWindowHandle);
            _magnifier.SetExcludedWindows(handles.ToArray());
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                NativeMethods.POINT nativePoint;
                if (!NativeMethods.GetCursorPos(out nativePoint))
                {
                    return;
                }
                Point cursor = new Point(nativePoint.X, nativePoint.Y);
                Rectangle monitor = Screen.FromPoint(cursor).Bounds;

                bool leftDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0;
                bool rightDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RBUTTON) & 0x8000) != 0;
                bool clickStarted = (leftDown && !_lastLeftDown) || (rightDown && !_lastRightDown);
                _lastLeftDown = leftDown;
                _lastRightDown = rightDown;

                if (_spotlightEnabled && _spotlight != null)
                {
                    int spotlightRadius = _settings.SpotlightRadius;
                    if (_magnifierEnabled && _settings.LensShape == LensShapeMode.Circle)
                    {
                        spotlightRadius = Math.Max(spotlightRadius, _settings.LensWidth / 2 + 7);
                    }
                    _spotlight.UpdateAt(monitor, cursor, spotlightRadius);
                }

                if (_magnifierEnabled && _magnifier != null)
                {
                    _magnifier.UpdateAt(monitor, cursor);
                }

                if (_highlightEnabled && _highlight != null)
                {
                    if (clickStarted)
                    {
                        _highlight.TriggerClick();
                    }
                    _highlight.AdvanceAnimation();
                    _highlight.UpdateAt(cursor, _settings.HighlightRadius);
                }
            }
            catch
            {
                // A transient desktop or display-mode transition should not terminate the app.
            }
        }

        private void RaiseStateChanged()
        {
            EventHandler handler = StateChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
            if (_highlight != null) _highlight.Dispose();
            if (_spotlight != null) _spotlight.Dispose();
            if (_magnifier != null) _magnifier.Dispose();
            if (_magnificationInitialized)
            {
                NativeMethods.MagUninitialize();
                _magnificationInitialized = false;
            }
        }
    }

    internal sealed class MainForm : Form
    {
        private const int HotkeyHighlight = 101;
        private const int HotkeySpotlight = 102;
        private const int HotkeyMagnifier = 103;
        private const int HotkeyOff = 104;
        private const int HotkeyZoomIn = 105;
        private const int HotkeyZoomOut = 106;
        private const int HotkeyEscape = 107;

        private readonly AppSettings _settings;
        private OverlayController _controller;
        private Icon _appIcon;
        private NotifyIcon _trayIcon;
        private ToolStripMenuItem _trayOpenItem;
        private ToolStripMenuItem _trayHighlightItem;
        private ToolStripMenuItem _traySpotlightItem;
        private ToolStripMenuItem _trayMagnifierItem;
        private ToolStripMenuItem _trayOffItem;
        private ToolStripMenuItem _trayAboutItem;
        private ToolStripMenuItem _trayExitItem;
        private Button _highlightButton;
        private Button _spotlightButton;
        private Button _magnifierButton;
        private Button _offButton;
        private ComboBox _languageCombo;
        private ComboBox _zoomCombo;
        private ComboBox _lensShapeCombo;
        private ComboBox _lensSizeCombo;
        private TrackBar _spotlightSize;
        private TrackBar _darkness;
        private Label _statusLabel;
        private bool _allowClose;
        private bool _uiScaled;
        private bool _escapeRegistered;
        private bool _updatingLensControls;
        private bool _updatingLanguage;
        private string _magnifierShortcut = "Ctrl + Alt + M";
        private readonly List<int> _registeredHotkeys = new List<int>();
        private readonly List<string> _hotkeyWarnings = new List<string>();

        public MainForm()
        {
            _settings = AppSettings.Load();
            Text = "TeachLens 1.0 · Acorn";
            try
            {
                _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                _appIcon = null;
            }
            Icon = _appIcon ?? SystemIcons.Application;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(644, 634);
            MinimumSize = new Size(660, 673);
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.FromArgb(242, 247, 244);

            BuildInterface();
            CreateTrayIcon();
            ApplyLanguage();
            Shown += OnFirstShown;
            FormClosing += OnFormClosing;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ScaleInterfaceForDpi();
            RegisterGlobalHotkeys();
            _controller = new OverlayController(_settings, Handle);
            _controller.StateChanged += OnControllerStateChanged;
            UpdateStateUi();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            UnregisterGlobalHotkeys();
            if (_controller != null)
            {
                _controller.Dispose();
                _controller = null;
            }
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && _controller != null)
            {
                int id = m.WParam.ToInt32();
                if (id == HotkeyHighlight) _controller.ToggleHighlight();
                else if (id == HotkeySpotlight) _controller.ToggleSpotlight();
                else if (id == HotkeyMagnifier) _controller.ToggleMagnifier();
                else if (id == HotkeyOff) _controller.TurnOffAll();
                else if (id == HotkeyZoomIn) StepZoom(1);
                else if (id == HotkeyZoomOut) StepZoom(-1);
                else if (id == HotkeyEscape) _controller.DismissLastActive();
            }
            base.WndProc(ref m);
        }

        private void BuildInterface()
        {
            Panel header = new Panel();
            header.BackColor = Color.FromArgb(20, 47, 42);
            header.Location = new Point(0, 0);
            header.Size = new Size(644, 92);
            Controls.Add(header);

            PictureBox logo = new PictureBox();
            logo.Location = new Point(28, 23);
            logo.Size = new Size(46, 46);
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.Image = (_appIcon ?? SystemIcons.Application).ToBitmap();
            header.Controls.Add(logo);

            Label title = new Label();
            title.Text = "TeachLens";
            title.Font = new Font("Segoe UI Semibold", 20.0f, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Location = new Point(86, 14);
            header.Controls.Add(title);

            Label version = new Label();
            version.Text = "1.0 · Acorn";
            version.AutoSize = true;
            version.ForeColor = Color.FromArgb(137, 231, 170);
            version.Location = new Point(224, 29);
            header.Controls.Add(version);

            Label subtitle = new Label();
            subtitle.Tag = "slogan";
            subtitle.ForeColor = Color.FromArgb(201, 222, 215);
            subtitle.AutoSize = true;
            subtitle.Location = new Point(88, 57);
            header.Controls.Add(subtitle);

            Label languageLabel = new Label();
            languageLabel.Tag = "language";
            languageLabel.ForeColor = Color.FromArgb(201, 222, 215);
            languageLabel.AutoSize = true;
            languageLabel.Location = new Point(505, 11);
            header.Controls.Add(languageLabel);

            _languageCombo = new ComboBox();
            _languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _languageCombo.Items.AddRange(new object[] { "English", "Tiếng Việt" });
            _languageCombo.Location = new Point(505, 34);
            _languageCombo.Size = new Size(111, 28);
            _updatingLanguage = true;
            _languageCombo.SelectedIndex = _settings.Language == UiLanguage.Vietnamese ? 1 : 0;
            _updatingLanguage = false;
            _languageCombo.SelectedIndexChanged += OnLanguageChanged;
            header.Controls.Add(_languageCombo);

            GroupBox actions = new GroupBox();
            actions.Tag = "actions";
            actions.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
            actions.Location = new Point(28, 108);
            actions.Size = new Size(588, 136);
            actions.BackColor = Color.White;
            Controls.Add(actions);

            _highlightButton = CreateActionButton("highlight", "Ctrl + Alt + 1", new Point(18, 28));
            _highlightButton.Click += delegate { if (_controller != null) _controller.ToggleHighlight(); };
            actions.Controls.Add(_highlightButton);

            _spotlightButton = CreateActionButton("spotlight", "Ctrl + Alt + 2", new Point(206, 28));
            _spotlightButton.Click += delegate { if (_controller != null) _controller.ToggleSpotlight(); };
            actions.Controls.Add(_spotlightButton);

            _magnifierButton = CreateActionButton("magnifier", _magnifierShortcut, new Point(394, 28));
            _magnifierButton.Click += delegate { ToggleMagnifierSafely(); };
            actions.Controls.Add(_magnifierButton);

            _offButton = new Button();
            _offButton.Tag = "off";
            _offButton.Location = new Point(184, 96);
            _offButton.Size = new Size(220, 28);
            _offButton.FlatStyle = FlatStyle.Flat;
            _offButton.FlatAppearance.BorderColor = Color.FromArgb(190, 208, 199);
            _offButton.ForeColor = Color.FromArgb(49, 70, 62);
            _offButton.BackColor = Color.FromArgb(247, 250, 248);
            _offButton.Click += delegate { if (_controller != null) _controller.TurnOffAll(); };
            actions.Controls.Add(_offButton);

            GroupBox settingsBox = new GroupBox();
            settingsBox.Tag = "settings";
            settingsBox.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
            settingsBox.Location = new Point(28, 258);
            settingsBox.Size = new Size(588, 260);
            settingsBox.BackColor = Color.White;
            Controls.Add(settingsBox);

            AddSettingLabel(settingsBox, "shape", new Point(20, 33));
            _lensShapeCombo = new ComboBox();
            _lensShapeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _lensShapeCombo.Items.AddRange(new object[] { "Circle", "Rounded rectangle" });
            _lensShapeCombo.Location = new Point(150, 29);
            _lensShapeCombo.Size = new Size(210, 28);
            _lensShapeCombo.SelectedIndex = _settings.LensShape == LensShapeMode.Circle ? 0 : 1;
            _lensShapeCombo.SelectedIndexChanged += OnLensShapeChanged;
            settingsBox.Controls.Add(_lensShapeCombo);

            AddSettingLabel(settingsBox, "zoom", new Point(20, 76));
            _zoomCombo = new ComboBox();
            _zoomCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _zoomCombo.Items.AddRange(new object[] { "1.5×", "2.0×", "2.5×", "3.0×" });
            _zoomCombo.Location = new Point(150, 71);
            _zoomCombo.Size = new Size(104, 28);
            _zoomCombo.SelectedIndex = ZoomIndexFromFactor(_settings.ZoomFactor);
            _zoomCombo.SelectedIndexChanged += OnZoomChanged;
            settingsBox.Controls.Add(_zoomCombo);

            Label zoomHint = new Label();
            zoomHint.Tag = "zoomHint";
            zoomHint.AutoSize = true;
            zoomHint.ForeColor = Color.FromArgb(100, 108, 120);
            zoomHint.Location = new Point(274, 76);
            settingsBox.Controls.Add(zoomHint);

            AddSettingLabel(settingsBox, "lensSize", new Point(20, 118));
            _lensSizeCombo = new ComboBox();
            _lensSizeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _lensSizeCombo.Location = new Point(150, 113);
            _lensSizeCombo.Size = new Size(210, 28);
            PopulateLensSizeOptions(LensSizeIndex(_settings.LensWidth, _settings.LensShape));
            _lensSizeCombo.SelectedIndexChanged += OnLensSizeChanged;
            settingsBox.Controls.Add(_lensSizeCombo);

            AddSettingLabel(settingsBox, "spotlightArea", new Point(20, 164));
            _spotlightSize = CreateTrackBar(70, 300, _settings.SpotlightRadius, new Point(145, 153), 375);
            _spotlightSize.ValueChanged += OnSettingsTrackBarChanged;
            settingsBox.Controls.Add(_spotlightSize);

            AddSettingLabel(settingsBox, "darkness", new Point(20, 211));
            _darkness = CreateTrackBar(25, 80, (int)Math.Round(_settings.SpotlightOpacity * 100.0), new Point(145, 200), 375);
            _darkness.ValueChanged += OnSettingsTrackBarChanged;
            settingsBox.Controls.Add(_darkness);

            _statusLabel = new Label();
            _statusLabel.AutoSize = false;
            _statusLabel.Location = new Point(30, 533);
            _statusLabel.Size = new Size(350, 58);
            _statusLabel.ForeColor = Color.FromArgb(64, 75, 92);
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_statusLabel);

            Button infoButton = new Button();
            infoButton.Tag = "about";
            infoButton.Location = new Point(386, 545);
            infoButton.Size = new Size(100, 36);
            infoButton.FlatStyle = FlatStyle.Flat;
            infoButton.FlatAppearance.BorderColor = Color.FromArgb(185, 204, 195);
            infoButton.BackColor = Color.White;
            infoButton.Click += delegate { ShowAbout(); };
            Controls.Add(infoButton);

            Button hideButton = new Button();
            hideButton.Tag = "hide";
            hideButton.Location = new Point(496, 545);
            hideButton.Size = new Size(120, 36);
            hideButton.FlatStyle = FlatStyle.Flat;
            hideButton.FlatAppearance.BorderColor = Color.FromArgb(45, 168, 91);
            hideButton.BackColor = Color.FromArgb(48, 203, 105);
            hideButton.ForeColor = Color.FromArgb(15, 55, 34);
            hideButton.Click += delegate { HideToTray(); };
            Controls.Add(hideButton);

            Label author = new Label();
            author.Tag = "credit";
            author.AutoSize = true;
            author.ForeColor = Color.FromArgb(91, 112, 102);
            author.Location = new Point(30, 606);
            Controls.Add(author);
        }

        private void ScaleInterfaceForDpi()
        {
            if (_uiScaled)
            {
                return;
            }
            _uiScaled = true;
            uint dpi = NativeMethods.GetDpiForWindow(Handle);
            if (dpi == 0) dpi = 96;
            float factor = dpi / 96.0f;
            if (factor > 1.01f || factor < 0.99f)
            {
                SuspendLayout();
                Scale(new SizeF(factor, factor));
                MinimumSize = new Size((int)Math.Round(660 * factor), (int)Math.Round(673 * factor));
                ResumeLayout(true);
            }
        }

        private Button CreateActionButton(string textKey, string shortcut, Point location)
        {
            Button button = new Button();
            button.Tag = textKey;
            button.Text = UiText.Get(_settings.Language, textKey) + Environment.NewLine + shortcut;
            button.Location = location;
            button.Size = new Size(176, 60);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(184, 205, 195);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 248, 240);
            button.BackColor = Color.FromArgb(248, 251, 249);
            button.ForeColor = Color.FromArgb(32, 61, 50);
            button.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
            return button;
        }

        private static void AddSettingLabel(Control parent, string textKey, Point location)
        {
            Label label = new Label();
            label.Tag = textKey;
            label.Location = location;
            label.Size = new Size(125, 24);
            label.TextAlign = ContentAlignment.MiddleLeft;
            parent.Controls.Add(label);
        }

        private static TrackBar CreateTrackBar(int minimum, int maximum, int value, Point location, int width)
        {
            TrackBar track = new TrackBar();
            track.Minimum = minimum;
            track.Maximum = maximum;
            track.Value = Math.Max(minimum, Math.Min(maximum, value));
            track.TickStyle = TickStyle.None;
            track.Location = location;
            track.Size = new Size(width, 36);
            return track;
        }

        private void CreateTrayIcon()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            _trayOpenItem = new ToolStripMenuItem();
            _trayOpenItem.Font = new Font(_trayOpenItem.Font, FontStyle.Bold);
            _trayOpenItem.Click += delegate { ShowFromTray(); };
            menu.Items.Add(_trayOpenItem);
            menu.Items.Add(new ToolStripSeparator());

            _trayHighlightItem = new ToolStripMenuItem();
            _trayHighlightItem.Click += delegate { if (_controller != null) _controller.ToggleHighlight(); };
            menu.Items.Add(_trayHighlightItem);
            _traySpotlightItem = new ToolStripMenuItem();
            _traySpotlightItem.Click += delegate { if (_controller != null) _controller.ToggleSpotlight(); };
            menu.Items.Add(_traySpotlightItem);
            _trayMagnifierItem = new ToolStripMenuItem();
            _trayMagnifierItem.Click += delegate { ToggleMagnifierSafely(); };
            menu.Items.Add(_trayMagnifierItem);
            _trayOffItem = new ToolStripMenuItem();
            _trayOffItem.Click += delegate { if (_controller != null) _controller.TurnOffAll(); };
            menu.Items.Add(_trayOffItem);
            menu.Items.Add(new ToolStripSeparator());

            _trayAboutItem = new ToolStripMenuItem();
            _trayAboutItem.Click += delegate { ShowAbout(); };
            menu.Items.Add(_trayAboutItem);

            _trayExitItem = new ToolStripMenuItem();
            _trayExitItem.Click += delegate { ExitApplication(); };
            menu.Items.Add(_trayExitItem);

            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = _appIcon ?? SystemIcons.Application;
            _trayIcon.Text = "TeachLens";
            _trayIcon.Visible = true;
            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += delegate { ShowFromTray(); };
        }

        private void RegisterGlobalHotkeys()
        {
            uint modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT;
            RegisterHotkey(HotkeyHighlight, modifiers, Keys.D1, "Ctrl+Alt+1");
            RegisterHotkey(HotkeySpotlight, modifiers, Keys.D2, "Ctrl+Alt+2");
            if (!TryRegisterHotkey(HotkeyMagnifier, modifiers, Keys.M))
            {
                if (TryRegisterHotkey(HotkeyMagnifier, modifiers, Keys.L))
                {
                    _magnifierShortcut = "Ctrl + Alt + L";
                    _hotkeyWarnings.Add("magnifierFallback");
                }
                else
                {
                    _hotkeyWarnings.Add("magnifierBothBusy");
                }
            }
            RegisterHotkey(HotkeyOff, modifiers, Keys.D0, "Ctrl+Alt+0");
            RegisterHotkey(HotkeyZoomIn, modifiers, Keys.Up, "Ctrl+Alt+Up");
            RegisterHotkey(HotkeyZoomOut, modifiers, Keys.Down, "Ctrl+Alt+Down");
        }

        private void RegisterHotkey(int id, uint modifiers, Keys key, string description)
        {
            if (!TryRegisterHotkey(id, modifiers, key))
            {
                _hotkeyWarnings.Add("busy|" + description);
            }
        }

        private bool TryRegisterHotkey(int id, uint modifiers, Keys key)
        {
            if (!NativeMethods.RegisterHotKey(Handle, id, modifiers, (uint)key))
            {
                return false;
            }
            _registeredHotkeys.Add(id);
            return true;
        }

        private void UnregisterGlobalHotkeys()
        {
            foreach (int id in _registeredHotkeys)
            {
                NativeMethods.UnregisterHotKey(Handle, id);
            }
            _registeredHotkeys.Clear();
            _escapeRegistered = false;
        }

        private void OnFirstShown(object sender, EventArgs e)
        {
            UpdateStateUi();
        }

        private void OnControllerStateChanged(object sender, EventArgs e)
        {
            SyncEscapeHotkey();
            UpdateStateUi();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_updatingLanguage || _languageCombo.SelectedIndex < 0)
            {
                return;
            }

            _settings.Language = _languageCombo.SelectedIndex == 1
                ? UiLanguage.Vietnamese
                : UiLanguage.English;
            _settings.Save();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            Text = "TeachLens 1.0 · Acorn";
            ApplyTaggedText(Controls);

            int shapeIndex = _settings.LensShape == LensShapeMode.Circle ? 0 : 1;
            _updatingLensControls = true;
            _lensShapeCombo.Items.Clear();
            _lensShapeCombo.Items.Add(UiText.Get(_settings.Language, "shapeCircle"));
            _lensShapeCombo.Items.Add(UiText.Get(_settings.Language, "shapeRounded"));
            _lensShapeCombo.SelectedIndex = shapeIndex;
            _updatingLensControls = false;
            PopulateLensSizeOptions(LensSizeIndex(_settings.LensWidth, _settings.LensShape));
            UpdateActionButtonText();

            if (_trayOpenItem != null) _trayOpenItem.Text = UiText.Get(_settings.Language, "trayOpen");
            if (_trayHighlightItem != null) _trayHighlightItem.Text = UiText.Get(_settings.Language, "trayHighlight");
            if (_traySpotlightItem != null) _traySpotlightItem.Text = UiText.Get(_settings.Language, "traySpotlight");
            if (_trayMagnifierItem != null) _trayMagnifierItem.Text = String.Format(CultureInfo.InvariantCulture, UiText.Get(_settings.Language, "trayMagnifier"), _magnifierShortcut.Replace(" ", String.Empty));
            if (_trayOffItem != null) _trayOffItem.Text = UiText.Get(_settings.Language, "trayOff");
            if (_trayAboutItem != null) _trayAboutItem.Text = UiText.Get(_settings.Language, "trayAbout");
            if (_trayExitItem != null) _trayExitItem.Text = UiText.Get(_settings.Language, "trayExit");

            if (_controller == null)
            {
                _statusLabel.Text = UiText.Get(_settings.Language, "idle");
            }
            else
            {
                UpdateStateUi();
            }
        }

        private void ApplyTaggedText(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                string key = control.Tag as string;
                if (!String.IsNullOrEmpty(key))
                {
                    control.Text = UiText.Get(_settings.Language, key);
                }
                if (control.HasChildren)
                {
                    ApplyTaggedText(control.Controls);
                }
            }
        }

        private void UpdateActionButtonText()
        {
            if (_highlightButton != null) _highlightButton.Text = UiText.Get(_settings.Language, "highlight") + Environment.NewLine + "Ctrl + Alt + 1";
            if (_spotlightButton != null) _spotlightButton.Text = UiText.Get(_settings.Language, "spotlight") + Environment.NewLine + "Ctrl + Alt + 2";
            if (_magnifierButton != null) _magnifierButton.Text = UiText.Get(_settings.Language, "magnifier") + Environment.NewLine + _magnifierShortcut;
            if (_trayMagnifierItem != null) _trayMagnifierItem.Text = String.Format(CultureInfo.InvariantCulture, UiText.Get(_settings.Language, "trayMagnifier"), _magnifierShortcut.Replace(" ", String.Empty));
        }

        private string FormatWarning(string warning)
        {
            if (warning.StartsWith("busy|", StringComparison.Ordinal))
            {
                return String.Format(CultureInfo.InvariantCulture, UiText.Get(_settings.Language, "hotkeyBusy"), warning.Substring(5));
            }
            return UiText.Get(_settings.Language, warning);
        }

        private void SyncEscapeHotkey()
        {
            if (_controller == null)
            {
                return;
            }

            if (_controller.AnyEnabled && !_escapeRegistered)
            {
                if (TryRegisterHotkey(HotkeyEscape, NativeMethods.MOD_NOREPEAT, Keys.Escape))
                {
                    _escapeRegistered = true;
                }
                else if (!_hotkeyWarnings.Contains("escapeBusy"))
                {
                    _hotkeyWarnings.Add("escapeBusy");
                }
            }
            else if (!_controller.AnyEnabled && _escapeRegistered)
            {
                NativeMethods.UnregisterHotKey(Handle, HotkeyEscape);
                _registeredHotkeys.Remove(HotkeyEscape);
                _escapeRegistered = false;
            }
        }

        private void UpdateStateUi()
        {
            if (_controller == null)
            {
                return;
            }

            StyleActionButton(_highlightButton, _controller.HighlightEnabled);
            StyleActionButton(_spotlightButton, _controller.SpotlightEnabled);
            StyleActionButton(_magnifierButton, _controller.MagnifierEnabled);

            UpdateActionButtonText();

            List<string> active = new List<string>();
            if (_controller.HighlightEnabled) active.Add(UiText.Get(_settings.Language, "highlight").ToLowerInvariant());
            if (_controller.SpotlightEnabled) active.Add(UiText.Get(_settings.Language, "spotlight").ToLowerInvariant());
            if (_controller.MagnifierEnabled) active.Add(UiText.Get(_settings.Language, "magnifier").ToLowerInvariant() + " " + _settings.ZoomFactor.ToString("0.0", CultureInfo.InvariantCulture) + "×");
            _statusLabel.Text = active.Count == 0
                ? UiText.Get(_settings.Language, "idle")
                : UiText.Get(_settings.Language, "active") + String.Join(" · ", active.ToArray()) + Environment.NewLine + UiText.Get(_settings.Language, "pressEsc");
            if (_hotkeyWarnings.Count > 0)
            {
                List<string> warnings = new List<string>();
                foreach (string warning in _hotkeyWarnings)
                {
                    warnings.Add(FormatWarning(warning));
                }
                _statusLabel.Text += Environment.NewLine + UiText.Get(_settings.Language, "warning") + String.Join(", ", warnings.ToArray());
            }
        }

        private static void StyleActionButton(Button button, bool active)
        {
            if (button == null)
            {
                return;
            }
            button.BackColor = active ? Color.FromArgb(211, 247, 224) : Color.FromArgb(248, 251, 249);
            button.FlatAppearance.BorderColor = active ? Color.FromArgb(48, 190, 98) : Color.FromArgb(184, 205, 195);
        }

        private void ToggleMagnifierSafely()
        {
            try
            {
                if (_controller != null) _controller.ToggleMagnifier();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    String.Format(CultureInfo.InvariantCulture, UiText.Get(_settings.Language, "magnifierError"), LocalizeExceptionMessage(exception)),
                    "TeachLens",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private string LocalizeExceptionMessage(Exception exception)
        {
            if (exception.Message.StartsWith("MAG_CONTROL_CREATE:", StringComparison.Ordinal))
            {
                return String.Format(
                    CultureInfo.InvariantCulture,
                    UiText.Get(_settings.Language, "magControlCreateFailed"),
                    exception.Message.Substring("MAG_CONTROL_CREATE:".Length));
            }
            if (String.Equals(exception.Message, "MAG_TRANSFORM_FAILED", StringComparison.Ordinal))
            {
                return UiText.Get(_settings.Language, "magTransformFailed");
            }
            return exception.Message;
        }

        private void OnZoomChanged(object sender, EventArgs e)
        {
            if (_zoomCombo.SelectedIndex < 0)
            {
                return;
            }
            float[] factors = new float[] { 1.5f, 2.0f, 2.5f, 3.0f };
            _settings.ZoomFactor = factors[_zoomCombo.SelectedIndex];
            ApplyAndSaveSettings();
        }

        private void OnLensShapeChanged(object sender, EventArgs e)
        {
            if (_updatingLensControls || _lensShapeCombo.SelectedIndex < 0)
            {
                return;
            }
            _settings.LensShape = _lensShapeCombo.SelectedIndex == 0
                ? LensShapeMode.Circle
                : LensShapeMode.RoundedRectangle;
            PopulateLensSizeOptions(1);
            OnLensSizeChanged(this, EventArgs.Empty);
        }

        private void PopulateLensSizeOptions(int selectedIndex)
        {
            _updatingLensControls = true;
            _lensSizeCombo.Items.Clear();
            if (_settings.LensShape == LensShapeMode.Circle)
            {
                _lensSizeCombo.Items.AddRange(new object[]
                {
                    UiText.Get(_settings.Language, "small") + " · Ø 320",
                    UiText.Get(_settings.Language, "medium") + " · Ø 400",
                    UiText.Get(_settings.Language, "large") + " · Ø 480"
                });
            }
            else
            {
                _lensSizeCombo.Items.AddRange(new object[]
                {
                    UiText.Get(_settings.Language, "small") + " · 360 × 240",
                    UiText.Get(_settings.Language, "medium") + " · 460 × 300",
                    UiText.Get(_settings.Language, "large") + " · 560 × 360"
                });
            }
            _lensSizeCombo.SelectedIndex = Math.Max(0, Math.Min(2, selectedIndex));
            _updatingLensControls = false;
        }

        private void OnLensSizeChanged(object sender, EventArgs e)
        {
            if (_updatingLensControls || _lensSizeCombo.SelectedIndex < 0)
            {
                return;
            }

            if (_settings.LensShape == LensShapeMode.Circle)
            {
                int[] diameters = new int[] { 320, 400, 480 };
                _settings.LensWidth = diameters[_lensSizeCombo.SelectedIndex];
                _settings.LensHeight = _settings.LensWidth;
            }
            else if (_lensSizeCombo.SelectedIndex == 0)
            {
                _settings.LensWidth = 360;
                _settings.LensHeight = 240;
            }
            else if (_lensSizeCombo.SelectedIndex == 1)
            {
                _settings.LensWidth = 460;
                _settings.LensHeight = 300;
            }
            else
            {
                _settings.LensWidth = 560;
                _settings.LensHeight = 360;
            }
            ApplyAndSaveSettings();
        }

        private void OnSettingsTrackBarChanged(object sender, EventArgs e)
        {
            _settings.SpotlightRadius = _spotlightSize.Value;
            _settings.SpotlightOpacity = _darkness.Value / 100.0;
            ApplyAndSaveSettings();
        }

        private void ApplyAndSaveSettings()
        {
            _settings.Save();
            if (_controller != null)
            {
                _controller.ApplySettings();
            }
            UpdateStateUi();
        }

        private void StepZoom(int direction)
        {
            int next = Math.Max(0, Math.Min(_zoomCombo.Items.Count - 1, _zoomCombo.SelectedIndex + direction));
            _zoomCombo.SelectedIndex = next;
        }

        private static int ZoomIndexFromFactor(float factor)
        {
            if (factor < 1.75f) return 0;
            if (factor < 2.25f) return 1;
            if (factor < 2.75f) return 2;
            return 3;
        }

        private static int LensSizeIndex(int width, LensShapeMode shape)
        {
            if (shape == LensShapeMode.Circle)
            {
                if (width < 360) return 0;
                if (width > 440) return 2;
                return 1;
            }
            if (width < 410) return 0;
            if (width > 510) return 2;
            return 1;
        }

        private void ShowAbout()
        {
            string body = _settings.Language == UiLanguage.Vietnamese
                ? "TeachLens 1.0 · Acorn\nMake every pixel teach.\n\nCông cụ hỗ trợ giảng dạy và trình diễn phần mềm trên Windows.\n\nTác giả: Vũ Hữu Thành\nEmail: thanh.vuh@gmail.com\nGitHub: https://github.com/Thanhvh\nHỗ trợ kỹ thuật: OpenAI Codex\n\nHighlight xanh · Spotlight tròn · Kính lúp cục bộ theo từng màn hình"
                : "TeachLens 1.0 · Acorn\nMake every pixel teach.\n\nA focused Windows utility for teaching and software demonstrations.\n\nAuthor: Vũ Hữu Thành\nEmail: thanh.vuh@gmail.com\nGitHub: https://github.com/Thanhvh\nEngineering assistance: OpenAI Codex\n\nGreen pointer highlight · Circular spotlight · Per-monitor local magnifier";
            MessageBox.Show(
                this,
                body,
                UiText.Get(_settings.Language, "aboutTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void HideToTray()
        {
            Hide();
            _trayIcon.ShowBalloonTip(1200, UiText.Get(_settings.Language, "trayBalloonTitle"), UiText.Get(_settings.Language, "trayBalloonBody"), ToolTipIcon.Info);
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
            }
        }

        private void ExitApplication()
        {
            _allowClose = true;
            if (_controller != null)
            {
                _controller.TurnOffAll();
            }
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            Close();
        }
    }

    internal static class SelfTest
    {
        public static int Run(string reportPath)
        {
            List<string> report = new List<string>();
            bool success = true;
            report.Add("TeachLens self-test");
            report.Add("Time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            report.Add("Is64BitProcess=" + Environment.Is64BitProcess.ToString());
            report.Add("OS=" + Environment.OSVersion.VersionString);

            if (!Environment.Is64BitProcess)
            {
                success = false;
                report.Add("ERROR=Magnification API requires an x64 process on 64-bit Windows.");
            }

            bool magnificationInitialized = false;
            try
            {
                magnificationInitialized = NativeMethods.MagInitialize();
                report.Add("MagInitialize=" + (magnificationInitialized ? "OK" : "FAILED"));
                if (!magnificationInitialized)
                {
                    success = false;
                }
                else
                {
                    MagnifierForm form = new MagnifierForm();
                    try
                    {
                        form.SetShape(LensShapeMode.Circle);
                        form.SetLensSize(new Size(400, 400));
                        form.SetFactor(2.0f);
                        NativeMethods.POINT p;
                        NativeMethods.GetCursorPos(out p);
                        Point cursor = new Point(p.X, p.Y);
                        Rectangle monitor = Screen.FromPoint(cursor).Bounds;
                        form.Show();
                        form.UpdateAt(monitor, cursor);
                        Application.DoEvents();
                        report.Add("MagnifierCircle=" + (form.IsReady ? "OK" : "FAILED"));
                        if (!form.IsReady) success = false;
                        form.SetShape(LensShapeMode.RoundedRectangle);
                        form.SetLensSize(new Size(460, 300));
                        form.UpdateAt(monitor, cursor);
                        Application.DoEvents();
                        report.Add("MagnifierRoundedRectangle=" + (form.IsReady ? "OK" : "FAILED"));
                        form.Hide();
                    }
                    finally
                    {
                        form.Dispose();
                    }
                }
            }
            catch (Exception exception)
            {
                success = false;
                report.Add("ERROR=" + exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                if (magnificationInitialized)
                {
                    NativeMethods.MagUninitialize();
                }
            }

            try
            {
                NativeMethods.POINT p;
                NativeMethods.GetCursorPos(out p);
                HighlightForm highlight = new HighlightForm();
                try
                {
                    highlight.Show();
                    highlight.UpdateAt(new Point(p.X, p.Y), 34);
                    Application.DoEvents();
                    report.Add("LayeredHighlight=OK");
                    highlight.Hide();
                }
                finally
                {
                    highlight.Dispose();
                }

                SpotlightForm spotlight = new SpotlightForm(0.58);
                try
                {
                    Rectangle monitor = Screen.FromPoint(new Point(p.X, p.Y)).Bounds;
                    spotlight.UpdateAt(monitor, new Point(p.X, p.Y), 155);
                    report.Add("CircularSpotlightRegion=OK");
                }
                finally
                {
                    spotlight.Dispose();
                }
            }
            catch (Exception exception)
            {
                success = false;
                report.Add("ERROR=Overlay test: " + exception.Message);
            }

            int monitorIndex = 0;
            int geometryChecks = 0;
            foreach (Screen screen in Screen.AllScreens)
            {
                monitorIndex++;
                Rectangle bounds = screen.Bounds;
                report.Add("Monitor" + monitorIndex.ToString(CultureInfo.InvariantCulture) + "=" + bounds.ToString());
                Point[] probes = new Point[]
                {
                    new Point(bounds.Left, bounds.Top),
                    new Point(bounds.Right - 1, bounds.Top),
                    new Point(bounds.Left, bounds.Bottom - 1),
                    new Point(bounds.Right - 1, bounds.Bottom - 1),
                    new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)
                };
                foreach (Point probe in probes)
                {
                    Size[] lensSizes = new Size[] { new Size(480, 480), new Size(560, 360) };
                    foreach (Size lensSize in lensSizes)
                    {
                        geometryChecks++;
                        LensPlacement placement = LensGeometry.Compute(bounds, probe, lensSize, 5, 1.5f);
                        Rectangle source = Rectangle.FromLTRB(placement.Source.Left, placement.Source.Top, placement.Source.Right, placement.Source.Bottom);
                        if (!bounds.Contains(placement.Destination) || !bounds.Contains(source))
                        {
                            success = false;
                            report.Add("ERROR=Lens escaped monitor at " + probe.ToString() + " size " + lensSize.ToString());
                        }
                    }
                }
            }

            report.Add("GeometryChecks=" + geometryChecks.ToString(CultureInfo.InvariantCulture));

            report.Add("RESULT=" + (success ? "PASS" : "FAIL"));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
            File.WriteAllLines(reportPath, report.ToArray(), Encoding.UTF8);
            return success ? 0 : 1;
        }
    }

    internal static class NativeMethods
    {
        public const int WS_CHILD = 0x40000000;
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const uint LWA_ALPHA = 0x00000002;
        public const int ULW_ALPHA = 0x00000002;
        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;
        public const int WM_HOTKEY = 0x0312;
        public const int WM_NCHITTEST = 0x0084;
        public const int HTTRANSPARENT = -1;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_NOREPEAT = 0x4000;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const int MW_FILTERMODE_EXCLUDE = 0;
        public const int VK_LBUTTON = 0x01;
        public const int VK_RBUTTON = 0x02;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int Width;
            public int Height;

            public SIZE(int width, int height)
            {
                Width = width;
                Height = height;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MAGTRANSFORM
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
            public float[] v;
        }

        public static void UpdateLayeredBitmap(IntPtr window, Bitmap bitmap, Point location)
        {
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memoryDc = CreateCompatibleDC(screenDc);
            IntPtr bitmapHandle = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;
            try
            {
                bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = SelectObject(memoryDc, bitmapHandle);
                POINT destination = new POINT();
                destination.X = location.X;
                destination.Y = location.Y;
                POINT source = new POINT();
                SIZE size = new SIZE(bitmap.Width, bitmap.Height);
                BLENDFUNCTION blend = new BLENDFUNCTION();
                blend.BlendOp = AC_SRC_OVER;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = AC_SRC_ALPHA;
                UpdateLayeredWindow(window, screenDc, ref destination, ref size, memoryDc, ref source, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero) SelectObject(memoryDc, oldBitmap);
                if (bitmapHandle != IntPtr.Zero) DeleteObject(bitmapHandle);
                if (memoryDc != IntPtr.Zero) DeleteDC(memoryDc);
                if (screenDc != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        [DllImport("Magnification.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagInitialize();

        [DllImport("Magnification.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagUninitialize();

        [DllImport("Magnification.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetWindowTransform(IntPtr hwnd, ref MAGTRANSFORM transform);

        [DllImport("Magnification.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetWindowSource(IntPtr hwnd, RECT rect);

        [DllImport("Magnification.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MagSetWindowFilterList(IntPtr hwnd, int filterMode, int count, IntPtr[] windows);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
            int extendedStyle,
            string className,
            string windowName,
            int style,
            int x,
            int y,
            int width,
            int height,
            IntPtr parent,
            IntPtr menu,
            IntPtr instance,
            IntPtr parameter);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MoveWindow(IntPtr hwnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint colorKey, byte alpha, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr destinationDc, ref POINT destination, ref SIZE size, IntPtr sourceDc, ref POINT source, int colorKey, ref BLENDFUNCTION blend, int flags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr dc, IntPtr objectHandle);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr objectHandle);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateEllipticRgn(int left, int top, int right, int bottom);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, bool redraw);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InvalidateRect(IntPtr hwnd, IntPtr rect, bool erase);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hwnd);
    }
}
