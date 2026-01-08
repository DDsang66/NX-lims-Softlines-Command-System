using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using OfficeOpenXml;
using System.Configuration;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod;

namespace NX_lims_Softlines_Command_System.Application.Services.Factory
{

    public sealed class PrintExcelStrategyFactory : IPrintExcelStrategyFactory
    {
        private readonly IServiceProvider _sp;
        public PrintExcelStrategyFactory(IServiceProvider sp) => _sp = sp;

        public IPrintExcelStrategy GetStrategy(string buyer) =>
            buyer switch
            {
                "mango" => _sp.GetRequiredService<PrintMangoExcel>(),
                "adidas" => _sp.GetRequiredService<PrintAdidasExcel>(),
                "crazyline" => _sp.GetRequiredService<PrintCrazyLineExcel>(),
                "jako" => _sp.GetRequiredService<PrintJakoExcel>(),
                "tchibo" => _sp.GetRequiredService<PrintTchiboExcel>(),
                "primark" => _sp.GetRequiredService<PrintPrimarkExcel>(),
                "pepco" => _sp.GetRequiredService<PrintPepcoExcel>(),
                "kik" => _sp.GetRequiredService<PrintKikExcel>(),
                "next" => _sp.GetRequiredService<PrintNextExcel>(),
                "ovs" => _sp.GetRequiredService<PrintOvsExcel>(),
                "lpp" => _sp.GetRequiredService<PrintLppExcel>(),
                "woolworth" => _sp.GetRequiredService<PrintWoolworthExcel>(),
                "focus" => _sp.GetRequiredService<PrintFocusExcel>(),
                _ => throw new ArgumentException($"Unknown buyer: {buyer}")
            };
    }
}
