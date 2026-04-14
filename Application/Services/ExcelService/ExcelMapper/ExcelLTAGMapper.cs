using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelLTAGMapper
    {
        #region WET
        public static string[] MapAppearance()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BA11", "BR23"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapDStoWashing(string sampleDescription)
        {
            List<string> stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
            .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "F11" },
                "Fabric" => new List<string> { "M9", "T9", "AA9", "AI9", "G13", "AB13", "G24", "AB24" },
                "Socks" => new List<string> { "G11" },
                "Gloves" => new List<string> { "G20" },
                "Cap" => new List<string> { "G29" },
                _ => new List<string> { "M9", "T9", "AA9", "AI9", "G13", "AB13", "G24", "AB24" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapCFtoWashing()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "E7", "G7", "J7", "M7", "O7", "Q7"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoRubbing()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "E20", "G20", "J20", "M20", "O20", "Q20"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoLight()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "E27", "G27", "J27", "M27", "O27", "Q27"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoSeaWater()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D29","F29", "H29", "J29", "L29", "N29"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoPerspiration()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D5", "F5", "H5", "J5", "L5", "N5","D14", "F14", "H14", "J14", "L14", "N14"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoWater()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D26", "F26", "H26", "J26", "L26", "N26"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoDC()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D38", "F38", "H38", "J38", "L38", "N38"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapDyeTransfer()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D4", "F4", "H4", "J4", "L4", "N4"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCl()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D20", "F20", "H20", "J20", "L20", "N20"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoSalivaSweat()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D4", "F4", "H4", "J4", "L4", "N4"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoCl()
        {
            // 定义固定的单元格地址映射
            return new string[]
           {
                "D20", "F20", "H20", "J20", "L20", "N20"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapBleach(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "CF to Chlorine Bleaching":
                    stringMap = new List<string> { "D7", "F7", "H7", "J7", "L7", "N7" };
                    break;
                case "CF to Non-Chlorine Bleaching":
                    stringMap = new List<string> { "D14", "F14", "H14", "J14", "L14", "N14", "D21", "F21", "H21", "J21", "L21", "N21" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapSpirality()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "A26","A27","A28"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        #endregion
        #region Physics
        public static string[] MapWeight()
        {
            return new string[]
            {
                "A12", "A13", "A14", "A15","A16"
            };
        }
        public static string[] MapDensity()
        {
            return new string[]
            {
                "A10","A14"
            };
        }
        public static string[] MapYarn()
        {
            return new string[]
            {
                "A11"
            };
        }
        public static string[] MapTwist()
        {
            return new string[]
            {
                "A11","A16","A21"
            };
        }
        public static string[] MapWeave()
        {
            return new string[]
            {
                "R2"
            };
        }
        public static string[] MapThickness()
        {
            return new string[]
            {
                "A29","A41","A43"
            };
        }
        public static string[] MapWicking()
        {
            return new string[]
            {
                "A9","A13","A17","A21"
            };
        }
        public static string[] MapDryRate()
        {
            return new string[]
            {
                "I18","W18","A38","A40"
            };
        }
        public static string[] MapPilling()
        {
            return new string[] {  "A8","A10","A12","A14","A16","A8","A20","A22" };
        }
        public static string[] MapAbrasion()
        {
            return new string[]
            {
                "H8","O8","V8","AC8"
            };
        }
        public static string[] MapSeamStrength(string ItemName,string SampleDescription)
        {
            List<string>? map = null;
            if (SampleDescription.Contains("Fabric")) map = new List<string> { "A29", "A33" };
            else if (SampleDescription.Contains("Garment") && SampleDescription.Contains("Knit")&& ItemName.Contains("Seam Strength")) map = new List<string> { "D5" };
            else if (SampleDescription.Contains("Garment")) map = new List<string> { "D18" };
            return map?.ToArray() ?? new string[0];
        }
        public static string[] MapBursting(string SampleDescription)
        {
            List<string>? map = null;
            if (SampleDescription.Contains("Fabric")) map = new List<string> { "A8", "A10", "A12", "A14" };
            else if (SampleDescription.Contains("Garment")) map = new List<string> { "D3" };
            return map?.ToArray() ?? new string[0];
        }
        public static string[] MapSeamSlippage(string ItemName, string SampleDescription)
        {
            List<string>? map = null;
            if (SampleDescription.Contains("Fabric")) map = new List<string> { "A12", "A14" };
            else if (SampleDescription.Contains("Garment")) map = new List<string> { "D3" };

            return map?.ToArray() ?? new string[0];
        }
        public static string[] MapBond()
        {
            return new string[]
            {
                "A12","A16","A20"
            };
        }
        public static string[] TorqueTension() 
        {
            return new string[]
{
                "A6"
};
        }
        public static string[] MapZipperStrength()
        {
            return new string[]
            {
                "AG5"
            };
        }
        public static string[] MapUnSnapping() 
        {
            return new string[]
{
                "D5"
};
        }
        public static string[] MapExtensionAndRecovery()
        {
            return new string[]
            {
                "AG3","AW3"
            };
        }
        public static string[] MapHydrostaticPressing()
        {
            return new string[]
            {
                "A20","A22"
            };
        }
        public static string[] MapRepellency()
        {
            return new string[]
            {
                "A8","A9","A10","A15","A16","A17"
            };
        }
        public static string[] MapAirPermeability()
        {
            return new string[]
            {
                "I10","O10","U10","AA10"
            };
        }
        public static string[] MapAbsorbency()
        {
            return new string[]
            {
               "A11","A12","A13","A14","A15","A20","A21","A22","A23","A24","A25","A30","A31","A32","A33","A34","A35"
            };
        }
        public static string[] MapTear()
        {
            return new string[]
            {
               "A11","A13","A15"
            };
        }
        public static string[] MapTensile()
        {
            return new string[]
            {
                "A12","A14","A16","A18"
            };
        }
        #endregion

        //AfterWash
        public static string[] DStoWashingAf(string sampleDescription)
        {
            List<string> stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
            .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "W9", "AG11" },
                "Fabric" => new List<string> { "L14", "AF14", "L25", "AF25" },
                "Socks" => new List<string> { "W9", "AG11" },
                "Gloves" => new List<string> { "W18", "AG20" },
                "Cap" => new List<string> { "W27", "AG29" },
                _ => new List<string> { "L14", "AF14", "L25", "AF25" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] DStoDCAf(string sampleDescription)
        {
            List<string> stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
            .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "W6", "AG8" },
                "Fabric" => new List<string> { "J11", "AD11", "J22", "AD22" },
                "Socks" => new List<string> { "W6", "AG8" },
                "Gloves" => new List<string> { "W15", "AG17" },
                "Cap" => new List<string> { "W24", "AG26" },
                _ => new List<string> { "J11", "AD11", "J22", "AD22" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] AppearanceAf()
        {
            return new string[]
            {
                "BG12","BE23"
            };
        }
        public static string[] SpiralityAf()
        {
            return new string[]
            {
                "C5"
            };
        }

    }
}