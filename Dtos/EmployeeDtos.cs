namespace EmployeeHub.Dtos
{
    public record EmployeeReadDto(int Id, string FullName, string Email, string Role, int? DepartmentId, string? DepartmentName, string? PhotoUrl, DateTime CreatedAt);
    public record EmployeeUpdateDto(string FullName, int? DepartmentId, string Role);
}
