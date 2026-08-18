namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_
{
    namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_
    {
        public record BuildReportDto
        {
            /// <summary>
            /// 报告号
            /// </summary>
            public string ReportNo { get; set; } = string.Empty;

            /// <summary>
            /// 标准
            /// </summary>
            public string Standard { get; set; } = string.Empty;

            /// <summary>
            /// 方法类别 (Method Category)
            /// </summary>
            public string MethodCategory { get; set; } = string.Empty;

            /// <summary>
            /// 测点
            /// </summary>
            public string Sample { get; set; } = string.Empty;

            /// <summary>
            /// 测试条件
            /// </summary>
            public string Condition { get; set; } = string.Empty;

            /// <summary>
            /// 清洁方法
            /// </summary>
            public string CleanMethod { get; set; } = string.Empty;

            /// <summary>
            /// 买家要求
            /// </summary>
            public string Requirement { get; set; } = string.Empty;

            // ========== 新增字段 ==========

            /// <summary>
            /// M1常量值
            /// </summary>
            public decimal? M1Constant { get; set; }

            /// <summary>
            /// M2常量值
            /// </summary>
            public decimal? M2Constant { get; set; }

            /// <summary>
            /// 磨耗里程 (Abrasion Distance): full/half/quarter
            /// </summary>
            public string AbrasionDistance { get; set; } = string.Empty;

            /// <summary>
            /// 测试样品密度数据
            /// </summary>
            public List<TestDensityData> TestDensities { get; set; } = new();

            /// <summary>
            /// 参照化合物密度数据
            /// </summary>
            public List<RefDensityData> RefDensities { get; set; } = new();

            /// <summary>
            /// 磨耗试样数据 (3个试样)
            /// </summary>
            public List<AbrasionSpecimenData> AbrasionSpecimens { get; set; } = new();

            /// <summary>
            /// 报告生成时间
            /// </summary>
            public DateTime GeneratedAt { get; set; } = DateTime.Now;

            /// <summary>
            /// 操作人
            /// </summary>
            public string Operator { get; set; } = string.Empty;
        }

        /// <summary>
        /// 测试样品密度数据 (Specimen A/B)
        /// </summary>
        public record TestDensityData
        {
            /// <summary>
            /// 试样名称: A 或 B
            /// </summary>
            public string Specimen { get; set; } = string.Empty;

            /// <summary>
            /// 空气中重量 m1 (g)
            /// </summary>
            public decimal? M1 { get; set; }

            /// <summary>
            /// 水中重量 m2 (g)
            /// </summary>
            public decimal? M2 { get; set; }

            /// <summary>
            /// 计算密度结果 (自动计算)
            /// </summary>
            public decimal? Density { get; set; }
        }

        /// <summary>
        /// 参照化合物密度数据 (Specimen A/B)
        /// </summary>
        public record RefDensityData
        {
            /// <summary>
            /// 试样名称: A 或 B
            /// </summary>
            public string Specimen { get; set; } = string.Empty;

            /// <summary>
            /// 空气中重量 m1 (g)
            /// </summary>
            public decimal? M1 { get; set; }

            /// <summary>
            /// 水中重量 m2 (g)
            /// </summary>
            public decimal? M2 { get; set; }

            /// <summary>
            /// 计算密度结果 (自动计算)
            /// </summary>
            public decimal? Density { get; set; }
        }

        /// <summary>
        /// 磨耗试样数据 (3个试样)
        /// </summary>
        public record AbrasionSpecimenData
        {
            /// <summary>
            /// 试样编号: 1, 2, 3
            /// </summary>
            public int SpecimenNumber { get; set; }

            /// <summary>
            /// 磨损前重量 W1 (g)
            /// </summary>
            public decimal? BeforeWeight { get; set; }

            /// <summary>
            /// 磨损后重量 W2 (g)
            /// </summary>
            public decimal? AfterWeight { get; set; }

            /// <summary>
            /// 质量损失 (g) - 自动计算
            /// </summary>
            public decimal? MassLoss { get; set; }

            /// <summary>
            /// 相对体积损失 (mm³) - 自动计算
            /// </summary>
            public decimal? VolLoss { get; set; }

            /// <summary>
            /// 磨耗指数 (AR Index) - 自动计算
            /// </summary>
            public decimal? ARIndex { get; set; }
        }

        /// <summary>
        /// M1/M2常量修改记录
        /// </summary>
        public record ConstantModificationDto
        {
            /// <summary>
            /// 常量类型: M1 或 M2
            /// </summary>
            public string Type { get; set; } = string.Empty;

            /// <summary>
            /// 修改后的值
            /// </summary>
            public decimal? Value { get; set; }

            /// <summary>
            /// 修改人
            /// </summary>
            public string Modifier { get; set; } = string.Empty;

            /// <summary>
            /// 修改原因
            /// </summary>
            public string Reason { get; set; } = string.Empty;

            /// <summary>
            /// 修改时间
            /// </summary>
            public DateTime ModifiedAt { get; set; } = DateTime.Now;
        }
    }
}
