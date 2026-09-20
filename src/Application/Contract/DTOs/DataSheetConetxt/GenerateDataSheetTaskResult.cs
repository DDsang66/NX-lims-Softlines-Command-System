namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt
{
    /// <summary>
    /// Result of the task of generating a data sheet
    /// </summary>
    /// <param name="BatchId"></param>
    /// <param name="CheckListId"></param>
    /// <param name="Total"></param>
    public record GenerateDataSheetTaskResult(
        Guid BatchId,
        Guid CheckListId,
        int Total);
}
