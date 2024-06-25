using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using web_groupware.Utilities;
#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629

namespace web_groupware.Models
{
    public class EmployeeListViewModel
    {
        public List<EmployeeViewModel> list_employee { get; set; } = new List<EmployeeViewModel>();
    }
}
