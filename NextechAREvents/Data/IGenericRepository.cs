using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NextechAREvents.Data
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        DbSet<TEntity> GetDbSet();

        IQueryable<TEntity> All();

        IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>> filter);

        IEnumerable<TEntity> FindAll(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "");

        Task<IEnumerable<TEntity>> FindAllAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "");

        TEntity FindById(object id);

        Task<TEntity> FindByIdAsync(object id);

        Task InsertAsync(TEntity entity);

        Task DeleteAsync(object id);

        Task DeleteAsync(TEntity entityToDelete);

        Task UpdateAsync(TEntity entityToUpdate);
    }
}
