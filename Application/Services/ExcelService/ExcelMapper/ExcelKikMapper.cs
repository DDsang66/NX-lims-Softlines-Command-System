namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelKikMapper
    {
        //WET

        public static string[] MapAppearance()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BA5", "BQ13"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MappSpirality()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "AR10", "AR12","AR14", "AR16", "AR18"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapDStoWashing(string sampleDescription)
        {
            List<string> stringMap = null;
            if (sampleDescription.Contains("Bra"))stringMap = new List<string> { "D12" };
            else if (sampleDescription.Contains("Body/Allover suit")) stringMap = new List<string> { "D18" };
            else if (sampleDescription.Contains("Slip")) stringMap = new List<string> { "D30" };
            else if (sampleDescription.Contains("Shirt")) stringMap = new List<string> { "D12" };
            else if (sampleDescription.Contains("Pullover")) stringMap = new List<string> { "D12" };
            else if (sampleDescription.Contains("Top")) stringMap = new List<string> { "D19" };
            else if (sampleDescription.Contains("Undershirt")) stringMap = new List<string> { "D19" };
            else if (sampleDescription.Contains("Pants")) stringMap = new List<string> { "D25" };
            else if (sampleDescription.Contains("Skirt")) stringMap = new List<string> { "D31" };
            else if (sampleDescription.Contains("Dress")) stringMap = new List<string> { "D37" };
            else if (sampleDescription.Contains("Baby-body suits")) stringMap = new List<string> { "D12" };
            else if (sampleDescription.Contains("Bib overall")) stringMap = new List<string> { "D19" };
            else if (sampleDescription.Contains("Panty pants")) stringMap = new List<string> { "D33" };
            else if (sampleDescription.Contains("Tights")) stringMap = new List<string> { "D33" };
            else if (sampleDescription.Contains("Socks")) stringMap = new List<string> { "D26" };
            else if (sampleDescription.Contains("Caps")) stringMap = new List<string> { "D43" };
            else if (sampleDescription.Contains("Fabric and Home Textile")) stringMap = new List<string> { "AZ8", "BG8", "BN8", "BU8", "AW12", "BO12", "AW23", "BO23" };
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

        public static string[] MapSC(string ItemName)
        {
            List<string>? map = null;
            switch (ItemName)
            {
                case "CF to Sea Water":
                    map = new List<string> { "D4", "F4", "H4", "L4", "N4", "P4" };
                    break;
                case "CF to Chlorinated Water":
                    map = new List<string> { "C20", "D20", "F20", "H20", "L20", "N20", "P20" };
                    break;
                default: break;
            }
            return map?.ToArray() ?? new string[0];
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

        public static string[] MapDeterminationToFc()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "BD6"
                // 可以根据需要添加更多固定的单元格地址
            };
        }

        public static string[] DeterminationOfSize()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "AV9", "AV13", "AV17", "AV21", "AV25", "AV29", "AV33"
                // 可以根据需要添加更多固定的单元格地址
            };
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

        public static string[] MapHydroatatic()
        {
            return new string[]
            {
                "A10", "A12","A18","A20"
            };
        }
        public static string[] MapSpray()
        {
            return new string[]
            {
                "A8",  "A9",  "A10"
            };
        }
        public static string[] MapAir()
        {
            return new string[]
            {
                "I10","O10","U10","AA10","AG10"
            };
        }

        public static string[] MapWeight()
        {
            return new string[]
            {
                "A12", "A13", "A14", "A15","A16","A17", "A18", "A19", "A20"
            };
        }

        public static string[] MapYarnCount()
        {
            return new string[]
            {
                "D10"
            };
        }

        public static string[] MapZipper()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D5"
            };
        }

        public static string[] MapDensity()
        {
            return new string[]
            {
                "A10","A14"
            };
        }
        //AfterWash
        public static string[] DStoWashingAf() 
        {
            return new string[]
            {
                "AZ13","BR13","AZ24","BR24"
                // 可以根据需要添加更多固定的单元格地址
            };
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