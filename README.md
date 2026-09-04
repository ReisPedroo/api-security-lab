# API Security Lab

Laboratório educacional de segurança de APIs, construído com **ASP.NET Core (.NET 10)**, **Entity Framework Core** e **PostgreSQL**.

## ⚠️ Aviso importante

Este projeto **introduz vulnerabilidades de propósito** para fins de aprendizado. Cada etapa documenta uma versão vulnerável de algo, explica por que ela é explorável e, em seguida, mostra a correção.

- **Não use este código como referência para um sistema em produção.**
- **Não exponha esta aplicação ou o banco de dados a redes públicas/internet.**
- Todo o laboratório foi pensado para rodar **apenas localmente**, em `localhost`, com dados fictícios.

## 📚 Sobre o projeto

A ideia é simples: primeiro construir uma API limpa e funcional (sem falhas conhecidas), e depois, etapa por etapa, introduzir vulnerabilidades comuns em APIs reais — demonstrar como explorá-las em um ambiente controlado, entender a causa raiz, e então corrigi-las.

### Roteiro

| Parte | Conteúdo | Status |
|---|---|---|
| 1 | Fundação: API limpa, PostgreSQL via Docker, CRUD básico de usuários | ✅ Concluída |
| 2 | SQL Injection: implementação vulnerável, exploração e correção | 🔜 Próxima |
| ... | Mais vulnerabilidades (a definir: auth quebrada, mass assignment, etc.) | 🔜 |

O passo a passo detalhado de cada etapa fica documentado nos arquivos `ApiSecurityLab_ParteN.md` na raiz do repositório.

## 🛠️ Stack

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core + Npgsql
- PostgreSQL (via Docker)
- Swagger / OpenAPI

## 🚀 Como rodar localmente

### Pré-requisitos
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Passos

1. Clone o repositório e entre na pasta:
   ```powershell
   git clone <url-do-repo>
   cd api-security-lab
   ```

2. Suba o banco PostgreSQL:
   ```powershell
   docker compose up -d
   ```

3. Configure a connection string local. O `appsettings.json` versionado **não** contém credenciais reais — para rodar localmente, crie/edite `ApiSecurityLab/appsettings.Development.json` (esse arquivo é ignorado pelo Git) com:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=api_security_lab;Username=postgres;Password=postgres"
     }
   }
   ```

4. Aplique as migrations:
   ```powershell
   cd ApiSecurityLab
   dotnet ef database update
   ```

5. Rode a aplicação:
   ```powershell
   dotnet run
   ```

6. Abra a URL HTTPS exibida no terminal — o Swagger estará disponível lá.

## 📄 Licença

Este projeto está sob a licença MIT — veja o arquivo [LICENSE](LICENSE) para mais detalhes.
