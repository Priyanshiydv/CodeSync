using Microsoft.EntityFrameworkCore;
using ProjectService.Data;
using ProjectService.DTOs;
using ProjectService.Interfaces;
using ProjectService.Models;

namespace ProjectService.Services
{
    /// <summary>
    /// Implements all project management operations including
    /// CRUD, forking, starring, archiving and member management.
    /// </summary>
    public class ProjectServiceImpl : IProjectService
    {
        private readonly ProjectDbContext _context;
        private readonly IProjectRepository _repository;

        public ProjectServiceImpl(
            ProjectDbContext context,
            IProjectRepository repository)
        {
            _context = context;
            _repository = repository;
        }

        public async Task<Project> CreateProject(int ownerId, CreateProjectDto dto)
        {
            var project = new Project
            {
                OwnerId = ownerId,
                Name = dto.Name,
                Description = dto.Description,
                Language = dto.Language,
                Visibility = dto.Visibility,
                TemplateId = dto.TemplateId
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Add owner as a member with OWNER role
            var member = new ProjectMember
            {
                ProjectId = project.ProjectId,
                UserId = ownerId,
                Role = "OWNER"
            };

            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();

            return project;
        }

        public async Task<Project?> GetProjectById(int projectId) =>
            await _repository.FindByProjectId(projectId);

        public async Task<List<Project>> GetProjectsByOwner(int ownerId) =>
            await _repository.FindByOwnerId(ownerId);

        public async Task<List<Project>> GetPublicProjects() =>
            await _repository.FindByVisibility("PUBLIC");

        public async Task<List<Project>> SearchProjects(string query) =>
            await _repository.SearchByName(query);

        public async Task<List<Project>> GetProjectsByMember(int userId) =>
            await _repository.FindByMemberUserId(userId);

        public async Task<Project> UpdateProject(int projectId, UpdateProjectDto dto)
        {
            var project = await _repository.FindByProjectId(projectId)
                ?? throw new Exception("Project not found!");

            if (dto.Name != null) project.Name = dto.Name;
            if (dto.Description != null) project.Description = dto.Description;
            if (dto.Language != null) project.Language = dto.Language;
            if (dto.Visibility != null) project.Visibility = dto.Visibility;

            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return project;
        }

        public async Task ArchiveProject(int projectId)
        {
            var project = await _repository.FindByProjectId(projectId)
                ?? throw new Exception("Project not found!");

            project.IsArchived = true;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProject(int projectId)
        {
            var project = await _repository.FindByProjectId(projectId)
                ?? throw new Exception("Project not found!");

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        public async Task<Project> ForkProject(int projectId, int newOwnerId)
        {
            var original = await _repository.FindByProjectId(projectId)
                ?? throw new Exception("Project not found!");

            // Create new project as a fork
            var forked = new Project
            {
                OwnerId = newOwnerId,
                Name = $"{original.Name}-fork",
                Description = $"Forked from {original.Name}",
                Language = original.Language,
                Visibility = "PRIVATE",
                TemplateId = original.ProjectId
            };

            _context.Projects.Add(forked);

            // Increment fork count on original
            original.ForkCount++;

            await _context.SaveChangesAsync();

            // Add new owner as member
            _context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = forked.ProjectId,
                UserId = newOwnerId,
                Role = "OWNER"
            });

            await _context.SaveChangesAsync();
            return forked;
        }

        public async Task StarProject(int projectId)
        {
            var project = await _repository.FindByProjectId(projectId)
                ?? throw new Exception("Project not found!");

            project.StarCount++;
            await _context.SaveChangesAsync();
        }

        public async Task<List<Project>> GetProjectsByLanguage(string language) =>
            await _repository.FindByLanguage(language);

        public async Task<List<ProjectMember>> GetMembers(int projectId) =>
            await _context.ProjectMembers
                .Where(m => m.ProjectId == projectId)
                .ToListAsync();

        public async Task AddMember(int projectId, AddMemberDto dto)
        {
            // Check if already a member
            var exists = await _context.ProjectMembers
                .AnyAsync(m => m.ProjectId == projectId
                    && m.UserId == dto.UserId);

            if (exists)
                throw new Exception("User is already a member!");

            _context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = projectId,
                UserId = dto.UserId,
                Role = dto.Role
            });

            await _context.SaveChangesAsync();
        }

        public async Task RemoveMember(int projectId, int userId)
        {
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId
                    && m.UserId == userId)
                ?? throw new Exception("Member not found!");

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }
}