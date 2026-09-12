namespace EmployeeHub.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.Employee;
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
        public string? PhotoBlobName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
