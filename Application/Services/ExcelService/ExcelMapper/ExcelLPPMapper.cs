using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelLPPMapper
    {
        #region WET
        public static string[] MapAppearance()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BA5", "BM13"
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
                "Garment" => new List<string> { "G10" },
                "Fabric" => new List<string> { "AZ8", "BG8", "BN8", "BU8", "AW12", "BO12", "AW23", "BO23" },
                "Socks" => new List<string> { "F10" },
                "Gloves" => new List<string> { "F19" },
                "Cap" => new List<string> { "F28" },
                _ => new List<string> { "AZ8", "BG8", "BN8", "BU8", "AW12", "BO12", "AW23", "BO23" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapCFtoWashing()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D7", "F7", "H7", "L7", "N7", "P7"
                // 可以根据需要添加更多固定的单元格地址
            };
        }

        public static string[] MapCFtoRubbing()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D21", "F21", "H21", "L21", "N21", "P21"
                // 可以根据需要添加更多固定的单元格地址
            };
        }

        public static string[] MapCFtoLight()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "C29","D29", "F29", "H29", "L29", "N29", "P29"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoSeaWater()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D36","F36", "H36", "L36", "N36", "P36"
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
                "D6", "G6", "J6", "M6", "P6", "S6"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoOrganic()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "H12","J12","L12","N12","P12","R12","T12"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapSpirality(string sampleDesc)
        {

            List<string> stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
            .FirstOrDefault(key => sampleDesc?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "A29", "A30", "A31" },
                "Fabric" => new List<string> { "A10", "A11", "A12" },
                _ => new List<string> {" A10", "A11", "A12"}
            };
            return stringMap?.ToArray() ?? new string[0];
            // 定义固定的单元格地址映射
        }
        #endregion
        //Physics
        public static string[] MapWeight()
        {
            return new string[]
            {
                "A12", "A13", "A14", "A15","A16"
            };
        }
        public static string[] MapPilling(string Standard)
        {
            List<string> stringMap = null;
            if (Standard.Contains("12945-1"))
            {
                stringMap = new List<string> { "A8", "A9", "A10" };
            }
            else if (Standard.Contains("12945-2"))
            {
                stringMap = new List<string> { "A18", "A25" };
            }
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapAbrasion()
        {
            return new string[]
            {
                "H8","O8","V8","AC8"
            };
        }
        public static string[] MapSeam(string ItemName,string SampleDescription)
        {
            List<string>? map = null;
            if (SampleDescription.Contains("Fabric")) map = new List<string> { "A10", "A12" };
            else if (SampleDescription.Contains("Garment") && SampleDescription.Contains("Knit")&& ItemName.Contains("Seam Strength")) map = new List<string> { "D5" };
            else if (SampleDescription.Contains("Garment")) map = new List<string> { "D3" };

            return map?.ToArray() ?? new string[0];
        }
        public static string[] MapZipperStrength()
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
                "A37"
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
                "I10","O10","U10","AA10","AG10"
            };
        }

        public static string[] MapAttachmentStrength()
        {
            return new string[]
            {
                "AC3"
            };
        }
        public static string[] MapDryRate()
        {
            return new string[]
            {
                "A8","A15","A22","A29"
            };
        }
        public static string[] MapTear()
        {
            return new string[]
            {
                "A14","A16","A18"
            };
        }
        public static string[] MapTensile()
        {
            return new string[]
            {
                "A11","A13","A15"
            };
        }






        //AfterWash
        public static string[] DStoWashingAf(string sampleDescription)
        {
            List<string> stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
            .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "W8", "AG10" },
                "Fabric" => new List<string> { "AZ13", "BR13", "AZ24", "BR24" },
                "Socks" => new List<string> { "W8", "AG10" },
                "Gloves" => new List<string> { "W17", "AG19" },
                "Cap" => new List<string> { "W26", "AG28" },
                _ => new List<string> { "AZ13", "BR13", "AZ24", "BR24" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] AppearanceAf()
        {
            return new string[]
            {
                "BG6","BE13"
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