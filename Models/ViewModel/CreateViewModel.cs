using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace web_groupware.Models
{
    public class CreateViewModel
    {
        public T_GROUPM GroupStaff { get; set; }
        public IEnumerable<M_STAFF> StaffList { get; set; }
        public List<int> SelectedStaffIds { get; set; } // New property to hold selected staff IDs
    }
}
