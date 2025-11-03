using DocumentFormat.OpenXml.Presentation;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class OrderDto
    {
        public List<OrderInput>? Rows { get; set; }
        public string? Remark { get; set; } 
        public string? Id { get; set; }
    }

    public class OrderInput
    {
        public string? ReportNum { get; set; }
        public string? OrderEntry { get; set; }
        public string? Express { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public int? Cs { get; set; }
        public string? Group { get; set; }
        public DateTimeOffset? LabIn { get; set; }
        public string? Remark { get; set; }
        //新增Remark字段，为每个Group单独提供备注,类型为string
    }


    public class OrderOutput
    {
        public string? ReportNum { get; set; }
        public string? OrderEntry { get; set; }
        public string? Cs { get; set; }
        public string? TestGroups { get; set; }
        public List<GroupOutput>? Groups { get; set; } = new();
    }

    public class GroupOutput
    {
        public string? RecordId { get; set; }
        public string? Express { get; set; }
        public string? Group { get; set; }
        public int TestSampleNum { get; set; }
        public int TestItemNum { get; set; }
        public string? Remark { get; set; }
        public string? Reviewer { get; set; }
        public DateTimeOffset? ReviewFinish { get; set; }
        public DateTimeOffset? LabIn { get; set; }
        public DateOnly DueDate { get; set; }
        public DateTimeOffset? LabOut { get; set; }
        public string? Status { get; set; }
    }
    public class OrderSummary
    {
        public string? RecordId { get; set; }
        public string? ReportNum { get; set; }
        public string? OrderEntry { get; set; }
        public string? Express { get; set; }
        public string? Cs { get; set; }
        public string? TestGroup { get; set; }
        public DateTimeOffset? ReviewFinish { get; set; }
        public string? Reviewer { get; set; }
        public DateOnly DueDate { get; set; }
        public DateTimeOffset? LabIn { get; set; }
        public DateTimeOffset? LabOut { get; set; }
        public int? TestSampleNum{ get; set; }
        public int? TestItemNum { get; set; }
        public string? Remark { get; set; }
        public string? Status { get; set; }
    }



    public sealed class PageResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();//存放后端响应的数据，类型为泛型，根据前端的需求提供不同的格式
        public int TotalCount { get; init; }//用于前端计算页面数量
        public int Page { get; init; }//当前页码
        public int PageSize { get; init; }//每页显示数量
        // 便捷只读属性
        public bool HasPrevious => Page > 1;//判断是否有上一页
        public bool HasNext => Page * PageSize < TotalCount;//判断是否有下一页
    }

    public class OrderQueryParams
    {
        public Dictionary<string, object>? QueryParam { get; set; }  // 查询值
        public int PageNum { get; set; }
        public int PageSize { get; set; }
    }

    public class OrderUpdateDto
    { 
        public List<OrderUpdate>? Rows { get; set; }
    }


    public class OrderUpdate
    {
        public string? RecordId { get; set; }  // LabTestInfo的主键

        // LabTestInfo表的字段
        public string? ReviewerId { get; set; }
        public string? TestEngineer { get; set; }
        public string? Status { get; set; }
        public string? TestGroup { get; set; }
        public int? TestSampleNum { get; set; }
        public int? TestItemNum { get; set; }
        public string? Remark { get; set; }
        public string? Express { get; set; }
        public string? DelayType { get; set; }
        public string? DelayReason { get; set; }

        // LabTestSchedule表的字段
        public DateTimeOffset? ReportDueDate { get; set; }
        public DateTimeOffset? OrderInTime { get; set; }
        public DateTimeOffset? ReviewFinishTime { get; set; }
        public DateTimeOffset? LabOutTime { get; set; }
    }


    public record OrderDeleteItem(string RecordId, string Reason);

    public record OrderDeleteRequest(
        IReadOnlyList<OrderDeleteItem> Items,
        string UserId
    );

    public sealed class LabTestJoinDto
    {
        public LabTestInfo Info { get; init; }
        public LabTestInfo Schedule { get; init; }
    }





    public class OrderCardOutput
    {
        public int? NeedLabOut { get; set; }
        public int? ActuallyLabOut { get; set; }
        public int? DelayLabOut { get; set; }
        public int? InAdvanceLabOut { get; set; }
        public int? NumOfSample { get; set; }
    }

    public class OrderFanCardOutput
    {
        public int? Delay { get; set; }
        public int? InAdvance { get; set; }
        public int? Normal { get; set; }
    }


    public class OrderLineCardOutput
    {
        public int[]? TimePropertyName { get; set; }

        public List<TimePropertyValue>? TimeProperty { get; set; }
    }

    public class TimePropertyValue 
    {
        public string? TimeHead { get; set; }
        public int[]? TimeValue { get; set; }
    }
}
