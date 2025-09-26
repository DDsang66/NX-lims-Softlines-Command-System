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
        public DateOnly DueDate { get; set; }
        public int? Cs { get; set; }
        public string? Group { get; set; }
        public DateTimeOffset LabIn { get; set; }
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
        public long? RecodeId { get; set; }
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
        public string? ReportNum { get; set; }
        public string? OrderEntry { get; set; }
        public string? Express { get; set; }
        public string? Cs { get; set; }
        public string? TestGroup { get; set; }
        public DateTimeOffset? ReviewFinish { get; set; }
        public string? Reviewer { get; set; }
        public DateOnly DueDate { get; set; }
        public DateTimeOffset LabIn { get; set; }
        public DateTimeOffset? LabOut { get; set; }
        public int? TestSampleNum{ get; set; }
        public int? TestItemNum { get; set; }
        public string? Remark { get; set; }
        public string? Status { get; set; }
    }



    public sealed class PageResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();//存放后端响应的数据
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
        public long Id { get; set; }  // LabTestInfo的主键
        public long IdSchedule { get; set; }  // LabTestSchedule的主键

        // LabTestInfo表的字段
        public string? ReportNumber { get; set; }
        public string? Reviewer { get; set; }
        public string? TestEngineer { get; set; }
        public string? OrderEntryPerson { get; set; }
        public string? CustomerService { get; set; }
        public byte? Status { get; set; }
        public string? TestGroup { get; set; }
        public int? TestSampleNum { get; set; }
        public int? TestItemNum { get; set; }
        public string? Remark { get; set; }
        public string? Express { get; set; }
        public string? Describe { get; set; }
        public long ScheduleIndex { get; set; }

        // LabTestSchedule表的字段
        public DateOnly? ReportDueDate { get; set; }
        public DateTimeOffset? OrderInTime { get; set; }
        public DateTimeOffset? ReviewFinishTime { get; set; }
        public DateTimeOffset? LabOutTime { get; set; }

        // 通用字段
        public DateTimeOffset? LastUpdateTime { get; set; }
    }







}
