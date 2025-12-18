namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper
{
    public static class ExcelTchiboMapper
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
            List<string> stringMap=null;
            if (sampleDescription.Contains("Fabric"))
            {
                stringMap = new List<string> { "AZ8", "BG8", "BN8", "BU8", "AW12", "BO12", "AW23", "BO23" };
            }
            else 
            {
                stringMap = new List<string> { "G10"};
            }
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
                "C30","D30", "F30", "H30", "L30", "N30", "P30"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoSeaWater()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D37","F37", "H37", "L37", "N37", "P37"
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
        public static string[] MapCFtoSalivaSweat()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D4", "F4", "H4", "J4", "L4", "N4"
                // 可以根据需要添加更多固定的单元格地址
            };
        }

        public static string[] MapCFtoSublimation()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "G5", "J5", "M5", "P5", "S5"
                // 可以根据需要添加更多固定的单元格地址
            };
        }

        public static string[] MapCFtoHotPressing()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D16", "F16", "H16", "J16", "L16","N16","P16","R16","T16"
                // 可以根据需要添加更多固定的单元格地址
            };
        }
        public static string[] MapCFtoCl()
        {
            // 定义固定的单元格地址映射
            return new string[]
            {
                "D30", "G30", "J16", "M30", "P30", "S16"
                // 可以根据需要添加更多固定的单元格地址
            };
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

        public static string[] MapYarnCount()
        {
            return new string[]
            {
                "D10"
            };
        }
        public static string[] MapPilling(string sampleDescription)
        {
            List<string> stringMap = null;
            if (sampleDescription.Contains("Knit"))
            {
                stringMap = new List<string> { "A18", "A25" };
            }
            else
            {
                stringMap = new List<string> { "A18","A25" };
            }
            return stringMap?.ToArray() ?? new string[0];
        }

        public static string[] MapZipperStrength()
        {
            return new string[]
            {
                "D5"
            };
        }
        public static string[] MapUnsnapping()
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
                "D37"
            };
        }
        public static string[] MapHydrostaticPressing()
        {
            return new string[]
            {
                "A12","A14","A20","A22","A28","A30","A36","A38"
            };
        }

        public static string[] MapRepellency(string SampleDescription)
        {
            List<string>? map = null;
            if(SampleDescription.Contains("Before and After Wash")) map = new List<string>{  "A8","A9","A10", "A15","A16","A17" }; 
            else if(SampleDescription.Contains("Only After Wash") )map = new List<string> { "A15", "A16", "A17" };
            else if (SampleDescription.Contains( "Only Before Wash")) map = new List<string> { "A8", "A9", "A10"};
            return map?.ToArray() ?? new string[0];
        }
        public static string[] MapAirPermeability()
        {
            return new string[]
            {
                "I10","O10","U10","AA10","AG10"
            };
        }

        public static string[] MapAbsorbency()
        {
            return new string[]
            {
                "A10","A11","A12","A13","A14","A15"
            };
        }

        public static string[] MapAttachmentStrength()
        {
            return new string[]
            {
                "AC3"
            };
        }
        public static string[] MapDensity()
        {
            return new string[]
            {
                "A10","A14"
            };
        }

        public static string[] MapSeamSlippage(string sampleDescription)
        {
            List<string> stringMap = null;
            if (sampleDescription.Contains("Fabric"))
            {
                stringMap = new List<string> { "A10", "A12"};
            }
            else
            {
                stringMap = new List<string> { "D3" };
            }
            return stringMap?.ToArray() ?? new string[0];
        }






        //AfterWash
        public static string[] DStoWashingAf(string sampleDescription)
        {
            List<string> stringMap = null;
            if (sampleDescription.Contains("Fabric"))
            {
                stringMap = new List<string> { "AZ13", "BR13","AZ24","BR24" };
            }
            else
            {
                stringMap = new List<string> { "W8", "AG10" };
            }
            return stringMap?.ToArray() ?? new string[0];
        }
        public static string[] AppearanceAf()
        {
            return new string[]
            {
                "BG6","BE13"
            };
        }

        public static string[] SprayAf()
        {
            return new string[]
            {
                "C12"
            };
        }

    }
}