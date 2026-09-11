using System.Runtime.InteropServices;
using Word = Microsoft.Office.Interop.Word;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    public class PDFConverter
    {
        /// <summary>
        /// 将 docx 文件转换为 pdf 文件,
        /// </summary>
        /// <param name="docxPath"></param>
        /// <param name="pdfPath"></param>
        /// <returns></returns>
        internal static string ConvertDocxToPdf(string docxPath, string pdfPath)
        {
            Word.Application wordApp = null;
            Word.Document wordDoc = null;

            try
            {
                // ✅ 正确：使用 Interop 的 Application
                wordApp = new Word.Application();
                wordApp.Visible = false;
                wordApp.DisplayAlerts = Word.WdAlertLevel.wdAlertsNone;

                object missing = System.Reflection.Missing.Value;

                // ✅ 全部使用命名参数，避免位置参数混用
                wordDoc = wordApp.Documents.Open(
                    FileName: docxPath,
                    ConfirmConversions: false,
                    ReadOnly: true,
                    AddToRecentFiles: false,
                    PasswordDocument: missing,
                    PasswordTemplate: missing,
                    Revert: false,
                    WritePasswordDocument: missing,
                    WritePasswordTemplate: missing,
                    Format: Word.WdOpenFormat.wdOpenFormatAuto,
                    Encoding: missing,
                    Visible: false,
                    OpenAndRepair: false,
                    DocumentDirection: Word.WdDocumentDirection.wdLeftToRight,
                    NoEncodingDialog: true
                );

                // 导出为 PDF
                wordDoc.ExportAsFixedFormat(
                    OutputFileName: pdfPath,
                    ExportFormat: Word.WdExportFormat.wdExportFormatPDF,
                    OpenAfterExport: false,
                    OptimizeFor: Word.WdExportOptimizeFor.wdExportOptimizeForPrint,
                    Range: Word.WdExportRange.wdExportAllDocument,
                    Item: Word.WdExportItem.wdExportDocumentContent,
                    IncludeDocProps: true,
                    KeepIRM: true,
                    CreateBookmarks: Word.WdExportCreateBookmarks.wdExportCreateNoBookmarks,
                    DocStructureTags: true,
                    BitmapMissingFonts: true,
                    UseISO19005_1: false
                );

                return pdfPath;
            }
            finally
            {
                if (wordDoc != null)
                {
                    wordDoc.Close(SaveChanges: false);
                    Marshal.ReleaseComObject(wordDoc);
                }
                if (wordApp != null)
                {
                    wordApp.Quit();
                    Marshal.ReleaseComObject(wordApp);
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}