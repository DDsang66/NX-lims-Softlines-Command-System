using DocumentFormat.OpenXml.Math;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelOVSMapper
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
        public static string[] MapAppearance()
        {
            return new string[]
            {
              "BA5","BM13"
            };
        }

        public static string[] MapSteam()
        {
            return new string[]
            {
              "BA5","BJ5","BS5","AR11","AR15","AR19"
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
                "Garment" => new List<string> { "A26", "A27", "A28" },
                "Fabric" => new List<string> { "A10", "A11", "A12" },
                _ => new List<string> { "A26", "A27", "A28" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }


        public static string[] MapCS(string? itemName)
        {
            // 定义固定的单元格地址映射
            List<string>? stringMap = null;
            switch (itemName)
            {
                case "Calculation of Color Differences":
                    stringMap = new List<string> { "A13", "A14", "A15", "A16", "A17" };
                    break;
                case "Colour Fastness to Sublimation in Storage":
                    stringMap = new List<string> { "G24", "J24", "M24","'P24","S24" };
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

        public static string[] MapWLPS(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "Colour Fastness to Washing":
                    stringMap = new List<string> { "D7", "F7", "H7", "L7", "N7", "P7" };
                    break;
                case "Colour Fastness to Light":
                    stringMap = new List<string> { "D21", "F21", "H21", "L21", "N21", "P21" };
                    break;
                case "Colour Fastness to Migration on PVC":
                    stringMap = new List<string> { "D28", "F28", "H28", "L28", "N28", "P28" };
                    break;
                case "Colour Fastness to Sea Water":
                    stringMap = new List<string> { "D35", "F35", "H35", "L35", "N35", "P35" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }



        public static string[] MapPWR(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "Colour Fastness to Perspiration":
                    stringMap = new List<string> { "D5", "F5", "H5","J5", "L5", "N5" , "D15", "F15", "H15", "J15", "L15", "N15" };
                    break;
                case "Colour Fastness to Rubbing":
                    stringMap = new List<string> { "D41", "F41", "H41", "J41", "L41", "N41" };
                    break;
                case "Colour Fastness to Water":
                    stringMap = new List<string> { "D28", "F28", "H28", "J28", "L28", "N28" };
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
        public static string[] MapYCB(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "Phenolic Yellowing":
                    stringMap = new List<string> { "F5", "I5", "L5", "O5", "R5" };
                    break;
                case "Colour Fastness to Chlorinated Water":
                    stringMap = new List<string> { "D15", "G15", "J15", "M15", "P15", "S15" };
                    break;
                case "Colour Fastness to Chlorine Bleach":
                    stringMap = new List<string> { "D25", "G25", "J25", "M25", "P25", "S25" };
                    break;
                case "Colour Fastness to Non Chlorine Bleach":
                    stringMap = new List<string> { "D32", "G32", "J32", "M32", "P32", "S32", "D39", "G39", "J39", "M39", "P39", "S39" };
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

        #region PHY
        public static string[] MapWeight()
        {
            return new string[]
            {
                "A12", "A13", "A14", "A15","A16"
            };
        }
        public static string[] MapWidth()
        {
            return new string[]
            {
                "A9", "A11", "A13", "A15","A17","A19","A21","A23"
            };
        }
        public static string[] MapPilling(string standard)
        {
            List<string>? stringMap = null;
            if (standard!.Contains("12945-1")) stringMap = new List<string> { "A8" ,"A9","A10"};
            else if (standard!.Contains("12945-2")) stringMap = new List<string> { "A18","A25" };
            else stringMap = new List<string> { "A8", "A9", "A10" };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapRepellency()
        {
            List<string>? map = null;
            map = new List<string>
                {
                   "A8","A9","A10", "A11","A12","A13","A14","A15","A16"
                };

            return map?.ToArray() ?? new string[0];
        }

        public static string[] MapHydroatatic()
        {
            return new string[]
            {
               "A18","A20"//可能有洗前洗后
            };
        }
        public static string[] MapDryRate()
        {
            return new string[]
            {
                 "A14","A20","A26"
            };
        }

        public static string[] MapWicking()
        {
            return new string[]
            {
                 "A9", "A13","A17","A21"
            };
        }
        public static string[] MapAir()
        {
            return new string[]
            {
                "I10","O10","U10","AA10","AG10"
            };
        }
        public static string[] MapAccelerotor()
        {
            return new string[]
            {
                "D11","D18","D25","D32"
            };
        }
        public static string[] MapPhysicalMechanical(string? standard)
        {
            List<string>? stringMap = null;
            if (standard!.Contains("EN 71-1:2014+A1:2018 8.4")) stringMap = new List<string> { "AC3" };
            else if (standard!.Contains("ASTM F963-23")) stringMap = new List<string> { "AD3" };
            else stringMap = new List<string> { "AD3" };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapAttachmentStrength()
        {
            return new string[]
            {
                "AC3"
            };
        }
        public static string[] MapTorqueTension(string? standard)
        {
            List<string>? stringMap = null;
            switch (standard)
            {
                case "EN 71-1:2024+A1:2018":
                    stringMap = new List<string> { "A5" };
                    break;
                case "16 CFR 1500.51-53":
                    stringMap = new List<string> { "A6" };
                    break;
                default:
                    stringMap = new List<string> { "A6" };
                    break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapAbrasion()
        {
            List<string>? stringMap = null;
            stringMap = new List<string> { "H8","O8","V8","AC8" };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapSlippageStrength(string itemName,string sampleDescription)
        {
            List<string> stringMap = null;
            if (sampleDescription.Contains("Fabric"))
            {
                stringMap = new List<string> { "A12", "A14" };
            }
            else
            {
                if(itemName == "Seam Slippage")stringMap = new List<string> { "D3" };
                else if (itemName == "Seam Strength") stringMap = new List<string> { "D18" };
                else stringMap = new List<string> { "D3" };
            }
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapElastic()
        {
            return new string[]
            {
                "A37"
            };
        }

        public static string[] MapZipper()
        {
            return new string[]
            {
                "AE3"
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
        public static string[] MapBursting(string sampleDescription)
        {
            List<string> stringMap = null;
            if (sampleDescription.Contains("Fabric")) stringMap = new List<string> { "A9", "A10", "A11" };
            else if (sampleDescription.Contains("Seam")) stringMap = new List<string> { "A24", "A26", "A28", "A30" };
            else if (sampleDescription.Contains("Garment")) stringMap = new List<string> { "D3"};
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapAbsorbency()
        {
            return new string[]
            {
                "A10","A11","A12","A13","A14","A15"
            };
        }

        public static string[] MapMoisture()
        {
            return new string[]
            {
                "A14"
            };
        }
        public static string[] MapElectrostatic()
        {
            return new string[]
            {
                "K8","R8", "Y8","AF8"
            };
        }

        public static string[] MapDensity()
        {
            return new string[]
            {
                "A10","A14"
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

        public static string[] SpiralityAf()
        {
            return new string[]
            {
                "C5"
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
        public static string[] AppearanceAf()
        {
            return new string[]
            {
                "BG6","BE13"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] EasyCareAf(string? standard)
        {
            // 定义固定的单元格地址映射
            List<string>? stringMap = null;
            switch (standard)
            {
                case "AATCC TM124-2018te":
                    stringMap = new List<string> { "AT6", "AT13" };
                    break;
                case "ISO7769:2009":
                    stringMap = new List<string> { "AT25" };
                    break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }
    }
}