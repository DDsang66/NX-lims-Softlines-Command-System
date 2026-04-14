using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class CheckListDto
    {
        public string? ItemName { get; set; }
        public string? MenuName { get; set; }

        public string? Standard { get; set; }
        public string? Parameter { get; set; }
        public string? Type { get; set; }
        public string? Sample { get; set; }
        public string? Method { get; set; }

        public string? sampleDescription { get; set; }

        public object? Extra { get; set; }


        public CheckListDto CreateDto(SelectedRows row,string menuName,string SampleDescription) 
        {
            return new CheckListDto 
            {
                ItemName = row.itemName,
                MenuName = menuName,
                Standard = row.standards,
                Parameter = row.parameters,
                Type = row.types,
                Sample = row.samples,
                Method = string.Empty,
                sampleDescription = SampleDescription,
                Extra = row.extra
            };
        }

    }
}