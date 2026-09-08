using ApiSecurityLab.Data;
using ApiSecurityLab.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ApiSecurityLab.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetUsers),
            new { id = user.Id },
            user
        );
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<User>>> SearchUsers([FromQuery] string username)
    {
        // VULNERÁVEL (SQL Injection)
        //
        // var sql = $"SELECT * FROM \"Users\" WHERE \"Username\" = '{username}'";
        // var users = await _context.Users.FromSqlRaw(sql).ToListAsync();
        //
        // Problema: o valor de "username" é concatenado direto na string SQL.
        // Um input como  ' OR '1'='1  quebra a query pretendida e vira:
        //   SELECT * FROM "Users" WHERE "Username" = '' OR '1'='1'
        // condição sempre verdadeira -> retorna todos os usuários, ignorando o filtro.
        // Também abre espaço para UNION SELECT vazar dados de outras tabelas.

        // CORRIGIDO (SQL injection)
        var users = await _context.Users
            .Where(u => u.Username == username)
            .ToListAsync();

        return Ok(users);
    }
}