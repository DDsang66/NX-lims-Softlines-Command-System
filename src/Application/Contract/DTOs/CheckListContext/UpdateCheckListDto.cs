namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext
{
    public record UpdateCheckListDto : AddCheckListDto
    {
        /// <summary>
        /// CheckListId
        /// </summary>
        public Guid Id { get; set; }
    }
}
