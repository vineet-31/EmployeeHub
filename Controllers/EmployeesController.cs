using EmployeeHub.Data;
using EmployeeHub.Dtos;
using EmployeeHub.Models;
using EmployeeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // every endpoint below requires SOME valid JWT, unless overridden
    public class EmployeesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly BlobStorageService _blobService;
        private readonly IConfiguration _config;

        public EmployeesController(AppDbContext db, BlobStorageService blobService, IConfiguration configuration)
        {
            _db = db;
            _blobService = blobService;
            _config = configuration;
        }

        // GET /api/employees  — Admin & Manager only
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAll()
        {
            var employees = new List<EmployeeReadDto>();
            
            var emps = await _db.Employees
                .Include(e => e.Department)
                .ToListAsync();

            foreach (var e in emps)
            {
                employees.Add(new EmployeeReadDto
                (
                    e.Id,
                    e.FullName,
                    e.Email,
                    e.Role.ToString(),
                    e.DepartmentId,
                    e.Department != null ? e.Department.Name : null,
                    e.PhotoBlobName != null ? _blobService.GetUrl(e.PhotoBlobName) : null,
                    e.CreatedAt
                ));
            }

            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var employee = await _db.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == id);
            if (employee is null) return NotFound();

            return Ok(new EmployeeReadDto(employee.Id, employee.FullName, employee.Email, employee.Role.ToString(),
                employee.DepartmentId, employee.Department?.Name, employee.PhotoBlobName != null ? _blobService.GetUrl(employee.PhotoBlobName) : null, employee.CreatedAt));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(int id, EmployeeUpdateDto dto)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee is null) return NotFound();

            employee.FullName = dto.FullName;
            employee.DepartmentId = dto.DepartmentId;

            if (Enum.TryParse<Role>(dto.Role, out var parsedRole))
                employee.Role = parsedRole;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/employees/5 — Admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee is null) return NotFound();

            _db.Employees.Remove(employee);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/employees/5/photo — self, or Admin/Manager
        [HttpPost("{id}/photo")]
        public async Task<IActionResult> UploadPhoto(int id, IFormFile file)
        {
            if (!IsSelfOrPrivileged(id)) return Forbid();

            if (file is null || file.Length == 0) return BadRequest("No file provided.");
            if (!file.ContentType.StartsWith("image/")) return BadRequest("Only image files are allowed.");
            if (file.Length > 5 * 1024 * 1024) return BadRequest("File too large (max 5MB).");

            var employee = await _db.Employees.FindAsync(id);
            if (employee is null) return NotFound();

            // Remove old photo if one exists
            if (!string.IsNullOrEmpty(employee.PhotoBlobName))
                await _blobService.DeleteAsync(employee.PhotoBlobName);

            var extension = Path.GetExtension(file.FileName);
            var blobName = $"{_config["Env"]}/{id}-{Guid.NewGuid()}{extension}";
            var url = await _blobService.UploadAsync(file, blobName);

            employee.PhotoBlobName = blobName;
            await _db.SaveChangesAsync();

            return Ok(new { photoUrl = url });
        }

        // DELETE /api/employees/5/photo — self, or Admin/Manager
        [HttpDelete("{id}/photo")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            if (!IsSelfOrPrivileged(id)) return Forbid();

            var employee = await _db.Employees.FindAsync(id);
            if (employee is null) return NotFound();
            if (string.IsNullOrEmpty(employee.PhotoBlobName)) return NoContent();

            await _blobService.DeleteAsync(employee.PhotoBlobName);
            employee.PhotoBlobName = null;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private bool IsSelfOrPrivileged(int employeeId)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Manager")) return true;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                                 ?? User.FindFirstValue("sub");

            return currentUserId is not null && int.Parse(currentUserId) == employeeId;
        }
    }
}
