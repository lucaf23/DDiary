using System.Collections.Generic;
using System.Threading.Tasks;

namespace DDiary.Repositories
{
    /// <summary>
    /// Repository generico con operazioni CRUD di base.
    /// </summary>
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(int id);
    }
}
