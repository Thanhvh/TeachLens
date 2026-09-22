using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class IconBuilder
{
    private static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            return 2;
        }

        int[] sizes = new int[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
        List<byte[]> images = new List<byte[]>();
        foreach (int size in sizes)
        {
            images.Add(Render(size));
        }

        using (FileStream stream = new FileStream(args[0], FileMode.Create, FileAccess.Write))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)images.Count);
            int offset = 6 + images.Count * 16;
            for (int i = 0; i < images.Count; i++)
            {
                int size = sizes[i];
                writer.Write((byte)(size == 256 ? 0 : size));
                writer.Write((byte)(size == 256 ? 0 : size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(images[i].Length);
                writer.Write(offset);
                offset += images[i].Length;
            }
            foreach (byte[] image in images)
            {
                writer.Write(image);
            }
        }
        return 0;
    }

    private static byte[] Render(int size)
    {
        using (Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            RectangleF tile = new RectangleF(size * 0.035f, size * 0.035f, size * 0.93f, size * 0.93f);
            using (GraphicsPath tilePath = RoundedRectangle(tile, size * 0.22f))
            using (LinearGradientBrush background = new LinearGradientBrush(tile, Color.FromArgb(17, 54, 47), Color.FromArgb(24, 83, 67), 45.0f))
            {
                graphics.FillPath(background, tilePath);
            }

            float lensDiameter = size * 0.49f;
            RectangleF lens = new RectangleF(size * 0.17f, size * 0.15f, lensDiameter, lensDiameter);
            float ringWidth = Math.Max(1.4f, size * 0.095f);
            using (SolidBrush glass = new SolidBrush(Color.FromArgb(36, 213, 245, 225)))
            using (Pen ring = new Pen(Color.FromArgb(255, 69, 225, 124), ringWidth))
            using (Pen highlight = new Pen(Color.FromArgb(230, 221, 255, 234), Math.Max(1.0f, size * 0.025f)))
            {
                graphics.FillEllipse(glass, lens);
                graphics.DrawEllipse(ring, lens);
                RectangleF gleam = new RectangleF(lens.X + lens.Width * 0.19f, lens.Y + lens.Height * 0.16f, lens.Width * 0.34f, lens.Height * 0.34f);
                graphics.DrawArc(highlight, gleam, 200, 100);
            }

            PointF handleStart = new PointF(size * 0.60f, size * 0.59f);
            PointF handleEnd = new PointF(size * 0.82f, size * 0.82f);
            using (Pen handleShadow = new Pen(Color.FromArgb(120, 3, 23, 18), Math.Max(2.0f, size * 0.14f)))
            using (Pen handle = new Pen(Color.FromArgb(255, 233, 255, 241), Math.Max(1.5f, size * 0.095f)))
            {
                handleShadow.StartCap = LineCap.Round;
                handleShadow.EndCap = LineCap.Round;
                handle.StartCap = LineCap.Round;
                handle.EndCap = LineCap.Round;
                graphics.DrawLine(handleShadow, handleStart, handleEnd);
                graphics.DrawLine(handle, handleStart, handleEnd);
            }

            using (MemoryStream output = new MemoryStream())
            {
                bitmap.Save(output, ImageFormat.Png);
                return output.ToArray();
            }
        }
    }

    private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        GraphicsPath path = new GraphicsPath();
        float diameter = radius * 2.0f;
        RectangleF arc = new RectangleF(bounds.Left, bounds.Top, diameter, diameter);
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
