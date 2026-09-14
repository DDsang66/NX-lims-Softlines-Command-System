using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine
{
    public class BarcodePictureHelper
    {
        /// <summary>
        /// 将条形码 Bitmap 转换为字节数组
        /// </summary>
        /// <param name="barcode">条形码位图</param>
        /// <param name="format">图片格式（默认 PNG）</param>
        /// <returns>字节数组</returns>
        public static byte[] ToByteArray(Bitmap barcode, ImageFormat? format = null)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            format ??= ImageFormat.Png;

            using var memoryStream = new MemoryStream();
            barcode.Save(memoryStream, format);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// 将条形码 Bitmap 保存为文件
        /// </summary>
        /// <param name="barcode">条形码位图</param>
        /// <param name="filePath">保存路径</param>
        /// <param name="format">图片格式（默认 PNG）</param>
        public static void SaveToFile(Bitmap barcode, string filePath, ImageFormat? format = null)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            format ??= ImageFormat.Png;
            barcode.Save(filePath, format);
        }

        /// <summary>
        /// 调整条形码尺寸（默认最近邻插值，保持硬边界，避免灰阶过渡）
        /// </summary>
        /// <param name="barcode">原始条形码位图</param>
        /// <param name="newWidth">新宽度（像素）</param>
        /// <param name="newHeight">新高度（像素）</param>
        /// <param name="interpolationMode">插值模式（默认 NearestNeighbor）</param>
        /// <returns>缩放后的条形码位图</returns>
        public static Bitmap Resize(Bitmap barcode, int newWidth, int newHeight,
            InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            if (newWidth <= 0 || newHeight <= 0)
                throw new ArgumentException("新尺寸必须大于 0");

            // 二值/硬边界图形用 24bpp，避免 alpha 通道引入边缘混合
            var result = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);

            // 保留原图 DPI，避免 Word 插入时显示尺寸偏差
            result.SetResolution(barcode.HorizontalResolution, barcode.VerticalResolution);

            using var graphics = Graphics.FromImage(result);

            // 最近邻时不需要高质量平滑，反而要避免半像素偏移
            graphics.InterpolationMode = interpolationMode;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.CompositingQuality = CompositingQuality.HighSpeed;

            // 先铺白底，避免透明/黑底
            graphics.Clear(Color.White);
            graphics.DrawImage(barcode, 0, 0, newWidth, newHeight);

            return result;
        }

        /// <summary>
        /// 调整条形码尺寸（按比例缩放）
        /// </summary>
        /// <param name="barcode">原始条形码位图</param>
        /// <param name="scale">缩放比例（如 1.5 表示放大 1.5 倍）</param>
        /// <param name="interpolationMode">插值模式（默认 NearestNeighbor）</param>
        /// <returns>缩放后的条形码位图</returns>
        public static Bitmap Scale(Bitmap barcode, double scale,
            InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            if (scale <= 0)
                throw new ArgumentException("缩放比例必须大于 0", nameof(scale));

            var newWidth = (int)Math.Round(barcode.Width * scale);
            var newHeight = (int)Math.Round(barcode.Height * scale);

            return Resize(barcode, newWidth, newHeight, interpolationMode);
        }

        /// <summary>
        /// 为条形码添加白色背景（去除透明背景）
        /// </summary>
        /// <param name="barcode">条形码位图</param>
        /// <param name="margin">边距（像素）</param>
        /// <returns>带白色背景的条形码位图</returns>
        public static Bitmap AddWhiteBackground(Bitmap barcode, int margin = 10)
        {
            if (barcode == null) throw new ArgumentNullException(nameof(barcode));
            if (barcode.Width <= 0 || barcode.Height <= 0)
                throw new ArgumentException("Barcode has invalid size", nameof(barcode));
            if (margin < 0)
                throw new ArgumentException("margin 不能为负数", nameof(margin));

            var newW = barcode.Width + margin * 2;
            var newH = barcode.Height + margin * 2;

            // 24bpp，不用 alpha
            var result = new Bitmap(newW, newH, PixelFormat.Format24bppRgb);
            result.SetResolution(barcode.HorizontalResolution, barcode.VerticalResolution);

            using var g = Graphics.FromImage(result);
            g.Clear(Color.White);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.None;
            g.DrawImage(barcode, margin, margin);

            return result;
        }

        /// <summary>
        /// 旋转条形码（修复原实现只设置变换矩阵、未实际绘制的问题）
        /// </summary>
        /// <param name="barcode">条形码位图</param>
        /// <param name="angle">旋转角度（度）</param>
        /// <returns>旋转后的条形码位图</returns>
        public static Bitmap Rotate(Bitmap barcode, float angle)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            var result = new Bitmap(barcode.Width, barcode.Height, PixelFormat.Format24bppRgb);
            result.SetResolution(barcode.HorizontalResolution, barcode.VerticalResolution);

            using var g = Graphics.FromImage(result);
            g.Clear(Color.White);
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.None;

            // 绕中心旋转
            g.TranslateTransform(barcode.Width / 2f, barcode.Height / 2f);
            g.RotateTransform(angle);
            g.TranslateTransform(-barcode.Width / 2f, -barcode.Height / 2f);

            g.DrawImage(barcode, 0, 0);

            return result;
        }

        /// <summary>
        /// 克隆条形码位图（保留 DPI 与像素格式）
        /// </summary>
        /// <param name="barcode">原始条形码位图</param>
        /// <returns>克隆的条形码位图</returns>
        public static Bitmap Clone(Bitmap barcode)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            var clone = new Bitmap(barcode.Width, barcode.Height, barcode.PixelFormat);
            clone.SetResolution(barcode.HorizontalResolution, barcode.VerticalResolution);

            using var g = Graphics.FromImage(clone);
            g.DrawImage(barcode, 0, 0);

            return clone;
        }

        /// <summary>
        /// 获取条形码尺寸信息
        /// </summary>
        /// <param name="barcode">条形码位图</param>
        /// <returns>尺寸信息</returns>
        public static BarcodeSizeInfo GetSizeInfo(Bitmap barcode)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            return new BarcodeSizeInfo
            {
                Width = barcode.Width,
                Height = barcode.Height,
                PixelFormat = barcode.PixelFormat.ToString(),
                HorizontalResolution = barcode.HorizontalResolution,
                VerticalResolution = barcode.VerticalResolution
            };
        }

        /// <summary>
        /// 将条形码转换为指定格式的字节数组（用于 Word 文档插入）
        /// 注意：先加白边，再整体缩放到目标尺寸，避免非整数倍缩放破坏条宽。
        /// 若 targetWidth/targetHeight 传 0，则保持原尺寸（仅加白边）。
        /// </summary>
        /// <param name="barcode">条形码位图</param>
        /// <param name="targetWidth">目标宽度（像素），0 表示不缩放</param>
        /// <param name="targetHeight">目标高度（像素），0 表示不缩放</param>
        /// <param name="margin">白边（像素）</param>
        /// <returns>字节数组</returns>
        public static byte[] ToWordCompatibleBytes(Bitmap barcode,
            int targetWidth = 0, int targetHeight = 0, int margin = 5)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            // 1. 先加白边
            using var withBackground = AddWhiteBackground(barcode, margin);

            // 2. 如需缩放，再整体缩放到目标尺寸
            if (targetWidth > 0 && targetHeight > 0)
            {
                using var resized = Resize(withBackground, targetWidth, targetHeight);
                return ToByteArray(resized);
            }

            return ToByteArray(withBackground);
        }

        /// <summary>
        /// 验证条形码是否有效
        /// </summary>
        /// <param name="barcode">条形码位图</param>
        /// <returns>是否有效</returns>
        public static bool IsValid(Bitmap barcode)
        {
            if (barcode == null)
                return false;

            return barcode.Width > 0 && barcode.Height > 0;
        }

        /// <summary>
        /// 释放条形码资源
        /// </summary>
        public static void Dispose(Bitmap barcode)
        {
            barcode?.Dispose();
        }
    }

    /// <summary>
    /// 条形码尺寸信息
    /// </summary>
    public class BarcodeSizeInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string PixelFormat { get; set; } = string.Empty;
        public float HorizontalResolution { get; set; }
        public float VerticalResolution { get; set; }
    }
}