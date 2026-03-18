using Microsoft.EntityFrameworkCore;
using DDiary.Data;
using DDiary.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DDiary.Repositories
{
    /// <summary>
    /// Implementazione EF Core del repository per i profili utente.
    /// </summary>
    public class ProfileRepository : IProfileRepository
    {
        private readonly DDiaryDbContext _context;

        public ProfileRepository(DDiaryDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfile?> GetByIdAsync(int id)
            => await _context.UserProfiles.FindAsync(id);

        public async Task<IEnumerable<UserProfile>> GetAllAsync()
            => await _context.UserProfiles.OrderBy(p => p.DisplayName).ToListAsync();

        public async Task<UserProfile?> GetByNameAsync(string displayName)
            => await _context.UserProfiles.FirstOrDefaultAsync(p => p.DisplayName == displayName);

        public async Task<UserProfile> AddAsync(UserProfile entity)
        {
            _context.UserProfiles.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<UserProfile> UpdateAsync(UserProfile entity)
        {
            _context.UserProfiles.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(int id)
        {
            var profile = await _context.UserProfiles.FindAsync(id);
            if (profile != null)
            {
                _context.UserProfiles.Remove(profile);
                await _context.SaveChangesAsync();
            }
        }
    }
}
