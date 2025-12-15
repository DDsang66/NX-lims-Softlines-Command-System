namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class ParamsInput
    {
        public string? MenuName { get; set; }
        public string? ItemName { get; set; }
        public string? Standard { get; set; }
        public string? OrderNumber { get; set; }
        public string? WashingProcedure { get; set; }
        public string? DryProcedure { get; set; }
        public string? DCProcedure { get; set; }
        public string? Sci { get; set; }
        public string? Iron { get; set; }
        public string? IronMethod { get; set; }
        public string? Bleach { get; set; }
        public string? Detergent { get; set; }
        public List<string>? AfterWash { get; set; }
        public List<FiberDto>? FiberContent { get; set; }
        public string? additionalRequire { get; set; }
        public string? SampleDescription { get; set; }



        public ParamsInput CreateParamsInput(RequiredInfoDto requiredInfoDto,string itemName,string standard)
        {
            return new ParamsInput()
            {
                // 根据 requiredInfoDto 的属性来设置 ParamsInput 的属性
                MenuName = requiredInfoDto.menuName,
                ItemName = itemName,
                Standard =standard,
                OrderNumber = requiredInfoDto.reportNumber,
                WashingProcedure = requiredInfoDto.washingProcedure,
                DryProcedure = requiredInfoDto.dryProcedure,
                DCProcedure = requiredInfoDto.dcProcedure,
                Sci = requiredInfoDto.sci,
                Iron = requiredInfoDto.ironProcedure,
                IronMethod = requiredInfoDto.ironMethod,
                Bleach = requiredInfoDto.bleachProcedure,
                Detergent = requiredInfoDto.detergent,
                AfterWash = requiredInfoDto.afterWash,
                FiberContent = requiredInfoDto.fiberComposition,
                additionalRequire = requiredInfoDto.additionalRequire,
                SampleDescription = requiredInfoDto.sampleDescription,
            };
        }
    }

    public class FiberDto
    {
        public string? Composition { get; set; }
        public int Rate { get; set; }
    }


}
