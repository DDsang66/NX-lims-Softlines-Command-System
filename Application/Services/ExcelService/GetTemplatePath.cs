using DocumentFormat.OpenXml.Spreadsheet;
using NX_lims_Softlines_Command_System.Application.DTO;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService
{
    public class GetPath
    {
        /// <summary>
        /// 返回相对于 wwwroot 的模板路径
        /// </summary>
        public static string GetTemplatePath(ExcelSubmitDto Dto, string sheetType)
        {
            string DtoField = Dto.Buyer!;
            return $"ExcelModel/{DtoField}_{sheetType}_sheet.xlsx";
        }

        /// <summary>
        /// 返回文件名（不含目录）
        /// </summary>
        public static string GetOutputPath(ExcelSubmitDto Dto, string sheetType)
        {
            string orderNumber = Dto.ReportNumber!;
            string reviewer = Dto.Reviewer!;
            string buyer = Dto.Buyer!;
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{orderNumber}_{buyer}_{sheetType}_{timestamp}_{reviewer}.xlsx";
        }
    }
}