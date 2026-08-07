using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext
{
    /// <summary>
    /// 物理称重记录聚合根(轻量, 单实体)
    /// </summary>
    public sealed class PhysicalWeightRecord : AggregateRoot<PhysicalWeightRecordId, Guid>
    {
        public int RecordIndex { get; private set; }

        /// <summary>试样编号</summary>
        public string? SampleId { get; private set; }

        /// <summary>试样测点</summary>
        public string? TestPoint { get; private set; }

        public decimal Weight { get; private set; }

        public decimal Area { get; private set; }

        public decimal Gsm { get; private set; }

        public decimal Oz { get; private set; }

        /// <summary>测试类型: area(面积克重) | length(长度克重) | piece(条重)</summary>
        public string? TestType { get; private set; }

        /// <summary>试样长度 cm(长度克重用)</summary>
        public decimal? LengthCm { get; private set; }

        /// <summary>条数(条重用: 称重条数或每打条数)</summary>
        public int? PieceCount { get; private set; }

        /// <summary>长度克重 g/m(前端已算)</summary>
        public decimal GPerM { get; private set; }

        /// <summary>长度克重 oz/yd(前端已算)</summary>
        public decimal OzPerYd { get; private set; }

        /// <summary>条重 g/piece(前端已算)</summary>
        public decimal GPerPiece { get; private set; }

        /// <summary>条重 lb/dozen(前端已算)</summary>
        public decimal LbPerDozen { get; private set; }

        public decimal? EnvTemperature { get; private set; }

        public decimal? EnvHumidity { get; private set; }

        public DateTime TestTime { get; private set; }

        public string? ReportNumber { get; private set; }

        public DateTime CreatedAt { get; private set; }

        private PhysicalWeightRecord() { }

        /// <summary>工厂: 创建新记录</summary>
        public static PhysicalWeightRecord Create(
            PhysicalWeightRecordId id, int recordIndex, string? sampleId,
            string? testPoint, decimal weight, decimal area, decimal gsm, decimal oz,
            string? testType, decimal? lengthCm, int? pieceCount,
            decimal gPerM, decimal ozPerYd, decimal gPerPiece, decimal lbPerDozen,
            decimal? envTemperature, decimal? envHumidity, DateTime testTime, string? reportNumber)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            return new PhysicalWeightRecord
            {
                Id = id, RecordIndex = recordIndex, SampleId = sampleId, TestPoint = testPoint,
                Weight = weight, Area = area, Gsm = gsm, Oz = oz,
                TestType = testType, LengthCm = lengthCm, PieceCount = pieceCount,
                GPerM = gPerM, OzPerYd = ozPerYd, GPerPiece = gPerPiece, LbPerDozen = lbPerDozen,
                EnvTemperature = envTemperature, EnvHumidity = envHumidity,
                TestTime = testTime, ReportNumber = reportNumber, CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>从数据库重建</summary>
        public static PhysicalWeightRecord Reconstitute(
            PhysicalWeightRecordId id, int recordIndex, string? sampleId,
            string? testPoint, decimal weight, decimal area, decimal gsm, decimal oz,
            string? testType, decimal? lengthCm, int? pieceCount,
            decimal gPerM, decimal ozPerYd, decimal gPerPiece, decimal lbPerDozen,
            decimal? envTemperature, decimal? envHumidity, DateTime testTime, string? reportNumber, DateTime createdAt)
        {
            return new PhysicalWeightRecord
            {
                Id = id, RecordIndex = recordIndex, SampleId = sampleId, TestPoint = testPoint,
                Weight = weight, Area = area, Gsm = gsm, Oz = oz,
                TestType = testType, LengthCm = lengthCm, PieceCount = pieceCount,
                GPerM = gPerM, OzPerYd = ozPerYd, GPerPiece = gPerPiece, LbPerDozen = lbPerDozen,
                EnvTemperature = envTemperature, EnvHumidity = envHumidity,
                TestTime = testTime, ReportNumber = reportNumber, CreatedAt = createdAt
            };
        }
    }
}
