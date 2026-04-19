# CodeSync Backend — Online Code Collaboration Platform

> **CodeSync** — Code Together. Build Faster. Ship Smarter.

A full-stack **Online Code Collaboration Platform** built with **ASP.NET Core 8.0 Microservices Architecture**. CodeSync enables developers to write, execute, review, and version-control code in real time with teammates — all from a browser-based live editor.

---

## 📋 Table of Contents

- [About the Project](#about-the-project)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Microservices](#microservices)
- [Getting Started](#getting-started)
- [API Documentation](#api-documentation)
- [Git Branching Strategy](#git-branching-strategy)
- [Project Status](#project-status)

---

## 📖 About the Project

CodeSync is a microservices-based platform inspired by **Replit** and **GitHub Codespaces**. It supports three primary roles:

| Role | Description |
|---|---|
| **Guest** | Unauthenticated visitor — browse public projects in read-only mode |
| **Developer** | Registered user — create projects, write code, collaborate in real time |
| **Admin** | Platform administrator — manage users, projects, sessions and analytics |

---

## 🏗️ Architecture

CodeSync follows a **Microservices Architecture** where each service:
- Runs **independently** on its own port
- Has its **own dedicated SQL Server database**
- Exposes a **REST API** with Swagger documentation
- Uses **JWT Authentication** shared across all services
- Communicates via **HttpClient / IHttpClientFactory**
- Uses **SignalR** for real-time communication
CodeSync Backend
├── AuthService          (Port 5157) → CodeSync_Auth DB
├── ProjectService       (Port 5257) → CodeSync_Projects DB
├── FileService          (Port 5357) → CodeSync_Files DB
├── CollabService        (Port 5457) → CodeSync_Collab DB
├── VersionService       (Port 5557) → CodeSync_Versions DB
├── ExecutionService     (Port 5657) → CodeSync_Execution DB
├── CommentService       (Port 5757) → CodeSync_Comments DB
└── NotificationService  (Port 5857) → CodeSync_Notifications DB

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Backend Framework | ASP.NET Core 8.0 Web API |
| Architecture | Microservices |
| Database | SQL Server (EF Core 8.0) |
| Authentication | JWT (System.IdentityModel.Tokens.Jwt) |
| Password Hashing | BCrypt.Net-Next |
| Real-time | ASP.NET Core SignalR |
| Code Execution | Docker.DotNet |
| Version Diff | DiffPlex (Myers Algorithm) |
| Email | MailKit |
| API Documentation | Swashbuckle / Swagger OpenAPI 3.0 |
| IDE | VS Code |
| Version Control | Git / GitHub |

---

## 🔧 Microservices

### ✅ 1. Auth Service — `CodeSync.Auth`
> Port: `5157` | Database: `CodeSync_Auth`

The security gateway for the entire platform. Manages user registration, login, JWT token lifecycle, profile management, and role-based access control.

**Key Components:**
| Component | Description |
|---|---|
| `User` | Model with UserId, Username, Email, PasswordHash, Role, Provider, Bio |
| `IUserRepository` | Data access interface for user operations |
| `IAuthService` | Business contract for auth operations |
| `AuthServiceImpl` | JWT generation, BCrypt hashing, profile update |
| `AuthController` | REST endpoints for auth operations |

**API Endpoints:**
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

---

### ✅ 2. Project Service — `CodeSync.Project`
> Port: `5257` | Database: `CodeSync_Projects`

Manages top-level code project containers. Handles project CRUD, visibility, forking, starring, archiving and member management.

**Key Components:**
| Component | Description |
|---|---|
| `Project` | Model with ProjectId, OwnerId, Name, Language, Visibility, StarCount, ForkCount |
| `ProjectMember` | Join table linking users to projects with roles |
| `IProjectRepository` | Data access interface for project queries |
| `IProjectService` | Business contract for project operations |
| `ProjectServiceImpl` | CRUD, fork, star, archive, member management |
| `ProjectController` | REST endpoints for project operations |

**API Endpoints:**
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

---

### ✅ 3. File Service — `CodeSync.File`
> Port: `5357` | Database: `CodeSync_Files`

Manages the multi-file directory structure of each project. Supports nested folder hierarchies, soft-delete with restore, collaborative content attribution, and in-project search.

**Key Components:**
| Component | Description |
|---|---|
| `CodeFile` | Model with FileId, ProjectId, Name, Path, Content, Size, IsDeleted, IsFolder |
| `IFileRepository` | Data access interface with soft-delete filter support |
| `IFileService` | Business contract for file operations |
| `FileServiceImpl` | CRUD, soft-delete, restore, move, rename, file tree |
| `FileController` | REST endpoints for file operations |

**API Endpoints:**
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

---

### ✅ 4. Collab Service — `CodeSync.Collab`
> Port: `5457` | Database: `CodeSync_Collab`

Manages real-time co-editing sessions. Each session is bound to a specific file tracked by a GUID. Uses SignalR for real-time cursor sharing and code broadcasting.

**Key Components:**
| Component | Description |
|---|---|
| `CollabSession` | Model with SessionId (Guid), ProjectId, FileId, Status, MaxParticipants |
| `Participant` | Model with UserId, Role, CursorLine, CursorCol, Color |
| `ICollabRepository` | Data access interface for session queries |
| `ICollabService` | Business contract for session operations |
| `CollabServiceImpl` | Session lifecycle, participant management, SignalR broadcast |
| `CollabHub` | SignalR Hub for real-time cursor and code events |
| `CollabController` | REST endpoints for session operations |

**API Endpoints:**
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/sessions` | Create session | ✅ |
| GET | `/api/sessions/{id}` | Get session by ID | ❌ |
| GET | `/api/sessions/project/{id}` | Get sessions by project | ❌ |
| GET | `/api/sessions/active/{fileId}` | Get active session | ❌ |
| GET | `/api/sessions/{id}/participants` | Get participants | ❌ |
| POST | `/api/sessions/{id}/join` | Join session | ✅ |
| POST | `/api/sessions/{id}/leave` | Leave session | ✅ |
| POST | `/api/sessions/{id}/end` | End session | ✅ |
| POST | `/api/sessions/{id}/kick` | Kick participant | ✅ |
| PUT | `/api/sessions/{id}/cursor` | Update cursor | ✅ |
| POST | `/api/sessions/{id}/broadcast` | Broadcast change | ✅ |

---

### ✅ 5. Version Service — `CodeSync.Version`
> Port: `5557` | Database: `CodeSync_Versions`

Git-inspired version control for project files. Each snapshot captures full file content with SHA-256 hash for integrity. Supports branching, tagging and DiffPlex diff computation.

**Key Components:**
| Component | Description |
|---|---|
| `Snapshot` | Model with SnapshotId, Hash (SHA-256), ParentSnapshotId, Branch, Tag |
| `ISnapshotRepository` | Data access interface for snapshot queries |
| `IVersionService` | Business contract for version operations |
| `VersionServiceImpl` | Snapshot CRUD, SHA-256 hashing, DiffPlex diff, branch, restore |
| `VersionController` | REST endpoints for version operations |

**API Endpoints:**
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/versions` | Create snapshot | ✅ |
| GET | `/api/versions/{id}` | Get snapshot by ID | ❌ |
| GET | `/api/versions/file/{fileId}` | Get snapshots by file | ❌ |
| GET | `/api/versions/project/{id}` | Get snapshots by project | ❌ |
| GET | `/api/versions/branch/{branch}` | Get snapshots by branch | ❌ |
| GET | `/api/versions/history/{fileId}` | Get file history | ❌ |
| GET | `/api/versions/latest/{fileId}` | Get latest snapshot | ❌ |
| POST | `/api/versions/{id}/restore` | Restore snapshot | ✅ |
| GET | `/api/versions/diff/{id1}/{id2}` | Diff two snapshots | ❌ |
| POST | `/api/versions/createBranch` | Create branch | ✅ |
| POST | `/api/versions/tag` | Tag snapshot | ✅ |

---

### ✅ 6. Execution Service — `CodeSync.Execution`
> Port: `5657` | Database: `CodeSync_Execution`

Manages secure code execution in isolated Docker sandbox containers. Jobs are processed asynchronously by a background worker. Supports 14 programming languages.

**Key Components:**
| Component | Description |
|---|---|
| `ExecutionJob` | Model with JobId (Guid), Status, Stdout, Stderr, ExitCode, ExecutionTimeMs |
| `SupportedLanguage` | Registry of 14 languages with Docker images |
| `IExecutionRepository` | Data access interface for job queries |
| `IExecutionService` | Business contract for execution operations |
| `ExecutionServiceImpl` | Job submission, cancellation, stats |
| `ExecutionWorker` | IHostedService background worker for Docker execution |
| `ExecutionController` | REST endpoints for execution operations |

**Supported Languages:**
Python, JavaScript, Java, C, C++, Go, Rust, TypeScript, PHP, Ruby, Kotlin, Swift, R, CSharp

**API Endpoints:**
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/executions/submit` | Submit code for execution | ✅ |
| GET | `/api/executions/{jobId}` | Get job by ID | ✅ |
| GET | `/api/executions/user/{id}` | Get jobs by user | ✅ |
| GET | `/api/executions/project/{id}` | Get jobs by project | ✅ |
| POST | `/api/executions/{jobId}/cancel` | Cancel job | ✅ |
| GET | `/api/executions/{jobId}/result` | Get job result | ✅ |
| GET | `/api/executions/languages` | Get supported languages | ❌ |
| GET | `/api/executions/languages/{lang}/version` | Get language version | ❌ |
| GET | `/api/executions/stats` | Get execution stats | ✅ |

---

### ✅ 7. Comment Service — `CodeSync.Comment`
> Port: `5757` | Database: `CodeSync_Comments`

Enables inline code review comments anchored to specific lines. Supports two-level threading, resolve/unresolve workflow and @mention parsing.

**Key Components:**
| Component | Description |
|---|---|
| `Comment` | Model with CommentId, LineNumber, ParentCommentId, IsResolved, SnapshotId |
| `ICommentRepository` | Data access interface for comment queries |
| `ICommentService` | Business contract for comment operations |
| `CommentServiceImpl` | CRUD, threading, resolve workflow, @mention parsing |
| `CommentController` | REST endpoints for comment operations |

**API Endpoints:**
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/comments` | Add comment | ✅ |
| GET | `/api/comments/{id}` | Get comment by ID | ❌ |
| GET | `/api/comments/file/{id}` | Get comments by file | ❌ |
| GET | `/api/comments/project/{id}` | Get comments by project | ❌ |
| GET | `/api/comments/{id}/replies` | Get replies | ❌ |
| GET | `/api/comments/file/{id}/line/{line}` | Get comments by line | ❌ |
| GET | `/api/comments/file/{id}/count` | Get comment count | ❌ |
| PUT | `/api/comments/{id}` | Update comment | ✅ |
| PUT | `/api/comments/{id}/resolve` | Resolve comment | ✅ |
| PUT | `/api/comments/{id}/unresolve` | Unresolve comment | ✅ |
| DELETE | `/api/comments/{id}` | Delete comment | ✅ |

---

### ✅ 8. Notification Service — `CodeSync.Notification`
> Port: `5857` | Database: `CodeSync_Notifications`

Dispatches and persists in-app and email alerts for all key collaboration events. Uses SignalR for real-time unread badge updates and MailKit for email delivery.

**Key Components:**
| Component | Description |
|---|---|
| `Notification` | Model with NotificationId, Type, ActorId, RelatedId, IsRead |
| `INotificationRepository` | Data access interface for notification queries |
| `INotificationService` | Business contract for notification operations |
| `NotificationServiceImpl` | Send, bulk, email via MailKit, read-state management |
| `NotificationHub` | SignalR Hub for real-time badge count updates |
| `NotificationController` | REST endpoints for notification operations |

**Notification Types:** SESSION_INVITE, COMMENT, MENTION, SNAPSHOT, FORK

**API Endpoints:**
| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/notifications/send` | Send notification | ✅ |
| POST | `/api/notifications/bulk` | Send bulk notification | ✅ |
| POST | `/api/notifications/email` | Send email | ✅ |
| GET | `/api/notifications/all` | Get all notifications | ✅ |
| GET | `/api/notifications/recipient/{id}` | Get by recipient | ✅ |
| GET | `/api/notifications/unread/{id}` | Get unread count | ✅ |
| PUT | `/api/notifications/{id}/read` | Mark as read | ✅ |
| PUT | `/api/notifications/read-all/{id}` | Mark all as read | ✅ |
| DELETE | `/api/notifications/{id}` | Delete notification | ✅ |
| DELETE | `/api/notifications/read/{id}` | Delete read notifications | ✅ |

---

## 🚀 Getting Started

### Prerequisites
✅ .NET 8.0 SDK
✅ SQL Server (SQLEXPRESS)
✅ Git
✅ VS Code
✅ Docker Desktop

### Clone the Repository
```bash
git clone https://github.com/Priyanshiydv/CodeSync.git
cd CodeSync
```

### Setup Each Service
```bash
# Navigate to service folder
cd AuthService

# Update connection string in appsettings.json
# Change Server=YOUR_SERVER_NAME to your SQL Server instance

# Run migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run the service
dotnet run
```

### Connection String Format
```json
"DefaultConnection": "Server=YOUR_SERVER_NAME\\SQLEXPRESS;Database=DB_NAME;Trusted_Connection=True;TrustServerCertificate=True;"
```

---

## 📚 API Documentation

Each service has its own Swagger UI:

| Service | Swagger URL |
|---|---|
| Auth Service | `http://localhost:5157/swagger` |
| Project Service | `http://localhost:5257/swagger` |
| File Service | `http://localhost:5357/swagger` |
| Collab Service | `http://localhost:5457/swagger` |
| Version Service | `http://localhost:5557/swagger` |
| Execution Service | `http://localhost:5657/swagger` |
| Comment Service | `http://localhost:5757/swagger` |
| Notification Service | `http://localhost:5857/swagger` |

### JWT Authentication
1. Call `POST /api/auth/login` to get token
2. Click **Authorize** in Swagger UI
3. Enter: `Bearer YOUR_TOKEN_HERE`

---

## 🌿 Git Branching Strategy
main (production)
└── dev
└── feature/auth-service        ✅
└── feature/project-service ✅
└── feature/file-service ✅
└── feature/collab-service ✅
└── feature/version-service ✅
└── feature/execution-service ✅
└── feature/comment-service ✅
└── feature/notification-service ✅

---

## 📊 Project Status

| Service | Status | Port | Database |
|---|---|---|---|
| Auth Service | ✅ Complete | 5157 | CodeSync_Auth |
| Project Service | ✅ Complete | 5257 | CodeSync_Projects |
| File Service | ✅ Complete | 5357 | CodeSync_Files |
| Collab Service | ✅ Complete | 5457 | CodeSync_Collab |
| Version Service | ✅ Complete | 5557 | CodeSync_Versions |
| Execution Service | ✅ Complete | 5657 | CodeSync_Execution |
| Comment Service | ✅ Complete | 5757 | CodeSync_Comments |
| Notification Service | ✅ Complete | 5857 | CodeSync_Notifications |

---

## 👩‍💻 Developer

**Priyanshi Yadav**
- GitHub: [@Priyanshiydv](https://github.com/Priyanshiydv)

---

*CodeSync Platform | 2026*
