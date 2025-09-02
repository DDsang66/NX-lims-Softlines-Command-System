//using Spire.Xls;
//using Spire.Xls.Core;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using Workbook = Spire.Xls.Workbook;

//namespace NX_lims_Softlines_Command_System.Tools
//{
//    public class SelectPrintExcel
//    {
//        /// <summary>
//        /// 过滤并删除包含空 "reportnumber" 单元格的工作表
//        /// </summary>
//        /// <param name="inputFilePath">输入Excel文件路径</param>
//        /// <param name="outputFilePath">输出Excel文件路径</param>
//        /// <param name="keyword">要查找的命名范围关键词（默认"reportname"）</param>
//        /// <returns>是否成功处理</returns>
//        public static void ProcessExcelFile(string filePath)
//        {
//            using (Workbook workbook = new Workbook())
//            {
//                workbook.LoadFromFile(filePath);
//                Console.WriteLine($"处理文件: {filePath}");

//                // 反向遍历避免索引问题
//                for (int i = workbook.Worksheets.Count - 1; i >= 0; i--)
//                {
//                    Worksheet sheet = workbook.Worksheets[i];
//                    bool shouldKeep = false;

//                    /* 只看当前 sheet 自己的命名范围 */
//                    foreach (INamedRange nr in sheet.Names)
//                    {
//                        if (nr.Name.Equals("ReportNumber", StringComparison.OrdinalIgnoreCase) &&
//                            nr.RefersToRange != null)
//                        {
//                            string value = nr.RefersToRange!.Value?.ToString()!;
//                            Console.WriteLine($"{sheet.Name}!{nr.Name} = '{value}'");

//                            if (!string.IsNullOrWhiteSpace(value))
//                            {
//                                shouldKeep = true;
//                                break;
//                            }
//                        }
//                    }

//                    if (!shouldKeep)
//                    {
//                        Console.WriteLine($"删除工作表: {sheet.Name}（未找到有效reportname）");

//                        if (i == 0 && workbook.Worksheets.Count() == 1)
//                        {
//                            Console.WriteLine("不用删除最后的工作表直接将excel删除");
//                            System.IO.File.Delete(filePath);
//                            break; // 如果没有工作表，退出循环
//                        }
//                        else
//                        {
//                            workbook.Worksheets.RemoveAt(i);
//                        }
//                    }

//                }

//                if (System.IO.File.Exists(filePath))
//                {
//                    workbook.SaveToFile(filePath, FileFormat.Version2013);
//                    Console.WriteLine("处理完成！");

//                }

//            }
//        }
//    }
//}

using OfficeOpenXml;
using System;
using System.IO;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService
{
    public class SelectPrintExcel
    {
        /// <summary>
        /// 过滤并删除包含空 "ReportNumber" 单元格的工作表
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        public static void ProcessExcelFile(string filePath)
        {

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                Console.WriteLine($"处理文件: {filePath}");
                var workbook = package.Workbook;

                // 反向遍历避免索引问题
                for (int i = workbook.Worksheets.Count - 1; i >= 0; i--)
                {
                    var worksheet = workbook.Worksheets[i];
                    bool shouldKeep = false;

                    // 查找当前工作表中的ReportNumber命名范围
                    foreach (var namedRange in worksheet.Names)
                    {
                        if (string.Equals(namedRange.Name, "ReportNumber", StringComparison.OrdinalIgnoreCase) &&
                            namedRange.Worksheet == worksheet) // 确保是当前工作表的命名范围
                        {
                            // 获取命名范围的值
                            string value = namedRange.Value?.ToString() ?? string.Empty;
                            Console.WriteLine($"{worksheet.Name}!{namedRange.Name} = '{value}'");

                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                shouldKeep = true;
                                break;
                            }
                        }
                    }

                    if (!shouldKeep)
                    {
                        Console.WriteLine($"删除工作表: {worksheet.Name}（未找到有效ReportNumber）");

                        if (i == 0 && workbook.Worksheets.Count == 1)
                        {
                            Console.WriteLine("不用删除最后的工作表直接将excel删除");
                            package.Dispose(); // 释放文件占用
                            File.Delete(filePath);
                            return; // 直接退出方法
                        }
                        else
                        {
                            workbook.Worksheets.Delete(i);
                        }
                    }
                }

                // 保存修改
                package.Save();
                Console.WriteLine("处理完成！");
            }
        }
    }
}

