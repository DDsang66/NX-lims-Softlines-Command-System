using NX_lims_Softlines_Command_System.Application.DTO;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NX_lims_Softlines_Command_System.Application.Interfaces
{
    public interface IPrintExcelStrategy
    {
        void PrintJsonData(ExcelSubmitDto Dto, ExcelPackage packageWet,ExcelPackage packagePhysics);
    }
    /// <summary>
    /// 用来把“买家”映射到对应策略
    /// </summary>
    public interface IPrintExcelStrategyFactory
    {
        IPrintExcelStrategy GetStrategy(string buyer);
    }
}
