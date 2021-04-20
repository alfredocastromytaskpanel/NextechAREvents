using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace NextechAREvents.Data
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        internal EventContext context;
        internal DbSet<TEntity> dbSet;

        public GenericRepository(EventContext context)
        {
            this.context = context;
            this.dbSet = context.Set<TEntity>();
        }

        public virtual DbSet<TEntity> GetDbSet()
        {
            return dbSet;
        }

        public virtual IQueryable<TEntity> All()
        {
            return dbSet.AsQueryable();
        }

        public virtual IQueryable<TEntity> FindAll(Expression<Func<TEntity, bool>> filter)
        {
            return dbSet.Where(filter);
        }

        public virtual IEnumerable<TEntity> FindAll(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<TEntity> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return orderBy(query).ToList();
            }
            else
            {
                return query.ToList();
            }
        }

        public virtual async Task<IEnumerable<TEntity>> FindAllAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<TEntity> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }
            else
            {
                return await query.ToListAsync();
            }
        }

        public virtual TEntity FindById(object id)
        {
            return dbSet.Find(id);
        }

        public virtual async Task<TEntity> FindByIdAsync(object id)
        {
            return await dbSet.FindAsync(id);
        }

        public virtual async Task InsertAsync(TEntity entity)
        {
            await dbSet.AddAsync(entity);
            await context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(object id)
        {
            TEntity entityToDelete = dbSet.Find(id);
            await DeleteAsync(entityToDelete);
            await context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(TEntity entityToDelete)
        {
            if (context.Entry(entityToDelete).State == EntityState.Detached)
            {
                dbSet.Attach(entityToDelete);
            }
            dbSet.Remove(entityToDelete);
            await context.SaveChangesAsync();
        }

        public virtual async Task UpdateAsync(TEntity entityToUpdate)
        {
            dbSet.Attach(entityToUpdate);
            context.Entry(entityToUpdate).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

    }
}
