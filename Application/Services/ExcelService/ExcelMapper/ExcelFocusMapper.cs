using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelFocusMapper
    {
        #region WET
        public static string[] MapAppearance()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BA4", "BM13"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapAging()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BR3"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapSmoothnessAppearance()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "AX7", "BX7","BR7", "AX14", "BX14","BR14"
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
        public static string[] MapDStoDC()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "AZ6", "BG6", "BN6", "BU6", "AW10", "BO10", "AW20", "BO20"
            };
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
        public static string[] MapDStoSteam()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BA5", "BJ5", "BS5", "AR11", "AR16", "AR21"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapDStoIron()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BA6", "BJ6", "BS6", "AR12", "AR17", "AR22"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoRubbing()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D20", "F20", "H20", "L20", "N20", "P20"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoLight()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "C28","D28", "F28", "H28", "L28", "N28", "P28"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoSeaWater()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D41","F41", "H41", "L41", "N41", "P41"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoPerspiration()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D5", "F5", "H5", "J5", "L5", "N5","D15", "F15", "H15", "J15", "L15", "N15"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoWater()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D28", "F28", "H28", "J28", "L28", "N28"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoDC()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BA13", "BE13", "BI13", "BM13", "BQ13", "BU13"
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
        public static string[] MapCFtoYellow()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BA6", "BE6", "BI6", "BM6", "BQ6", "BU6"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapSpirality()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "A10","A11","A12"
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
                "A10", "A14"
            };
        }
        public static string[] MapYarnCount()
        {
            return new string[]
            {
                "A11"
            };
        }
        public static string[] MapYarnTwist()
        {
            return new string[]
            {
               "A11", "A16","A21"
            };
        }
        public static string[] MapWidth()
        {
            return new string[]
            {
               "A11", "A15","A19","A23"
            };
        }
        public static string[] MapWeave()
        {
            return new string[]
            {
               "R2"
            };
        }
        public static string[] MapBowSkew()
        {
            return new string[]
            {
               "A17","A20","A23","A26"
            };
        }
        public static string[] MapThickness()
        {
            return new string[]
            {
               "A39","A41","A43"
            };
        }
        public static string[] MapDryRate()
        {
            return new string[]
            {
                "D12","D18","D24"
            };
        }
        public static string[] MapPilling()
        {
            List<string> stringMap = null;

            stringMap = new List<string> { "A8", "A15" };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapAbrasion()
        {
            return new string[]
            {
                "H8","O8","V8","AC8", "H13","O13","V13","AC13"
            };
        }
        public static string[] MapSnagging()
        {
            return new string[]
            {
                "K30","Y30","R30","AF30"
            };
        }
        public static string[] MapSeam(string Standard)
        {
            List<string>? map = null;
            if (Standard.Contains("13936-1")) map = new List<string> { "A12", "A14" };
            else if (Standard.Contains("13936-2")) map = new List<string> { "A29","A31" };
            return map?.ToArray() ?? new string[0];
        }
        public static string[] MapZipperStrength()
        {
            return new string[]
            {
                "D5"
            };
        }
        public static string[] MapBond()
        {
            return new string[]
            {
                "A12","A16","A20"
            };
        }
        public static string[] MapBursting()
        {
            return new string[]
            {
                "A8","A10","A12","A14"
            };
        }
        public static string[] MapExtensionAndRecovery()
        {
            return new string[]
            {
                "A37"
            };
        }
        public static string[] MapHydrostatic()
        {
            return new string[]
            {
                "A10","A12", "A18","A20"
            };
        }
        public static string[] MapAbsorbency()
        {
            return new string[]
            {
                "A20","A21", "A22","A23","A24","A25"
            };
        }
        public static string[] MapElect()
        {
            return new string[]
            {
                "K8","R8", "Y8","AF8"
            };
        }
        public static string[] MapRepellency(string sampleDesc)
        {
            List<string>? map = null;
            if (sampleDesc.Contains("1 Wash")) map = new List<string> { "A15", "A16","A17" };
            else map = new List<string> { "A8", "A9", "A10" };
            return map?.ToArray() ?? new string[0];
        }
        public static string[] MapAttachmentStrength()
        {
            return new string[]
            {
                "AC3"
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
                "A4"
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
                "Garment" => new List<string> { "W8", "AG10" },
                "Fabric" => new List<string> { "AZ13", "BR13", "AZ24", "BR24" },
                "Socks" => new List<string> { "W8", "AG10" },
                "Gloves" => new List<string> { "W17", "AG19" },
                "Cap" => new List<string> { "W26", "AG28" },
                _ => new List<string> { "AZ13", "BR13", "AZ24", "BR24" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] DStoDCAf()
        {
            return new string[]
            { "AZ11", "BR11", "AZ21", "BR21" };
        }
        public static string[] AppearanceAf()
        {
            return new string[]
            {
                "BG5","BE13"
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