using System.Drawing;
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
        /// 调整条形码尺寸
        /// </summary>
        /// <param name="barcode">原始条形码位图</param>
        /// <param name="newWidth">新宽度（像素）</param>
        /// <param name="newHeight">新高度（像素）</param>
        /// <param name="interpolationMode">插值模式</param>
        /// <returns>缩放后的条形码位图</returns>
        public static Bitmap Resize(Bitmap barcode, int newWidth, int newHeight,
            System.Drawing.Drawing2D.InterpolationMode interpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            if (newWidth <= 0 || newHeight <= 0)
                throw new ArgumentException("新尺寸必须大于 0");

            var result = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(result);
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = interpolationMode;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.DrawImage(barcode, 0, 0, newWidth, newHeight);

            return result;
        }

        /// <summary>
        /// 调整条形码尺寸（按比例缩放）
        /// </summary>
        /// <param name="barcode">原始条形码位图</param>
        /// <param name="scale">缩放比例（如 1.5 表示放大 1.5 倍）</param>
        /// <returns>缩放后的条形码位图</returns>
        public static Bitmap Scale(Bitmap barcode, double scale)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            if (scale <= 0)
                throw new ArgumentException("缩放比例必须大于 0", nameof(scale));

            var newWidth = (int)(barcode.Width * scale);
            var newHeight = (int)(barcode.Height * scale);

            return Resize(barcode, newWidth, newHeight);
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

            var newW = barcode.Width + margin * 2;
            var newH = barcode.Height + margin * 2;
            var result = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(result);
            g.Clear(Color.White);
            g.DrawImage(barcode, margin, margin);
            return result;
        }

        /// <summary>
        /// 旋转条形码
        /// </summary>
        /// <param name="barcode">条形码位图</param>
        /// <param name="angle">旋转角度（度）</param>
        /// <returns>旋转后的条形码位图</returns>
        public static Bitmap Rotate(Bitmap barcode, float angle)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            using var graphics = Graphics.FromImage(barcode);
            graphics.RotateTransform(angle);
            return barcode;
        }

        /// <summary>
        /// 克隆条形码位图
        /// </summary>
        /// <param name="barcode">原始条形码位图</param>
        /// <returns>克隆的条形码位图</returns>
        public static Bitmap Clone(Bitmap barcode)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            return new Bitmap(barcode);
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
        /// </summary>
        /// <param name="barcode">条形码位图</param>
        /// <param name="targetWidth">目标宽度（像素），自动缩放</param>
        /// <param name="targetHeight">目标高度（像素），自动缩放</param>
        /// <returns>字节数组</returns>
        public static byte[] ToWordCompatibleBytes(Bitmap barcode, int targetWidth = 200, int targetHeight = 40)
        {
            if (barcode == null)
                throw new ArgumentNullException(nameof(barcode));

            // 缩放条形码到合适尺寸
            var resized = Resize(barcode, targetWidth, targetHeight);
            // 添加白色背景
            var withBackground = AddWhiteBackground(resized, 5);
            // 转换为字节数组
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
        /// <param name="barcode">条形码位图</param>
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
