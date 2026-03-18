using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DDiary.Models;
using DDiary.Repositories;

namespace DDiary.Services
{
    /// <summary>
    /// Servizio per la gestione dei profili utente.
    /// </summary>
    public interface IProfileService
    {
        Task<IEnumerable<UserProfile>> GetAllProfilesAsync();
        Task<UserProfile?> GetProfileAsync(int id);
        Task<UserProfile> CreateProfileAsync(string displayName);
        Task<UserProfile> UpdateProfileAsync(UserProfile profile);
        Task DeleteProfileAsync(int id);
    }

    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _profileRepo;

        public ProfileService(IProfileRepository profileRepo)
        {
            _profileRepo = profileRepo;
        }

        public Task<IEnumerable<UserProfile>> GetAllProfilesAsync() => _profileRepo.GetAllAsync();
        public Task<UserProfile?> GetProfileAsync(int id) => _profileRepo.GetByIdAsync(id);

        public async Task<UserProfile> CreateProfileAsync(string displayName)
        {
            var profile = new UserProfile
            {
                DisplayName = displayName,
                CreatedAt = DateTime.Now
            };
            return await _profileRepo.AddAsync(profile);
        }

        public Task<UserProfile> UpdateProfileAsync(UserProfile profile) => _profileRepo.UpdateAsync(profile);
        public Task DeleteProfileAsync(int id) => _profileRepo.DeleteAsync(id);
    }
}
