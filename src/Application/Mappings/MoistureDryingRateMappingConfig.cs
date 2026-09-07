using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MoistureDryingRateContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    /// <summary>
    /// 水分干燥速率上下文（MoistureDryingRateContext）Mapster 映射配置。
    /// 只含 AATCC 201 校准参数一组映射（NF5022/测试记录无持久化对象，无映射需求）。
    /// 由 Program.cs 的 TypeAdapterConfig.GlobalSettings.Scan 自动发现注册。
    /// </summary>
    public class MoistureDryingRateMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // SaveDto -> Domain (新建聚合): 走 Create 工厂
            config.NewConfig<Aatcc201ConfigSaveDto, Aatcc201Config>()
                .MapWith(src => Aatcc201Config.Create(
                    new Aatcc201ConfigId(Guid.NewGuid()),
                    src.MachineNo,
                    src.TempHw1, src.TempHw2, src.TempBoard1, src.TempBoard2,
                    src.Wind1, src.Wind2,
                    src.SlopePoint, src.FlatPoint, src.SlopeDgNo, src.SlopeContinueNo, src.SlopeContinueTemp,
                    src.SetTemp, src.TempBoard1X, src.TempBoard2X, src.P, src.I, src.D,
                    src.UpdatedBy));

            // Domain -> Output DTO
            config.NewConfig<Aatcc201Config, Aatcc201ConfigDto>()
                .Map(dest => dest.Id, src => src.Id.Value)
                .Map(dest => dest.MachineNo, src => src.MachineNo)
                .Map(dest => dest.TempHw1, src => src.TempHw1)
                .Map(dest => dest.TempHw2, src => src.TempHw2)
                .Map(dest => dest.TempBoard1, src => src.TempBoard1)
                .Map(dest => dest.TempBoard2, src => src.TempBoard2)
                .Map(dest => dest.Wind1, src => src.Wind1)
                .Map(dest => dest.Wind2, src => src.Wind2)
                .Map(dest => dest.SlopePoint, src => src.SlopePoint)
                .Map(dest => dest.FlatPoint, src => src.FlatPoint)
                .Map(dest => dest.SlopeDgNo, src => src.SlopeDgNo)
                .Map(dest => dest.SlopeContinueNo, src => src.SlopeContinueNo)
                .Map(dest => dest.SlopeContinueTemp, src => src.SlopeContinueTemp)
                .Map(dest => dest.SetTemp, src => src.SetTemp)
                .Map(dest => dest.TempBoard1X, src => src.TempBoard1X)
                .Map(dest => dest.TempBoard2X, src => src.TempBoard2X)
                .Map(dest => dest.P, src => src.P)
                .Map(dest => dest.I, src => src.I)
                .Map(dest => dest.D, src => src.D)
                .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy);

            // Domain -> PO (写入数据库)
            config.NewConfig<Aatcc201Config, src.Infrastructure.Data.Persistence.Aatcc201Config>()
                .Map(dest => dest.Id, src => src.Id.Value)
                .Map(dest => dest.MachineNo, src => src.MachineNo)
                .Map(dest => dest.TempHw1, src => src.TempHw1)
                .Map(dest => dest.TempHw2, src => src.TempHw2)
                .Map(dest => dest.TempBoard1, src => src.TempBoard1)
                .Map(dest => dest.TempBoard2, src => src.TempBoard2)
                .Map(dest => dest.Wind1, src => src.Wind1)
                .Map(dest => dest.Wind2, src => src.Wind2)
                .Map(dest => dest.SlopePoint, src => src.SlopePoint)
                .Map(dest => dest.FlatPoint, src => src.FlatPoint)
                .Map(dest => dest.SlopeDgNo, src => src.SlopeDgNo)
                .Map(dest => dest.SlopeContinueNo, src => src.SlopeContinueNo)
                .Map(dest => dest.SlopeContinueTemp, src => src.SlopeContinueTemp)
                .Map(dest => dest.SetTemp, src => src.SetTemp)
                .Map(dest => dest.TempBoard1X, src => src.TempBoard1X)
                .Map(dest => dest.TempBoard2X, src => src.TempBoard2X)
                .Map(dest => dest.P, src => src.P)
                .Map(dest => dest.I, src => src.I)
                .Map(dest => dest.D, src => src.D)
                .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
                .Map(dest => dest.UpdatedBy, src => src.UpdatedBy);
        }
    }
}
