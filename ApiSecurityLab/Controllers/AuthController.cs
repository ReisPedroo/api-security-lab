using ApiSecurityLab.Data;
using ApiSecurityLab.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiSecurityLab.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Comparação com senha em texto puro é esperado neste ponto do roadmap
        // (hash com BCrypt só entra na Parte 6 — Sensitive Data Exposure).
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

        if (user is null)
            return Unauthorized();

        var token = GenerateToken(user);
        return Ok(new { token });
    }

    // VULNERÁVEL (broke authentication)

    //private string GenerateToken(User user)
    //{
    //    var secret = "12345678901234567890123456789012"; // segredo fraco e hardcoded no código
    //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    //    var token = new JwtSecurityToken(
    //       claims: new[] { new Claim(ClaimTypes.Name, user.Username) },
    //        signingCredentials: creds
    // sem "expires" -> token nunca expira
    //    );
    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}

    // Problemas:
    // 1) Segredo fraco/hardcoded -> pode ser quebrado por força bruta offline.
    // 2) Sem expiração -> um token vazado é válido para sempre.
    // 3) A validação no Program.cs (ver AuthController pareado com ela) não
    //    restringe algoritmos, então um atacante pode forjar um token com
    //    "alg": "none" e o servidor aceita sem checar assinatura nenhuma.

    // CORRIGIDO (broke authentication)
    private string GenerateToken(User user)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret não configurado");
    
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: new[] { new Claim(ClaimTypes.Name, user.Username) },
            expires: DateTime.UtcNow.AddMinutes(15), // expiração curta
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record LoginRequest(string Username, string Password);