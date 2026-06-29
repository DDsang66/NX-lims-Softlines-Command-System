namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public record StandardQueryConditionDto
    {
        /// <summary>
        /// 查询条件
        /// </summary>
        public Dictionary<string, object> ModifyQueryParameters { get;set; } = new Dictionary<string, object>();
    }
}
