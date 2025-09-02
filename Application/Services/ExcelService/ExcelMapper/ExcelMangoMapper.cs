using DocumentFormat.OpenXml.Office.CustomUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelMangoMapper
    {
        //WET
        public static string[] GetFixedCellAddresses()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "AZ8", "BG8", "BN8", "BU8","AW12","BO12","AW23","BO23"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] GetDStodrycleanCellAddresses()
        {
            return new string[]
            {
                "AZ6", "BG6", "BN6", "BU6","AW10","BO10","AW21","BO21"
            };
        }
        public static string[] GetCRLCellAddresses(string ItemName)
        {
            List<string>? stringCFR = null;
            switch (ItemName)
            {
                case "CF to Washing":
                    stringCFR = new List<string> { "D7", "F7", "H7", "J7", "L7", "N7" };
                    break;
                case "CF to Rubbing":
                    stringCFR = new List<string> { "D21", "F21", "H21", "J21", "L21", "N21" };
                    break;
                case "CF to Light":
                    stringCFR = new List<string> { "D29", "F29", "H29", "J29", "L29", "N29" };
                    break;
                default: break;
            }
            return stringCFR?.ToArray() ?? new string[0];

        }

        public static string[] GetPWDCellAddresses(string ItemName)
        {
            List<string>? stringPWD = null;
            switch (ItemName)
            {
                case "CF to Perspiration":
                    stringPWD = new List<string> { "D5", "F5", "H5", "J5", "L5", "N5", "D14", "F14", "H14", "J14", "L14", "N14" };
                    break;
                case "CF to Water":
                    stringPWD = new List<string> { "D26", "F26", "H26", "J26", "L26", "N26" };
                    break;
                case "CF to Dry-clean":
                    stringPWD = new List<string> { "D38", "F38", "H38", "J38", "L38", "N38" };
                    break;
                default: break;
            }
            return stringPWD?.ToArray() ?? new string[0];

        }


        //Physics
        public static string[] GetYarnCountCellAddresses()
        {
            return new string[]
            {
                "D10"
            };
        }

        public static string[] GetWeightCellAddresses()
        {
            return new string[]
            {
                "A12", "A13", "A14", "A15","A15"
            };
        }
        public static string[] GetPillingCellAddresses(string menuName)
        {
            List<string>? stringPilling = null;
            switch (menuName)
            {
                case "Knit(Mango)":
                    stringPilling = new List<string> { "A8", "A9", "A10" };
                    break;
                case "Woven(Mango)":
                    stringPilling = new List<string> { "A18", "A25" };
                    break;
                default: break;
            }
            return stringPilling?.ToArray() ?? new string[0];
        }
        public static string[] GetSTCellAddresses(string ItemName)
        {
            List<string>? stringST = null;
            switch (ItemName)
            {
                case "Seam Slippage":
                    stringST = new List<string> { "A10", "A12" };
                    break;
                case "Tear Strength":
                    stringST = new List<string> { "A10", "A12", "A14" };
                    break;
                default: break;
            }
            return stringST?.ToArray() ?? new string[0];
        }


        public static string[] GetASCellAddresses(string ItemName)
        {
            List<string>? stringST = null;
            switch (ItemName)
            {
                case "Abrasion Resistance":
                    stringST = new List<string> { "H8", "O8", "V8", "AC8" };
                    break;
                case "Snagging Resistance":
                    stringST = new List<string> { "K28", "R28", "Y28", "AF28" };
                    break;
                default: break;
            }
            return stringST?.ToArray() ?? new string[0];
        }
    }
}