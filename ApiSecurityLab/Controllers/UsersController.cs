using ApiSecurityLab.Data;
using ApiSecurityLab.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(int id)
    {
        // VULNERÁVEL (Broken Object Level Authorization / IDOR)

        // var user = await _context.Users.FindAsync(id);

        // if (user is null)
        //     return NotFound();

        // return Ok(user);

        // Problema: o endpoint exige [Authorize] (é preciso estar logado),
        // mas não verifica QUEM está logado, só QUE está logado.
        // Qualquer usuário autenticado pode trocar o {id} na URL e ler os
        // dados de qualquer outro usuário — ex: usuário 3 loga normalmente
        // e faz GET /api/Users/1, GET /api/Users/2, GET /api/Users/3...
        // varrendo a tabela inteira mesmo sem ter permissão para isso.
        // É o item #1 do OWASP API Security Top 10 (API1:2023 BOLA).

        // CORRIGIDO (Broken Object Level Authorization / IDOR)

        // O id do dono do token vem do claim NameIdentifier (setado no
        // AuthController ao gerar o JWT). Comparamos com o {id} pedido na
        // rota: só o próprio usuário pode ler o seu recurso.
        var authenticatedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (authenticatedUserId is null || authenticatedUserId != id.ToString())
            return Forbid();

        var user = await _context.Users.FindAsync(id);

        if (user is null)
            return NotFound();

        return Ok(user);
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