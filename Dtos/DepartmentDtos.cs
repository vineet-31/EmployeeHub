namespace EmployeeHub.Dtos
{
    public record DepartmentCreateDto(string Name, string Description);
    public record DepartmentUpdateDto(string Name, string Description);
    public record DepartmentReadDto(int Id, string Name, string Description, int EmployeeCount);
    public record DepartmentDetailDto(int Id, string Name, string Description, List<EmployeeSummaryDto> Employees);
    public record EmployeeSummaryDto(int Id, string FullName, string Email);
}
