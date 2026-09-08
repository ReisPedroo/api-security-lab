using System.Text;
using ApiSecurityLab.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// VULNERÁVEL (broken authentication)

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//   .AddJwtBearer(options =>
//    {
//        var secret = "12345678901234567890123456789012"; // mesmo segredo hardcoded do AuthController
//        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = false,
//            ValidateAudience = false,
//            IssuerSigningKey = key,
//            RequireSignedTokens = false,
//           RequireExpirationTime = false
//        };
//    });
//
//
// Problema: sem restringir explicitamente os algoritmos aceitos e sem
// validar a chave de assinatura, um atacante pode forjar um token
// (ex: header "alg": "none") e o servidor aceita como válido.

// CORRIGIDO (broken authentication)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret não configurado");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }, // rejeita "alg: none" e outros
            ValidateLifetime = true,
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // precisa vir ANTES de UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();