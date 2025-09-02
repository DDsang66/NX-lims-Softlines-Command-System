using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace NX_lims_Softlines_Command_System.Models
{

    public partial class Standards
    {
        [Key]
        public int ValueID { get; set; }
        public int ItemID { get; set; }
        public string? Value_data { get; set; }
    }
}
