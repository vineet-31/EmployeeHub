using EmployeeHub.Data;
using EmployeeHub.Models;
using EmployeeHub.Services;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHub.Api.Services;

public record TokenResult(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt, string FullName, string Role);

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private const int RefreshTokenDays = 7;

    public AuthService(AppDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<TokenResult> IssueTokensAsync(Employee employee)
    {
        var accessToken = _tokenService.GenerateToken(employee);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            EmployeeId = employee.Id,
            TokenHash = _tokenService.HashToken(refreshToken),
            ExpiresAt = expiresAt
        });

        await _db.SaveChangesAsync();

        return new TokenResult(accessToken, refreshToken, expiresAt, employee.FullName, employee.Role.ToString());
    }

    public async Task<TokenResult?> RefreshAsync(string rawRefreshToken)
    {
        var hash = _tokenService.HashToken(rawRefreshToken);
        var existing = await _db.RefreshTokens
            .Include(rt => rt.Employee)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (existing is null) return null;

        if (!existing.IsActive)
        {
            await RevokeAllForEmployeeAsync(existing.EmployeeId);
            return null;
        }

        var newTokens = await IssueTokensAsync(existing.Employee);

        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByTokenHash = _tokenService.HashToken(newTokens.RefreshToken);
        await _db.SaveChangesAsync();

        return newTokens;
    }

    public async Task RevokeAllForEmployeeAsync(int employeeId)
    {
        var tokens = await _db.RefreshTokens
            .Where(rt => rt.EmployeeId == employeeId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var t in tokens)
            t.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}