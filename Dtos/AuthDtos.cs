namespace EmployeeHub.Dtos
{
    public record RegisterDto(string FullName, string Email, string Password, int? DepartmentId);
    public record LoginDto(string Email, string Password);
    public record AuthResponseDto(string Token, string FullName, string Role);
}
