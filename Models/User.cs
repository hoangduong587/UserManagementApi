namespace UserManagmentApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Salary { get; set; }
        public string Department { get; set; } = string.Empty;
    }
}