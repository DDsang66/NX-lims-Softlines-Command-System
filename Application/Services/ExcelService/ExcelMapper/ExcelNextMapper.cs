namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelNextMapper
    {
        #region WET
        public static string[] MapTM1TM2TM3(string itemName)
        {
            List<string> stringMap = null;
            switch (itemName) 
            {
                case "Fastness to Light":
                    stringMap = new List<string> { "D3", "F3", "H3", "L3", "N3", "P3" };
                    break;
                case "Fastness to Washing":
                    stringMap = new List<string> { "D12", "F12", "H12", "L12", "N12", "P12" };
                    break;
                case "Cross Staining to Washing":
                    stringMap = new List<string> { "D12", "F12", "H12", "L12", "N12", "P12" };
                    break;
                case "Fastness to Dry Cleaning":
                    stringMap = new List<string> { "D26", "F26", "H26", "L26", "N26", "P26" };
                    break;
                case "Cross Staining to Dry Cleaning":
                    stringMap = new List<string> { "D26", "F26", "H26", "L26", "N26", "P26" };
                    break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapTM4TM5TM36TM43(string itemName)
        {
            List<string> stringMap = null;
            switch (itemName)
            {
                case "Fastness to Water":
                    stringMap = new List<string> { "D4", "F4", "H4", "L4", "N4", "P4" };
                    break;
                case "Cross Staining to Water":
                    stringMap = new List<string> { "D4", "F4", "H4", "L4", "N4", "P4" };
                    break;
                case "Fastness to Chlorinated Water":
                    stringMap = new List<string> { "D18", "F18", "H18", "L18", "N18", "P18" };
                    break;
                case "Fastness to Rubbing":
                    stringMap = new List<string> { "D27", "F27", "H27", "L27", "N27", "P27" };
                    break;
                case "Phenolic Yellowing":
                    stringMap = new List<string> { "D36", "F36", "H36", "L36", "N36", "P36" };
                    break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapTM7TM7aTM7bTM7c()
        {
            return new string[]
            {
                "BQ3"
            };
        }

        public static string[] MapTM9TM9a()
        {
            return new string[]
            {
                "BD4","BM12"
            };
        }
        public static string[] MapTM12TM14(string sampleDescription)
        {
            List<string>? stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> {"F11"},
                "Fabric" => new List<string> { "AZ9", "BG9", "BN9", "BU9", "AW13", "BO13", "AW23", "BO23" },
                "Socks" => new List<string> { "F11" },
                "Gloves" => new List<string> { "F20" },
                "Cap" => new List<string> { "F29" },
                _ => new List<string> { "AZ9", "BG9", "BN9", "BU9", "AW13", "BO13", "AW23", "BO23" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapTM13(string sampleDescription)
        {
            List<string>? stringMap = null;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "AW41" },
                "Fabric" => new List<string> { "AW39" },
                "Socks" => new List<string> { "F46" },
                "Gloves" => new List<string> { "F46" },
                "Cap" => new List<string> { "F46" },
                _ => new List<string> { "AW39" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapTM11()
        {
            return new string[]
            {
                "BD4","BH4","BL4","BP4","BT4","BX4"
            };
        }

        public static string[] MapTM15TM24(string itemName)
        {
            List<string> stringMap = null;
            switch (itemName)
            {
                case "WIRA Steam Stability":
                    stringMap = new List<string> { "BA4", "BJ4", "BS4", "AR10", "AR14", "AR18" };
                    break;
                case "Assessment of Easy to Iron Fabrics":
                    stringMap = new List<string> { "AX32", "BH32", "BR32" };
                    break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapTM48()
        {
            return new string[]
            {
                "D4","F4","H4","J4","L4","N4"
            };
        }

        public static string[] MapTM52()
        {
            return new string[]
            {
                "D5","F5","H5","L5","N5","P5","D17","F17","H17","L17","N17","P17"
            };
        }

        public static string[] MapTM51()
        {
            return new string[]
            {
                "D3","F3","H3","J3","L3","N3"
            };
        }

        public static string[] MapTM55()
        {
            return new string[]
            {
                "D23","F23","H23","J23","L23","N23"
            };
        }
        #endregion

        #region Physics
        public static string[] MapTM62()
        {
            return new string[]
            {
                "A11", "A12", "A13", "A14","A15"
            };
        }
        public static string[] MapTM16()
        {
            return new string[]
            {
                "A11","A13"
            };
        }
        public static string[] MapTM16a()
        {
            return new string[]
            {
                "A7"
            };
        }
        public static string[] MapYarnCount()
        {
            return new string[]
            {
                "D10"
            };
        }

        public static string[] MapTM17()
        {
            return new string[]
            {
                "A10","A14"
            };
        }
        public static string[] MapTM18TM18a(string itemName)
        {
            List<string> stringMap = null;
            if (itemName.Contains("Abrasion Home")) stringMap = new List<string> { "E18", "E32" };
            else stringMap = new List<string> { "E4"};
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapTM19()
        {
            return new string[]
            {
                "D9"
            };
        }
        public static string[] MapTM21()
        {
            return new string[]
            {
                "A13","A17","A30","A34"
            };
        }
        public static string[] MapTM21a()
        {
            return new string[]
            {
                "A13","A17","A28","A36"
            };
        }
        public static string[] MapTM22TM23(string itemName)
        {
            List<string> stringMap = null;
            if (itemName.Contains("Bursting Strength")) stringMap = new List<string> { "A14","A16","A18" ,"A20","A22"};
            else if(itemName.Contains("Spray Test")) stringMap = new List<string> { "A34", "A36","A38" };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapTM25TM20(string itemName)
        {
            List<string> stringMap = null;
            if (itemName.Contains("Tearing Strength")) stringMap = new List<string> { "A8"};
            else if (itemName.Contains("Mass per Unit Area")) stringMap = new List<string> { "A30", "A32", "A34" };
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapTM26()
        {
            return new string[]
            {
                "AD6"
            };
        }
        public static string[] MapTM31()
        {
            return new string[]
            {
                "D10", "D17", "D24", "D31"
            };
        }
        public static string[] MapTM58()
        {
            return new string[]
            {
                "D9","E31","D56","E78"
            };
        }
        public static string[] MapTM59()
        {
            return new string[]
            {
                "E4"
            };
        }

        public static string[] MapTM63()
        {
            return new string[]
            {
                "A9","A11","A13","A15","A17","A19","A21","A23"
            };
        }

        public static string[] MapTM64TM65()
        {
            return new string[]
            {
                "A10","A14"
            };
        }
        public static string[] MapHydrostatic()
        {
            return new string[]
            {
                "A10","A12","A14", "A20","A22","A24"
            };
        }
        public static string[] MapAir()
        {
            return new string[]
            {
                "I10","O10","U10", "AA10"
            };
        }

        #endregion

        //AfterWash
        public static string[] WashingAf(string sampleDescription)
        {
            List<string> stringMap = null;
            if (sampleDescription.Contains("Fabric"))stringMap = new List<string> { "AZ14", "BR14", "AZ24", "BR24" };
            else if (sampleDescription.Contains("Garment")) stringMap = new List<string> { "W9", "AG11" };
            else if (sampleDescription.Contains("Socks")) stringMap = new List<string> { "W9", "AG11" };
            else if (sampleDescription.Contains("Glove")) stringMap = new List<string> { "W18", "AG20" };
            else if (sampleDescription.Contains("Cap")) stringMap = new List<string> { "W27", "AG29" };
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] AppearanceAf()
        {
            return new string[]
            {
                "BG6","BE13"
            };
        }

    }
}