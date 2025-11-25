using DocumentFormat.OpenXml.Math;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelPrimarkMapper
    {
        #region WET
        public static string[] MapStability(string? sampleDescription)
        {
            // 定义固定的单元格地址映射
            List<string>? stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "G10"},
                "Fabric" => new List<string> { "AZ8", "BG8", "BN8", "BU8", "AW12", "BO12", "AW23", "BO23" },
                "Socks" => new List<string> { "F10" },
                "Gloves" => new List<string> { "F19" },
                "Cap" => new List<string> { "F28" },
                _ => new List<string> { "G10" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapBra()
        {
            return new string[]
            {
              "AW5","BN4"
            };
        }
        public static string[] MapStabilityToDryClean()
        {
            return new string[]
            {
                "AZ6", "BG6", "BN6", "BU6", "AW10", "BO10", "AW21", "BO21"
            };
        }

        public static string[] MapSpirality(string? sampleDescription)
        {
            // 定义固定的单元格地址映射
            List<string>? stringMap = null;
            var matched = new[] { "Garment", "Fabric"}
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "A29","A30" },
                "Fabric" => new List<string> { "A10", "A11" },
                _ => new List<string> { "A29","A30" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapCFtoEI(string? standard)
        {            
            // 定义固定的单元格地址映射
            List<string>? stringMap = null;
            switch (standard) 
            {
                case "AATCC TM124-2018te":
                    stringMap = new List<string> { "AX7", "BG7", "BP7", "AX14", "BG14", "BP14" };
                    break;
                case "ISO7769:2009":
                    stringMap = new List<string> { "AX26", "BG26", "BP26" };
                    break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapCFtoTD(string? itemName)
        {
            // 定义固定的单元格地址映射
            List<string>? stringMap = null;
            switch (itemName)
            {
                case "Dye Transfer in Storage":
                    stringMap = new List<string> { "BA5", "BE5", "BI5", "BM5", "BQ5", "BU5" };
                    break;
                case "TS Board Fit":
                    stringMap = new List<string> { "AR21", "AR22", "AR23","'AR24","AR25","AR26","AR27","AR28","AR29","AR30","AR31" };
                    break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapAppearance(string standard)
        {
            List<string>? stringMap = null;
            switch (standard)
            {
                case "PM01":
                    stringMap = new List<string> { "BH4","DA4", "BL11", "BH60", "DA60", "BL67", "BH117", "DA117", "BL124", "BH174", "DA174", "BL181" };
                    break;
                default: stringMap = new List<string> { "BH4", "DA4", "BL11" };
                    break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapDurability()
        {
            return new string[]
            {
                "BO4"
            };
        }

        public static string[] MapWRLW(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "Colour Fastness to Washing":
                    stringMap = new List<string> { "D7", "F7", "H7", "L7", "N7", "P7" };
                    break;
                case "Colour Fastness to Rubbing":
                    stringMap = new List<string> { "D21", "F21", "H21", "L21", "N21", "P21" };
                    break;
                case "Colour Fastness to Light":
                    stringMap = new List<string> { "D29", "F29", "H29", "L29", "N29", "P29" };
                    break;
                case "Colour Fastness to Water":
                    stringMap = new List<string> { "D36", "F36", "H36", "L36", "N36", "P36" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapSeaWaterPVC(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "Colour Fastness to PVC Migration":
                    stringMap = new List<string> { "D4", "F4", "H4", "L4", "N4", "P4" };
                    break;
                case "Colour Fastness to Sea Water":
                    stringMap = new List<string> { "D11", "F11", "H11", "L11", "N11", "P11" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }


        public static string[] MapPB(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "Colour Fastness to Perspiration":
                    stringMap = new List<string> { "D5", "F5", "H5","J5", "L5", "N5" , "D16", "F16", "H16", "J16", "L16", "N16" };
                    break;
                case "Colour Fastness to Chlorine Bleach":
                    stringMap = new List<string> { "D33", "F33", "H33", "J33", "L33", "N33" };
                    break;
                case "Colour Fastness to Non Chlorine Bleach":
                    stringMap = new List<string> { "D40", "F40", "H40", "J40", "L40", "N40", "D47", "F47", "H47", "J47", "L47", "N47" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapSPC(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "Colour Fastness to Sublimation in Storage":
                    stringMap = new List<string> { "G5", "J5", "M5", "P5", "S5" };
                    break;
                case "Colour Fastness to Hot Pressing":
                    stringMap = new List<string> { "D16", "F16", "H16", "J16", "L16", "N16" , "P16", "R16", "T16" };
                    break;
                case "Colour Fastness to Chlorinated Water":
                    stringMap = new List<string> { "D30", "G30", "J30", "M30", "P30", "S30" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];
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
        public static string[] MapYD(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "Phenolic Yellowing":
                    stringMap = new List<string> { "BA5", "BE5", "BI5", "BM5", "BQ5", "BU5" };
                    break;
                case "Colour Fastness to Dry Cleaning":
                    stringMap = new List<string> { "BA14", "BE14", "BI14", "BM14", "BQ14", "BU14" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }



        public static string[] MapAttachment()
        {
            return new string[]
            {
                "BD6"
            };
        }
        #endregion

        //AfterWash
        public static string[] StabilityAf(string? sampleDescription) 
        {
            // 定义固定的单元格地址映射
            List<string>? stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> {"W8", "AG10" },
                "Fabric" => new List<string> { "AZ13", "BR13", "AZ24", "BR24" },
                "Socks" => new List<string> { "W8", "AG10" },
                "Gloves" => new List<string> { "W17", "AG19" },
                "Cap" => new List<string> { "W26", "AG28" },
                _ => new List<string> { "W8", "AG10" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] DStoDCAf()
        {
            return new string[]
            {
                "AZ11","BR11","AZ22","BR22"
                // 可以根据需要添加更多固定的单元格地址
            };
        }

        public static string[] DurabilityAf()
        {
            return new string[]
            {
                "BE4"
                // 可以根据需要添加更多固定的单元格地址
            };
        }

        public static string[] AttachmentAf()
        {
            return new string[]
            {
                "AT8"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
    }
}