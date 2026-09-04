# API Security Lab - Parte 1: Fundação do Projeto

A ideia é não colocar nenhuma vulnerabilidade ainda. Primeiro vamos ter uma API limpa, PostgreSQL e Docker funcionando. Depois começamos a introduzir as vulnerabilidades de propósito.

## 1. Criar o projeto
No PowerShell, escolha uma pasta onde você guarda seus projetos e rode:

```powershell
mkdir api-security-lab
cd api-security-lab
dotnet new webapi -n ApiSecurityLab
cd ApiSecurityLab
```

Depois podemos limpar o template padrão:

```powershell
Remove-Item WeatherForecast.cs -ErrorAction SilentlyContinue
Remove-Item Controllers\WeatherForecastController.cs -ErrorAction SilentlyContinue
```

## 2. Instalar os pacotes do PostgreSQL
Dentro de `ApiSecurityLab`:

```powershell
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

E para as migrations:

```powershell
dotnet tool install --global dotnet-ef
```
*Se disser que a ferramenta já existe, pode ignorar.*

## 3. Estrutura que vamos montar
No final dessa primeira etapa:

```text
api-security-lab/
│
├── ApiSecurityLab/
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Services/
│   ├── Properties/
│   ├── Program.cs
│   ├── appsettings.json
│   └── ApiSecurityLab.csproj
│
├── docker-compose.yml
├── .gitignore
└── README.md
```
*Por enquanto, não vamos criar tudo manualmente. Vamos primeiro fazer a aplicação funcionar.*

## 4. Criar o modelo User
Crie o arquivo: `ApiSecurityLab/Models/User.cs`

Com o seguinte conteúdo:

```csharp
namespace ApiSecurityLab.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
```

⚠️ **Uma coisa importante**
Você deve ter percebido que `public string Password` está armazenando senha diretamente. Isso é proposital. Mais para frente, no laboratório, vamos transformar isso em uma vulnerabilidade e depois corrigir. Mas não faça isso em um sistema real.

## 5. Criar o DbContext
Crie o arquivo: `ApiSecurityLab/Data/AppDbContext.cs`

```csharp
using ApiSecurityLab.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiSecurityLab.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}
```

## 6. Configurar o PostgreSQL
Abra: `ApiSecurityLab/appsettings.json`

E deixe assim:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=api_security_lab;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Por enquanto estamos usando:
* **Username:** postgres
* **Password:** postgres

*Isso é somente para nosso ambiente local de desenvolvimento. Depois vamos tirar credenciais do código e usar variáveis de ambiente.*

## 7. Configurar o Program.cs
Abra: `ApiSecurityLab/Program.cs`

E coloque:

```csharp
using ApiSecurityLab.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
```

## 8. Criar o Docker Compose
Agora vem a parte legal. Na raiz do projeto (`api-security-lab/`), crie o arquivo `docker-compose.yml` com:

```yaml
services:
  postgres:
    image: postgres:17
    container_name: api-security-lab-postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB: api_security_lab
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```
*Agora o PostgreSQL não precisa estar instalado no Windows. O Docker vai baixar a imagem e executar o banco dentro de um container.*

## 9. Subir o PostgreSQL
Assim que o Docker Desktop terminar de instalar e estiver aberto, volte ao PowerShell na pasta `api-security-lab` e execute:

```powershell
docker compose up -d
```

Depois:

```powershell
docker ps
```

Você deverá ver algo parecido com:
```text
CONTAINER ID   IMAGE         STATUS          PORTS
xxxxxxxx       postgres:17   Up 10 seconds   0.0.0.0:5432->5432/tcp
```
*Se aparecer isso: PostgreSQL está funcionando. 🐘*

## 10. Criar a migration
Agora entre na API:

```powershell
cd ApiSecurityLab
```

Execute:

```powershell
dotnet ef migrations add InitialCreate
```

Depois:

```powershell
dotnet ef database update
```
*Isso vai criar nosso banco `api_security_lab` com a tabela `Users`.*

## 11. Criar nosso primeiro endpoint
Crie o arquivo: `ApiSecurityLab/Controllers/UsersController.cs`

```csharp
using ApiSecurityLab.Data;
using ApiSecurityLab.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
}
```
*Agora temos `GET /api/users` e `POST /api/users`.*

## 12. Rodar a API
Ainda dentro de `ApiSecurityLab/`, execute:

```powershell
dotnet run
```

Ele deverá mostrar algo parecido com:
```text
Now listening on: https://localhost:7xxx
Now listening on: http://localhost:5xxx
```

Abra a URL HTTPS no navegador. Você deverá encontrar o Swagger.
E poderemos testar:
`GET /api/Users` -> Resultado inicial: `[]`

Depois podemos criar um usuário pelo Swagger:
```json
{
  "username": "pedro",
  "email": "pedro@example.com",
  "password": "123456"
}
```
E o `GET` deverá retornar esse usuário.

## 🧠 O que acabamos de construir
Nossa arquitetura inicial é:

```text
                ┌──────────────┐
                │   Swagger    │
                └──────┬───────┘
                       │
                       ▼
              ┌─────────────────┐
              │   ASP.NET API   │
              └────────┬────────┘
                       │
                       ▼
              ┌─────────────────┐
              │ Entity Framework│
              └────────┬────────┘
                       │
                       ▼
              ┌─────────────────┐
              │   PostgreSQL    │
              │    Docker       │
              └─────────────────┘
```

E a partir daqui começa a parte realmente divertida. Na próxima etapa, podemos pegar esse endpoint e começar a criar deliberadamente a primeira vulnerabilidade:

**💉 SQL Injection**
Vamos fazer uma implementação vulnerável de propósito, demonstrar como ela pode ser explorada somente dentro do nosso laboratório local, entender por que funciona e depois criar a implementação segura.

*Antes disso, porém, eu recomendo você executar somente até o `docker compose up -d` depois que o Docker terminar de instalar. Se o Docker der qualquer erro, me manda o erro exatamente como aparecer e resolvemos antes de continuar.*
