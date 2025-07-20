using Chatbot.Models;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Repositories
{
    public class BaseRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ChatbotContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(ChatbotContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync();
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.AsNoTracking().Where(x => !x.IsDeleted && EF.Property<int>(x, "Id") == id).FirstOrDefaultAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            entity.CreatedAt = DateTime.Now;
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync(); 
        }

        public virtual async Task UpdateAsync(T entity) 
        {
            entity.UpdatedAt = DateTime.Now;
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(); 
        }

        public virtual async Task DeleteAsync(T entity) 
        {
            entity.IsDeleted = true;
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}