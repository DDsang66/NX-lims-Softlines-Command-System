using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MenuContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class MenuMappingConfig:IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // DTO -> Domain (创建新聚合/实体)：使用 Create / 构造
            config.NewConfig<AddMenuDto, Menu>()
                .ConstructUsing(dto => Menu.Create(
                    new MenuId(dto.MenuId),
                    dto.MenuName,
                    dto.Remark,
                    new BuyerId(dto.BuyerId)
                ));

            // Domain -> DTO
            config.NewConfig<Menu, AddMenuDto>()
                .Map(dest => dest.MenuId, src => src.Id) // AggregateRootId 有隐式转换
                .Map(dest => dest.MenuName, src => src.MenuName)
                .Map(dest => dest.Remark, src => src.Remark)
                .Map(dest => dest.UpLoadTime, src => src.UpLoadTime)
                .Map(dest => dest.Status, src => src.Status.ToString())
                .Map(dest => dest.BuyerId, src => src.BuyerId);

            config.NewConfig<MenuItem, MenuItemDto>()
                .Map(dest => dest.TestItemId, src => src.TestItemId != null ? src.TestItemId.Value : null)
                .Map(dest => dest.BuyerModifiedTestItemId, src => src.BuyerModifiedTestItemId)
                // StandardIds 不直接 Map，在 AfterMapping 中处理
                .Map(dest => dest.BuyerModifiedTextMethodId, src => src.BuyerModifiedTextMethodId)
                .Map(dest => dest.BuyerModifiedGroup, src => src.BuyerModifiedGroup)
                .Map(dest => dest.Requirement, src => src.Requirement)
                .Map(dest => dest.BuyerOwnName, src => src.BuyerOwnName)
                .AfterMapping((src, dest) =>
                {
                    // 手动转换 StandardIds
                    dest.StandardIds = src.StandardIds?
                        .Where(s => s != null)
                        .Select(s => s!.Value)
                        .ToList()
                        ?? Enumerable.Empty<string>();
                });

            // Domain -> Persistence PO (写入数据库)
            config.NewConfig<Menu, BasicBuyerMenu>()
                .Map(dest => dest.MenuId, src => src.Id)
                .Map(dest => dest.MenuName, src => src.MenuName)
                .Map(dest => dest.Remark, src => src.Remark)
                .Map(dest => dest.Status, src => (byte)src.Status)
                .Map(dest => dest.UploadTime, src => src.UpLoadTime)
                .Map(dest => dest.BuyerCode, src => src.BuyerId);


            config.NewConfig<MenuItemDto, MenuItem>()
                .Map(dest => dest.TestItemId, src => src.TestItemId != null ? new TestItemId(src.TestItemId) : null)
                .Map(dest => dest.BuyerModifiedTestItemId, src => src.BuyerModifiedTestItemId)
                // StandardIds 不使用 Map，而是通过 AfterMapping 手动处理
                .Map(dest => dest.BuyerModifiedTextMethodId, src => src.BuyerModifiedTextMethodId)
                .Map(dest => dest.BuyerModifiedGroup, src => src.BuyerModifiedGroup)
                // Requirement 不走 Mapster（private set 不可靠），由 AppService 显式调用 UpdateRequirement 设置并校验
                .Map(dest => dest.BuyerOwnName, src => src.BuyerOwnName)
                // 新建（前端不传 Id → Guid.Empty）时生成新 Id；更新（前端传真实 Id）时保留
                .Map(dest => dest.Id, src => src.Id == Guid.Empty ? Guid.NewGuid() : src.Id)
                .AfterMapping((src, dest) =>
                {
                    // 手动转换 StandardIds
                    dest.StandardIds = src.StandardIds?
                        .Select(id => id != null ? new StandardId(id) : null)
                        .ToList()
                        ?? Enumerable.Empty<StandardId?>();
                });

            // MenuItem -> BasicMenuItem
            config.NewConfig<MenuItem, BasicMenuItem>()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.TestItemId, src => src.TestItemId != null ? src.TestItemId.Value : null)
                // StandardIds 集合转换：多个 StandardId 用逗号分隔存储
                .Map(dest => dest.StandardId, src => src.StandardIds != null && src.StandardIds.Any()
                    ? string.Join(",", src.StandardIds.Where(s => s != null).Select(s => s!.Value))
                    : null)
                .Map(dest => dest.BuyerOwnName, src => src.BuyerOwnName)
                .Map(dest => dest.BuyerModifiedTestItem, src => src.BuyerModifiedTestItemId)
                .Map(dest => dest.BuyerModifiedTestMethod, src => src.BuyerModifiedTextMethodId)
                .Map(dest => dest.Requirement, src => src.Requirement)
                .Map(dest => dest.BuyerModifiedGroup, src => src.BuyerModifiedGroup);


            config.NewConfig<BasicBuyerMenu, Menu>()
                .ConstructUsing(po => Menu.Reconstitute(
                    new MenuId(po.MenuId),
                    po.MenuName,
                    Array.Empty<MenuItem>(), // MenuItems 由仓储在查询时一并加载并手动设置/合并
                    po.Remark,
                    po.UploadTime,
                    (Status)po.Status,
                    new BuyerId(po.BuyerCode)
                ));

            // Menu -> MenuResponseDto（新增）
            config.NewConfig<Menu, MenuResponseDto>()
                .Map(dest => dest.MenuId, src => src.Id)
                .Map(dest => dest.MenuName, src => src.MenuName)
                .Map(dest => dest.MenuItems, src => src.MenuItems) // 自动映射 MenuItem -> MenuItemDto
                .Map(dest => dest.Remark, src => src.Remark)
                .Map(dest => dest.UpLoadTime, src => src.UpLoadTime)
                .Map(dest => dest.Status, src => src.Status.ToString());
        }
    }
}
