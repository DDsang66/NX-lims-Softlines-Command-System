using ClosedXML.Excel;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService
{
    internal class XLWorkbook : IDisposable
    {
        private bool disposed = false; // 标记是否已释放资源
        private ClosedXML.Excel.XLWorkbook workbook;

        public XLWorkbook(string filePath)
        {
            // 加载 Excel 文件
            workbook = new ClosedXML.Excel.XLWorkbook(filePath);
        }

        // 实现 IDisposable 接口
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // 释放资源
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    if (workbook != null)
                    {
                        workbook.Dispose();
                        workbook = null;
                    }
                }

                // 释放非托管资源（如果有）

                disposed = true;
            }
        }

        // 析构函数
        ~XLWorkbook()
        {
            Dispose(false);
        }

        // 提供一个方法来获取工作表
        public IXLWorksheet GetWorksheet(int worksheetIndex)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(XLWorkbook));
            }
            return workbook.Worksheet(worksheetIndex);
        }
    }
}