namespace web_groupware.Models
{
    public class EmployeeGroupModel
    {
        public int group_cd;
        public string? group_name;
        public List<int> staffs = new();
    }
}
