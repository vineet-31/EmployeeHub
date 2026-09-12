using EmployeeHub.Data;
using EmployeeHub.Dtos;
using EmployeeHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeHub.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DepartmentsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/departments — any authenticated user (e.g. populating a dropdown)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _db.Departments
            .Select(d => new DepartmentReadDto(d.Id, d.Name, d.Description, d.Employees.Count))
            .ToListAsync();

        return Ok(departments);
    }

    // GET /api/departments/5 — master-detail view: department + its employees
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var department = await _db.Departments
            .Where(d => d.Id == id)
            .Select(d => new DepartmentDetailDto(
                d.Id, d.Name, d.Description,
                d.Employees.Select(e => new EmployeeSummaryDto(e.Id, e.FullName, e.Email)).ToList()))
            .FirstOrDefaultAsync();

        if (department is null) return NotFound();
        return Ok(department);
    }

    // POST /api/departments — Admin only
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(DepartmentCreateDto dto)
    {
        var department = new Department { Name = dto.Name, Description = dto.Description };
        _db.Departments.Add(department);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = department.Id },
            new DepartmentReadDto(department.Id, department.Name, department.Description, 0));
    }

    // PUT /api/departments/5 — Admin only
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, DepartmentUpdateDto dto)
    {
        var department = await _db.Departments.FindAsync(id);
        if (department is null) return NotFound();

        department.Name = dto.Name;
        department.Description = dto.Description;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /api/departments/5 — Admin only
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _db.Departments.FindAsync(id);
        if (department is null) return NotFound();

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}