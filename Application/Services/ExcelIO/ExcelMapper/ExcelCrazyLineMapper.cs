using DocumentFormat.OpenXml.Office.CustomUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelIO
{
    public static class ExcelCrazyLineMapper
    {
        #region PHY
        public static string[] MapWeight()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "A12", "A13", "A14", "A15","A16"
            };
        }

        public static string[] MapPS(string ItemName)
        {
            List<string> stringPS = null;
            // 定义固定的单元格地址映射
            switch (ItemName)
            {
                case "Snagging Resistance":
                    stringPS = new List<string> { "K19", "R19", "Y19", "AF19"};
                    break;
                case "Pilling Resistance":
                    stringPS = new List<string> { "A8", "A9", "A10", "A11" };
                    break;
                default: break;
            }
            return stringPS?.ToArray() ?? new string[0];

        }

        public static string[] MapSeamSlippage()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "A10", "A12"
            };
        }

        public static string[] MapRegular()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D5"
            };
        }

        #endregion

        #region WET
        public static string[] MapDStoWasing(string sampleDescription)
        {
            List<string> stringMap;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "G11" },
                "Fabric" => new List<string> { "M9", "T9", "AA9", "AI9", "G13", "AB13", "G24", "AB24" },
                "Socks" => new List<string> { "G11" },
                "Gloves" => new List<string> { "G20" },
                "Cap" => new List<string> { "G29" },
                _ => new List<string> { "M9", "T9", "AA9", "AI9", "G13", "AB13", "G24", "AB24" }
            };

            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapDStoDC(string sampleDescription)
        {
            List<string> stringMap;
            var matched = new[] { "Garment", "Fabric", "Socks", "Glaves", "Cap" }
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "G8" },
                "Fabric" => new List<string> { "K6", "R6", "Y6", "AG6", "F10", "Z10", "F21", "Z21" },
                "Socks" => new List<string> { "G8" },
                "Gloves" => new List<string> { "G17" },
                "Cap" => new List<string> { "G26" },
                _ => new List<string> { "K6", "R6", "Y6", "AG6", "F10", "Z10", "F21", "Z21" }
            };

            return stringMap?.ToArray() ?? new string[0];
        }



        public static string[] MapWRL(string ItemName)
        {
            List<string> stringPWD = null;
            // 定义固定的单元格地址映射
            switch (ItemName)
            {
                case "CF to Washing":
                    stringPWD = new List<string> { "E7", "G7", "J7", "M7", "O7", "Q7"};
                    break;
                case "CF to Rubbing":
                    stringPWD = new List<string> { "E21", "G21", "J21", "M21", "O21", "Q21" };
                    break;
                case "CF to Light":
                    stringPWD = new List<string> { "E29", "G29", "J29", "M29", "O29", "Q29" };
                    break;
                default: break;
            }
            return stringPWD?.ToArray() ?? new string[0];
        }

        public static string[] MapPWD(string ItemName)
        {
            List<string> stringPWD = null;
            // 定义固定的单元格地址映射
            switch (ItemName)
            {
                case "CF to Perspiration":
                    stringPWD = new List<string> { "D5", "F5", "H5", "J5","L5","N5" };
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




        public static string[] MapSpirality(string sampleDescription)
        {
            List<string> stringSpirality;
            var matched = new[] { "Garment", "Fabric", "Socks", "Glaves", "Cap" }
                              .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            stringSpirality = matched switch
            {
                "Garment" => new List<string> { "A26","A27","A28"},
                "Fabric" => new List<string> { "A10", "A11", "A12"},
                "Socks" => new List<string> { "A10", "A11", "A12" },
                "Gloves" => new List<string> { "A10", "A11", "A12" },
                "Cap" => new List<string> { "A10", "A11", "A12" },
                _ => new List<string> { "M9", "T9", "AA9", "AI9", "G13", "AB13", "G24", "AB24" }
            };
            return stringSpirality?.ToArray() ?? new string[0];
        }

        #endregion


    }
}