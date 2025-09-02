using OfficeOpenXml;
using NX_lims_Softlines_Command_System.Tools.Factory;
using NX_lims_Softlines_Command_System.Application.Interfaces;
namespace NX_lims_Softlines_Command_System.Controllers
{
    public class FillExcel
    {
        public required string excelOutputPath { get; set; }
        //private LabCommandTestEntities Db;

        //设计一个类返回对应的买家对象
        public async Task FillExcelMethod(string templatePathWet, string templatePathPhysics, string excelOutputPathWet, string excelOutputPathPhy, dynamic jsonData)
        {
            try
            {
                // 异步复制文件
                await Task.Run(() =>
                {
                    System.IO.File.Copy(templatePathWet, excelOutputPathWet, true);
                    System.IO.File.Copy(templatePathPhysics, excelOutputPathPhy, true);
                });

                // 异步加载 Excel 文件
                var packageWet = await Task.Run(() => new ExcelPackage(new FileInfo(excelOutputPathWet)));
                var packagePhysics = await Task.Run(() => new ExcelPackage(new FileInfo(excelOutputPathPhy)));

                // 获取买家信息
                string buyer = jsonData.buyer;

                // 获取打印策略
                //IPrintExcelStrategy printStrategy = PrintExcelStrategyFactory.GetStrategy(buyer);

                //// 异步处理 JSON 数据
                //printStrategy.PrintJsonData(jsonData, packageWet, packagePhysics);

                // 异步保存 Excel 文件
                await Task.Run(() => packageWet.Save());
                await Task.Run(() => packagePhysics.Save());
            }
            catch (Exception ex)
            {
                // 记录异常信息
                Console.WriteLine($"Error in FillExcelMethodAsync: {ex.Message}");
            }
        }
    }
}