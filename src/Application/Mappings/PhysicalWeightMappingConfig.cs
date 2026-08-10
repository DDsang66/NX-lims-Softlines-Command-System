using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class PhysicalWeightMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // DTO -> Domain (创建新聚合): 走 Create 工厂
            config.NewConfig<PhysicalWeightInputDto, Domain.Aggregeates.PhysicalWeightContext.PhysicalWeightRecord>()
                .MapWith(src => Domain.Aggregeates.PhysicalWeightContext.PhysicalWeightRecord.Create(
                    new PhysicalWeightRecordId(Guid.NewGuid()),
                    src.RecordIndex, src.SampleId, src.TestPoint, src.Weight, src.Area,
                    src.Gsm, src.Oz, src.TestType, src.LengthCm, src.PieceCount,
                    src.GPerM, src.OzPerYd, src.GPerPiece, src.LbPerDozen,
                    src.EnvTemperature, src.EnvHumidity,
                    src.TestTime, src.ReportNumber));

            // Domain -> Output DTO
            config.NewConfig<Domain.Aggregeates.PhysicalWeightContext.PhysicalWeightRecord, PhysicalWeightOutputDto>()
                .Map(dest => dest.Id, src => src.Id.Value)
                .Map(dest => dest.SampleId, src => src.SampleId)
                .Map(dest => dest.TestPoint, src => src.TestPoint)
                .Map(dest => dest.Weight, src => src.Weight)
                .Map(dest => dest.Area, src => src.Area)
                .Map(dest => dest.Gsm, src => src.Gsm)
                .Map(dest => dest.Oz, src => src.Oz)
                .Map(dest => dest.TestType, src => src.TestType)
                .Map(dest => dest.LengthCm, src => src.LengthCm)
                .Map(dest => dest.PieceCount, src => src.PieceCount)
                .Map(dest => dest.GPerM, src => src.GPerM)
                .Map(dest => dest.OzPerYd, src => src.OzPerYd)
                .Map(dest => dest.GPerPiece, src => src.GPerPiece)
                .Map(dest => dest.LbPerDozen, src => src.LbPerDozen)
                .Map(dest => dest.EnvTemperature, src => src.EnvTemperature)
                .Map(dest => dest.EnvHumidity, src => src.EnvHumidity)
                .Map(dest => dest.TestTime, src => src.TestTime)
                .Map(dest => dest.ReportNumber, src => src.ReportNumber)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt);

            // Domain -> PO (写入数据库)
            config.NewConfig<Domain.Aggregeates.PhysicalWeightContext.PhysicalWeightRecord, src.Infrastructure.Data.Persistence.PhysicalWeightRecord>()
                .Map(dest => dest.Id, src => src.Id.Value)
                .Map(dest => dest.SampleId, src => src.SampleId)
                .Map(dest => dest.TestPoint, src => src.TestPoint)
                .Map(dest => dest.Weight, src => src.Weight)
                .Map(dest => dest.Area, src => src.Area)
                .Map(dest => dest.GPerSqm, src => src.Gsm)
                .Map(dest => dest.OzPerSqyd, src => src.Oz)
                .Map(dest => dest.TestType, src => src.TestType)
                .Map(dest => dest.LengthCm, src => src.LengthCm)
                .Map(dest => dest.PieceCount, src => src.PieceCount)
                .Map(dest => dest.GPerM, src => src.GPerM)
                .Map(dest => dest.OzPerYd, src => src.OzPerYd)
                .Map(dest => dest.GPerPiece, src => src.GPerPiece)
                .Map(dest => dest.LbPerDozen, src => src.LbPerDozen)
                .Map(dest => dest.EnvTemperature, src => src.EnvTemperature)
                .Map(dest => dest.EnvHumidity, src => src.EnvHumidity)
                .Map(dest => dest.TestTime, src => src.TestTime)
                .Map(dest => dest.ReportNumber, src => src.ReportNumber)
                .Map(dest => dest.CreatedAt, src => src.CreatedAt);
        }
    }
}
