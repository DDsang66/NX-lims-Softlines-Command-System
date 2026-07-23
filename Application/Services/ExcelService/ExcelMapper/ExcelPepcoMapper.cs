using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelPepcoMapper
    {
        //WET
        public static string[] MapWeight()
        {
            return new string[]
            {
                "A12", "A13","A14","A15","A16"
            };
       }
        public static string[] MapAppearance()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BA5", "BM13"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MappPrintDurability()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BA4", "BT12"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapDStoWashing(string sampleDescription)
        {
            List<string> stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap", "HomeTextile" }
            .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "G10" },
                "Fabric" => new List<string> { "AZ8", "BG8", "BN8", "BU8", "AW12", "BO12", "AW23", "BO23" },
                "HomeTextile" => new List<string> { "AZ8", "BG8", "BN8", "BU8", "AW12", "BO12", "AW23", "BO23" },
                "Socks" => new List<string> { "F10" },
                "Gloves" => new List<string> { "F19" },
                "Cap" => new List<string> { "F28" },
                _ => new List<string> { "AZ8", "BG8", "BN8", "BU8", "AW12", "BO12", "AW23", "BO23" }
            };


            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapWRL(string ItemName)
        {
            List<string>? map = null;
            switch (ItemName)
            {
                case "CF to Washing":
                    map = new List<string> { "D7", "F7", "H7", "L7", "N7","P7" };
                    break;
                case "CF to Rubbing":
                    map = new List<string> { "D21", "F21", "H21", "L21", "N21", "P21" };
                    break;
                case "CF to Light":
                    map = new List<string> { "C30","D30", "F30", "H30", "L30", "N30", "P30" };
                    break;
                default: break;
            }
            return map?.ToArray() ?? new string[0];
        }

        public static string[] MapPW(string ItemName)
        {
            List<string>? map = null;
            switch (ItemName)
            {
                case "CF to Water":
                    map = new List<string> { "D28", "F28", "H28", "J28", "L28", "N28" };
                    break;
                case "CF to Perspiration":
                    map = new List<string> { "D5", "F5", "H5", "J5", "L5", "N5", "D15", "F15", "H15", "J15", "L15", "N15" };
                    break;
                default: break;
            }
            return map?.ToArray() ?? new string[0];
        }
        //Physics
        public static string[] MapAttachment()
        {
            return new string[]
            {
                "AC3"
            };
        }
        public static string[] MapPilling()
        {
            return new string[]
            {
                "A8", "A15"
            };
        }
        public static string[] MapRepellency(string SampleDescription)
        {
            List<string>? map = null;
            map = new List<string>
                {
                    "A8","A9","A10","A15","A16","A17"
                };

            return map?.ToArray() ?? new string[0];
        }

        public static string[] MapHydroatatic()
        {
            return new string[]
            {
                "A10", "A12","A18","A20"
            };
        }
        public static string[] MapDryRate()
        {
            return new string[]
            {
                 "D12","D19","D26"
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

        public static string[] MapSeamSlippage(string sampleDescription)
        {
            List<string> stringMap = null;
            if (sampleDescription.Contains("Fabric"))
            {
                stringMap = new List<string> { "A12", "A14" };
            }
            else
            {
                stringMap = new List<string> { "D3" };
            }
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapAbsorbency()
        {
            return new string[]
            {
                "A10","A11","A12","A13","A14","A15"
            };
        }
      
        
        //AfterWash
        public static string[] DStoWashingAf(string SampleDescription) 
        {

            List<string> stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap", "HomeTextile" }
            .FirstOrDefault(key => SampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "W8","AG10" },
                "Fabric" => new List<string>  {"AZ13","BR13","AZ24","BR24"  },
                "HomeTextile" => new List<string> { "AZ13", "BR13", "AZ24", "BR24" },
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
            {
                "AZ11","BR11","AZ22","BR22"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
    }
}