using System.Collections.Generic;
using System.Threading.Tasks;
using DDiary.Models;

namespace DDiary.Repositories
{
    /// <summary>
    /// Repository per i profili utente.
    /// </summary>
    public interface IProfileRepository : IRepository<UserProfile>
    {
        Task<UserProfile?> GetByNameAsync(string displayName);
    }
}
