\# CodeSync Backend — Online Code Collaboration Platform



!\[.NET](https://img.shields.io/badge/.NET-8.0-purple)

!\[ASP.NET Core](https://img.shields.io/badge/ASP.NET\_Core-8.0-blue)

!\[SQL Server](https://img.shields.io/badge/SQL\_Server-2022-red)

!\[Swagger](https://img.shields.io/badge/Swagger-OpenAPI\_3.0-green)

!\[JWT](https://img.shields.io/badge/Auth-JWT-orange)



> \*\*CodeSync\*\* — Code Together. Build Faster. Ship Smarter.



A full-stack \*\*Online Code Collaboration Platform\*\* built with \*\*ASP.NET Core 8.0 Microservices Architecture\*\*. CodeSync enables developers to write, execute, review, and version-control code in real time with teammates — all from a browser-based live editor.



\---



\## 📋 Table of Contents



\- \[About the Project](#about-the-project)

\- \[Architecture](#architecture)

\- \[Tech Stack](#tech-stack)

\- \[Microservices](#microservices)

\- \[Getting Started](#getting-started)

\- \[API Documentation](#api-documentation)

\- \[Git Branching Strategy](#git-branching-strategy)

\- \[Project Status](#project-status)



\---



\## 📖 About the Project



CodeSync is a microservices-based platform inspired by \*\*Replit\*\* and \*\*GitHub Codespaces\*\*. It supports three primary roles:



| Role | Description |

|---|---|

| \*\*Guest\*\* | Unauthenticated visitor — browse public projects in read-only mode |

| \*\*Developer\*\* | Registered user — create projects, write code, collaborate in real time |

| \*\*Admin\*\* | Platform administrator — manage users, projects, sessions and analytics |



\---



\## 🏗️ Architecture



CodeSync follows a \*\*Microservices Architecture\*\* where each service:

\- Runs \*\*independently\*\* on its own port

\- Has its \*\*own dedicated SQL Server database\*\*

\- Exposes a \*\*REST API\*\* with Swagger documentation

\- Uses \*\*JWT Authentication\*\* shared across all services

\- Communicates via \*\*HttpClient / IHttpClientFactory\*\*

CodeSync Backend

├── AuthService          (Port 5157) → CodeSync\_Auth DB

├── ProjectService       (Port 5257) → CodeSync\_Projects DB

└── FileService          (Port 5357) → CodeSync\_Files DB



\---



\## 🛠️ Tech Stack



| Layer | Technology |

|---|---|

| Backend Framework | ASP.NET Core 8.0 Web API |

| Architecture | Microservices |

| Database | SQL Server (EF Core 8.0) |

| Authentication | JWT (System.IdentityModel.Tokens.Jwt) |

| Password Hashing | BCrypt.Net-Next |

| API Documentation | Swashbuckle / Swagger OpenAPI 3.0 |

| IDE | VS Code |

| Version Control | Git / GitHub |



\---



\## 🔧 Microservices



\### ✅ 1. Auth Service — `CodeSync.Auth`

> Port: `5157` | Database: `CodeSync\_Auth`



The security gateway for the entire platform. Manages user registration, login, JWT token lifecycle, profile management, and role-based access control.



\*\*Key Components:\*\*

| Component | Description |

|---|---|

| `User` | Model with UserId, Username, Email, PasswordHash, Role, Provider, Bio |

| `IUserRepository` | Data access interface for user operations |

| `IAuthService` | Business contract for auth operations |

| `AuthServiceImpl` | JWT generation, BCrypt hashing, profile update |

| `AuthController` | REST endpoints for auth operations |



\*\*API Endpoints:\*\*

| Method | Endpoint | Description | Auth |

|---|---|---|---|

| POST | `/api/auth/register` | Register new user | ❌ |

| POST | `/api/auth/login` | Login and get JWT token | ❌ |

| POST | `/api/auth/logout` | Logout current user | ✅ |

| POST | `/api/auth/refresh` | Refresh JWT token | ❌ |

| GET | `/api/auth/profile` | Get current user profile | ✅ |

| PUT | `/api/auth/profile` | Update profile | ✅ |

| PUT | `/api/auth/password` | Change password | ✅ |

| GET | `/api/auth/search` | Search users by username | ✅ |

| PUT | `/api/auth/deactivate` | Deactivate user account | ✅ |

| GET | `/api/auth/roles` | Get all valid roles | ❌ |



\---



\### ✅ 2. Project Service — `CodeSync.Project`

> Port: `5257` | Database: `CodeSync\_Projects`



Manages top-level code project containers. Handles project CRUD, visibility, forking, starring, archiving and member management.



\*\*Key Components:\*\*

| Component | Description |

|---|---|

| `Project` | Model with ProjectId, OwnerId, Name, Language, Visibility, StarCount, ForkCount |

| `ProjectMember` | Join table linking users to projects with roles |

| `IProjectRepository` | Data access interface for project queries |

| `IProjectService` | Business contract for project operations |

| `ProjectServiceImpl` | CRUD, fork, star, archive, member management |

| `ProjectController` | REST endpoints for project operations |



\*\*API Endpoints:\*\*

| Method | Endpoint | Description | Auth |

|---|---|---|---|

| POST | `/api/projects` | Create new project | ✅ |

| GET | `/api/projects/{id}` | Get project by ID | ❌ |

| GET | `/api/projects/public` | Get all public projects | ❌ |

| GET | `/api/projects/owner/{id}` | Get projects by owner | ❌ |

| GET | `/api/projects/member` | Get projects by member | ✅ |

| GET | `/api/projects/language/{lang}` | Get projects by language | ❌ |

| GET | `/api/projects/search` | Search projects by name | ❌ |

| PUT | `/api/projects/{id}` | Update project | ✅ |

| PUT | `/api/projects/{id}/archive` | Archive project | ✅ |

| PUT | `/api/projects/{id}/star` | Star project | ✅ |

| DELETE | `/api/projects/{id}` | Delete project | ✅ |

| POST | `/api/projects/{id}/fork` | Fork project | ✅ |

| GET | `/api/projects/{id}/members` | Get project members | ❌ |

| POST | `/api/projects/{id}/members` | Add member | ✅ |

| DELETE | `/api/projects/{id}/members/{uid}` | Remove member | ✅ |



\---



\### ✅ 3. File Service — `CodeSync.File`

> Port: `5357` | Database: `CodeSync\_Files`



Manages the multi-file directory structure of each project. Supports nested folder hierarchies, soft-delete with restore, collaborative content attribution, and in-project search.



\*\*Key Components:\*\*

| Component | Description |

|---|---|

| `CodeFile` | Model with FileId, ProjectId, Name, Path, Content, Size, IsDeleted, IsFolder |

| `IFileRepository` | Data access interface with soft-delete filter support |

| `IFileService` | Business contract for file operations |

| `FileServiceImpl` | CRUD, soft-delete, restore, move, rename, file tree |

| `FileController` | REST endpoints for file operations |



\*\*API Endpoints:\*\*

| Method | Endpoint | Description | Auth |

|---|---|---|---|

| POST | `/api/files` | Create new file | ✅ |

| POST | `/api/files/createFolder` | Create new folder | ✅ |

| GET | `/api/files/{id}` | Get file by ID | ❌ |

| GET | `/api/files/project/{id}` | Get all files in project | ❌ |

| GET | `/api/files/{id}/content` | Get file content | ❌ |

| GET | `/api/files/tree/{projectId}` | Get full file tree | ❌ |

| GET | `/api/files/search/{projectId}` | Search within project | ❌ |

| PUT | `/api/files/{id}/content` | Update file content | ✅ |

| PUT | `/api/files/{id}/rename` | Rename file | ✅ |

| PUT | `/api/files/{id}/move` | Move file to new path | ✅ |

| DELETE | `/api/files/{id}` | Soft delete file | ✅ |

| POST | `/api/files/{id}/restore` | Restore deleted file | ✅ |



\---



\## 🚀 Getting Started



\### Prerequisites

✅ .NET 8.0 SDK

✅ SQL Server (SQLEXPRESS)

✅ Git

✅ VS Code

✅ Docker



\### Clone the Repository

```bash

git clone https://github.com/Priyanshiydv/CodeSync.git

cd CodeSync

```



\### Setup Each Service

```bash

\# Navigate to service folder

cd AuthService



\# Update connection string in appsettings.json

\# Change Server=YOUR\_SERVER\_NAME to your SQL Server instance



\# Run migrations

dotnet ef migrations add InitialCreate

dotnet ef database update



\# Run the service

dotnet run

```



\### Connection String Format

```json

"DefaultConnection": "Server=YOUR\_SERVER\_NAME\\\\SQLEXPRESS;Database=DB\_NAME;Trusted\_Connection=True;TrustServerCertificate=True;"

```



\---



\## 📚 API Documentation



Each service has its own Swagger UI:



| Service | Swagger URL |

|---|---|

| Auth Service | `http://localhost:5157/swagger` |

| Project Service | `http://localhost:5257/swagger` |

| File Service | `http://localhost:5357/swagger` |



\### JWT Authentication

1\. Call `POST /api/auth/login` to get token

2\. Click \*\*Authorize\*\* in Swagger UI

3\. Enter: `Bearer YOUR\_TOKEN\_HERE`



\---



\## 🌿 Git Branching Strategy

main (production)

└── dev

└── feature/auth-service

└── feature/project-service

└── feature/file-service



\---



\## 📊 Project Status



| Service | Status | Port | Database |

|---|---|---|---|

| Auth Service | ✅ Complete | 5157 | CodeSync\_Auth |

| Project Service | ✅ Complete | 5257 | CodeSync\_Projects |

| File Service | ✅ Complete | 5357 | CodeSync\_Files |



\---



\## 👩‍💻 Developer



\*\*Priyanshi Yadav\*\*

\- GitHub: \[@Priyanshiydv](https://github.com/Priyanshiydv)



\---



\*CodeSync Platform | 2026\*

