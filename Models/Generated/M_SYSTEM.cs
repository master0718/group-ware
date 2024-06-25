using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;

#pragma warning disable CS8600,CS8602,CS8604,CS8618

namespace web_groupware.Models
{
    public class M_SYSTEM
    {
        [Column(TypeName = "varchar(2)")]
        public string? system_id { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string? system_name { get; set; } 

        [Column(TypeName = "varchar(100)")]
        public string? system_url { get; set; } 
    }
}