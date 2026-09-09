using System;
using System.Drawing;
using System.Runtime.Versioning;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    public class BarcodeGenerator
    {
        /// <summary>
        /// 根据 GUID 生成条形码位图（即时生成，即时使用）
        /// </summary>
        /// <param name="id">要编码的 GUID</param>
        /// <param name="width">条形码图片宽度，默认300</param>
        /// <param name="height">条形码图片高度，默认100</param>
        /// <returns>条形码位图，失败时返回 null</returns>
         [SupportedOSPlatform("windows")]
        public static Bitmap GenerateBarcode(Guid id, int width = 200, int height = 80)
        {
            if (id == Guid.Empty)
                return null;

            if (!OperatingSystem.IsWindows())
                return null;

            string barcodeData = id.ToString("N");

            try
            {
                var writer = new BarcodeWriter<Bitmap>
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Width = width,
                        Height = height,
                        Margin = 10,
                        PureBarcode = false
                    },
                    Renderer = new BitmapRenderer()
                };

                return writer.Write(barcodeData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成条形码失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据 GUID 生成条形码并保存为文件
        /// </summary>
        /// <param name="id">要编码的 GUID</param>
        /// <param name="filePath">保存路径</param>
        /// <returns>是否保存成功</returns>
        [SupportedOSPlatform("windows")]
        public static bool GenerateBarcodeToFile(Guid id, string filePath, int width = 300, int height = 100)
        {
            var bitmap = GenerateBarcode(id, width, height);
            if (bitmap == null)
                return false;

            try
            {
                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存条形码文件失败: {ex.Message}");
                return false;
            }
            finally
            {
                bitmap.Dispose();
            }
        }
    }
}
