using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class OrderDto
    {
        public List<OrderInput>? rows { get; set; }
        public string? remark { get; set; }
        public string? id { get; set; }
    }

    public class OrderInput
    {
        public string? reportNum { get; set; }
        public string? orderEntry { get; set; }
        public string? express { get; set; }
        public DateOnly dueDate { get; set; }
        public int? cs { get; set; }
        public string? group { get; set; }
        public DateTime labIn { get; set; }
    }


    public class OrderOutput
    {
        public string? reportNum { get; set; }
        public string? orderEntry { get; set; }
        public string? express { get; set; }
        public DateOnly dueDate { get; set; }
        public string? cs { get; set; }
        public string? testGroup { get; set; }
        public DateTime labIn { get; set; }
        public string? remark { get; set; }
        public string? status { get; set; }
    }


    public class OrderSummary
    {
        public string? ReportNum { get; set; }
        public string? OrderEntry { get; set; }
        public string? Express { get; set; }
        public string? Cs { get; set; }
        public string? Testgroup { get; set; }
        public DateTime? ReviewFinish { get; set; }
        public DateOnly DueDate { get; set; }
        public DateTime LabIn { get; set; }
        public DateTime? LabOut { get; set; }
        public int TestSampleNum{ get; set; }
        public int TestItemNum { get; set; }
        public string? Remark { get; set; }
        public string? Status { get; set; }
    }



    public sealed class PageResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }

        // 便捷只读属性
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page * PageSize < TotalCount;
    }
}
