using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using OfficeOpenXml;
using System.Configuration;
using NX_lims_Softlines_Command_System.Application.Interfaces;
using NX_lims_Softlines_Command_System.Application.Services.ExcelIO.PrintExcelMethod;

namespace NX_lims_Softlines_Command_System.Tools.Factory
{

    public sealed class PrintExcelStrategyFactory:IPrintExcelStrategyFactory
    {
        private readonly IServiceProvider _sp;
        public PrintExcelStrategyFactory(IServiceProvider sp) => _sp = sp;

        public IPrintExcelStrategy GetStrategy(string buyer) =>
            buyer switch
            {
                "Mango" => _sp.GetRequiredService<PrintMangoExcel>(),
                "Adidas" => _sp.GetRequiredService<PrintAdidasExcel>(),
                "CrazyLine" => _sp.GetRequiredService<PrintCrazyLineExcel>(),
                //"Jako" => _sp.GetRequiredService<PrintJakoLineExcel>(),
                _ => throw new ArgumentException($"Unknown buyer: {buyer}")
            };
    }
}
