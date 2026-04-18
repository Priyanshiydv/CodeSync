using Microsoft.EntityFrameworkCore;
using ProjectService.Data;
using ProjectService.Interfaces;
using ProjectService.Models;

namespace ProjectService.Repositories
{
    /// <summary>
    /// Handles all database operations for Project entity.
    /// </summary>
    public class ProjectRepository : IProjectRepository
    {
        private readonly ProjectDbContext _context;

        public ProjectRepository(ProjectDbContext context)
        {
            _context = context;
        }

        public async Task<List<Project>> FindByOwnerId(int ownerId) =>
            await _context.Projects
                .Where(p => p.OwnerId == ownerId)
                .ToListAsync();

        public async Task<Project?> FindByProjectId(int projectId) =>
            await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        public async Task<List<Project>> FindByVisibility(string visibility) =>
            await _context.Projects
                .Where(p => p.Visibility == visibility)
                .ToListAsync();

        public async Task<List<Project>> FindByLanguage(string language) =>
            await _context.Projects
                .Where(p => p.Language == language)
                .ToListAsync();

        public async Task<List<Project>> SearchByName(string name) =>
            await _context.Projects
                .Where(p => p.Name.Contains(name))
                .ToListAsync();

        public async Task<List<Project>> FindByMemberUserId(int userId) =>
            await _context.Projects
                .Where(p => p.Members.Any(m => m.UserId == userId))
                .ToListAsync();

        public async Task<List<Project>> FindByIsArchived(bool isArchived) =>
            await _context.Projects
                .Where(p => p.IsArchived == isArchived)
                .ToListAsync();

        public async Task<int> CountByOwnerId(int ownerId) =>
            await _context.Projects
                .CountAsync(p => p.OwnerId == ownerId);
    }
}