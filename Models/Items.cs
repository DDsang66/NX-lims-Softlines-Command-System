using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
namespace NX_lims_Softlines_Command_System.Models
{
    public partial class Items
    {
        [Key]
        public int ItemID { get; set; }
        public string? ItemName { get; set; }
        public string? Type { get; set; }
    }
}