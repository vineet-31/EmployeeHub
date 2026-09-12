using EmployeeHub.Api.Services;
using EmployeeHub.Data;
using EmployeeHub.Dtos;
using EmployeeHub.Models;
using EmployeeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string RefreshCookieName = "refreshToken";

    private readonly AppDbContext _db;
    private readonly AuthService _authService;

    public AuthController(AppDbContext db, AuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _db.Employees.AnyAsync(e => e.Email == dto.Email))
            return BadRequest("Email already registered.");

        var employee = new Employee
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            DepartmentId = dto.DepartmentId,
            Role = Role.Employee
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        var tokens = await _authService.IssueTokensAsync(employee);
        SetRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiresAt);

        return Ok(new AuthResponseDto(tokens.AccessToken, tokens.FullName, tokens.Role));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Email == dto.Email);

        if (employee is null || !PasswordHasher.Verify(dto.Password, employee.PasswordHash))
            return Unauthorized("Invalid email or password.");

        var tokens = await _authService.IssueTokensAsync(employee);
        SetRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiresAt);

        return Ok(new AuthResponseDto(tokens.AccessToken, tokens.FullName, tokens.Role));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var rawRefreshToken) || string.IsNullOrEmpty(rawRefreshToken))
            return Unauthorized("No refresh token provided.");

        var tokens = await _authService.RefreshAsync(rawRefreshToken);
        if (tokens is null)
        {
            Response.Cookies.Delete(RefreshCookieName);
            return Unauthorized("Invalid or expired refresh token.");
        }

        SetRefreshTokenCookie(tokens.RefreshToken, tokens.RefreshTokenExpiresAt);

        return Ok(new AuthResponseDto(tokens.AccessToken, tokens.FullName, tokens.Role));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var employeeId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _authService.RevokeAllForEmployeeAsync(employeeId);

        Response.Cookies.Delete(RefreshCookieName);
        return NoContent();
    }

    private void SetRefreshTokenCookie(string token, DateTimeOffset expiresAt)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresAt,
            Path = "/"
        });
    }
}