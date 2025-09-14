namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelJakoMapper
    {
        #region Physics
        public static string[] WeightMap()
        {
            return new string[]
            {
                "A12", "A13", "A14", "A15","A16"
            };
        }
        public static string[] PillingMap()
        {
            return new string[]
            {
                "A8", "A15"
            };
        }

        public static string[] ElasticMap()
        {
            return new string[]
            {
                "D37"
            };
        }

        public static string[] ASMap(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "Abrasion Resistance":
                    stringMap = new List<string> { "H8", "O8", "V8", "AC8"};
                    break;
                case "Snagging Resistance":
                    stringMap = new List<string> { "K25", "R25", "Y25", "AF25" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];

        }
        public static string[] SeamSlippageMap(string sampleDescription)
        {
            List<string> stringMap;
            var matched = new[] { "Garment", "Fabric"}
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "D4", "D14"},
                "Fabric" => new List<string> { "A10", "A12" },
                _ => new List<string> { "A10", "A12" }
            };

            return stringMap?.ToArray() ?? new string[0];

        }


        public static string[] SeamStrengthMap(string sampleDescription)
        {
            List<string> stringMap;
            var matched = new[] { "Knit", "Garment" }
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "D23", "D33" },
                "Knit" => new List<string> { "D5", "D15" },
                _ => new List<string> { "D23", "D33" }
            };

            return stringMap?.ToArray() ?? new string[0];

        }
        public static string[] BurstingMap()
        {
            return new string[]
            {
                "A9", "A10", "A11"
            };
        }
        public static string[] TensileMap()
        {
            return new string[]
            {
                "A36", "A38", "A40"
            };
        }

        public static string[] TearMap()
        {
            return new string[]
            {
                "A9","A11","A13"
            };
        }

        public static string[] WaterRepellencyMap()
        {
            return new string[]
            {
                "A8","A9","A10","A14","A15","A16"
            };
        }

        public static string[] HydrostaticMap()
        {
            return new string[]
            {
                "A10","A12"
            };
        }
        #endregion

        #region Wet
        public static string[] MapAppearance(string sampleDescription)
        {
            List<string> stringMap;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "G11","AU4", "BG23" },
                "Fabric" => new List<string> { "AZ9", "AW13", "CK5", "CT13" },
                "Socks" => new List<string> { "G10" },
                "Gloves" => new List<string> { "G19" },
                "Cap" => new List<string> { "G28" },
                _ => new List<string> { "AZ9", "AW13", "CT13" }
            };

            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapDStoDS(string sampleDescription)
        {
            List<string> stringMap;
            var matched = new[] { "Garment", "Fabric", "Socks", "Gloves", "Cap" }
                  .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            // 定义固定的单元格地址映射
            stringMap = matched switch
            {
                "Garment" => new List<string> { "F8" },
                "Fabric" => new List<string> { "K6", "R6", "Y6", "AG6", "F10", "Z10", "F21", "Z21" },
                "Socks" => new List<string> { "G8" },
                "Gloves" => new List<string> { "G17" },
                "Cap" => new List<string> { "G26" },
                _ => new List<string> { "K6", "R6", "Y6", "AG6", "F10", "Z10", "F21", "Z21" }
            };

            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapSI(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "CF to Sublimation in Storage":
                    stringMap = new List<string> { "D5", "F5" ,"H5", "L5", "N5", "P5" };
                    break;
                case "CF to Hot Pressing":
                    stringMap = new List<string> { "D16", "F16", "H16", "L16", "N16", "P16" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] MapWRL(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "CF to Washing":
                    stringMap = new List<string> { "D7", "F7", "H7", "J7", "L7", "N7", "P7" };
                    break;
                case "CF to Rubbing":
                    stringMap = new List<string> { "D21", "F21", "H21", "J21", "L21", "N21", "P21" };
                    break;
                case "CF to Light":
                    stringMap = new List<string> { "D30", "F30", "H30", "J30", "L30", "N30", "P30" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];

        }

        public static string[] MapPWD(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "CF to Perspiration":
                    stringMap = new List<string> { "D5", "F5", "H5", "J5", "L5", "N5", "D14", "F14", "H14", "J14", "L14", "N14" };
                    break;
                case "CF to Water":
                    stringMap = new List<string> { "D26", "F26", "H26", "J26", "L26", "N26" };
                    break;
                case "CF to Dry-clean":
                    stringMap = new List<string> { "D38", "F38", "H38", "J38", "L38", "N38" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];

        }


        public static string[] MapCSY(string ItemName)
        {
            List<string>? stringMap = null;
            switch (ItemName)
            {
                case "CF to Chlorinated Water":
                    stringMap = new List<string> { "F5", "H5", "J5", "L5", "N5", "P5"};
                    break;
                case "CF to Sea Water":
                    stringMap = new List<string> { "F11", "H11", "J11", "L11", "N11", "P11" };
                    break;
                case "Phenolic Yellowing":
                    stringMap = new List<string> { "F23", "H23", "J23", "L23", "N23", "P23" };
                    break;
                default: break;
            }
            return stringMap?.ToArray() ?? new string[0];

        }




        public static string[] MapPrint(string sampleDescription)
        {
            List<string> stringMap;
            var matched = new[] { "1st Bulk", "Repeat Order" }
                              .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            stringMap = matched switch
            {
                "1st Bulk" => new List<string> { "I12", "I22", "I32", "I42" },
                "Repeat Order" => new List<string> { "I12", "I22" },
                _ => new List<string> { "I12", "I22", "I32", "I42" }
            };
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapHeat()
        {
            return new string[]
            {
                "D5","F5","H5","J5","B9","J9","B20","J20"
            };
        }

        public static string[] MapSpirality(string sampleDescription)
        {
            List<string> stringSpirality;
            var matched = new[] { "Garment", "Fabric"}
                              .FirstOrDefault(key => sampleDescription?.Contains(key) == true);
            stringSpirality = matched switch
            {
                "Garment" => new List<string> { "A29", "A30", "A31" },
                "Fabric" => new List<string> { "A10", "A11", "A12" },
                _ => new List<string> { "A10", "A11", "A12" }
            };
            return stringSpirality?.ToArray() ?? new string[0];
        }
        #endregion
    }
}
