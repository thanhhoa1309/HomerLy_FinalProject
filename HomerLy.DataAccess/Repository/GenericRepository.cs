using HomerLy.DataAccess;
using HomerLy.DataAccess.Entities;
using HomerLy.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace HomerLy.DataAccess.Repository
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
    {
        private readonly IClaimsService _claimsService;
        private readonly HomerLyDbContext _dbContext;
        private readonly DbSet<TEntity> _dbSet;
        private readonly ICurrentTime _timeService;

        public GenericRepository(HomerLyDbContext context, ICurrentTime timeService, IClaimsService claimsService)
        {
            _dbSet = context.Set<TEntity>();
            _dbContext = context;
            _timeService = timeService;
            _claimsService = claimsService;
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            // Convert all DateTime properties to UTC
            EnsureUtcDates(entity);

            entity.CreatedAt = _timeService.GetCurrentTime(); // Already returns UtcNow
            entity.UpdatedAt = _timeService.GetCurrentTime();
            entity.CreatedBy = _claimsService.GetCurrentUserId;

            var result = await _dbSet.AddAsync(entity);
            return result.Entity;
        }

        public async Task AddRangeAsync(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                EnsureUtcDates(entity);
                entity.CreatedAt = _timeService.GetCurrentTime();
                entity.UpdatedAt = _timeService.GetCurrentTime();
                entity.CreatedBy = _claimsService.GetCurrentUserId;
            }

            await _dbSet.AddRangeAsync(entities);
        }

        public async Task<bool> SoftRemove(TEntity entity)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = _timeService.GetCurrentTime();
            entity.DeletedBy = _claimsService.GetCurrentUserId;
            entity.UpdatedAt = _timeService.GetCurrentTime();

            _dbSet.Update(entity);
            return true;
        }

        public async Task<bool> SoftRemoveRange(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = _timeService.GetCurrentTime();
                entity.DeletedBy = _claimsService.GetCurrentUserId;
                entity.UpdatedAt = _timeService.GetCurrentTime();
            }

            _dbSet.UpdateRange(entities);
            return true;
        }

        public async Task<bool> SoftRemoveRangeById(List<Guid> entitiesId)
        {
            var entities = await _dbSet.Where(e => entitiesId.Contains(e.Id)).ToListAsync();

            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = _timeService.GetCurrentTime();
                entity.DeletedBy = _claimsService.GetCurrentUserId;
                entity.UpdatedAt = _timeService.GetCurrentTime();
            }

            _dbContext.UpdateRange(entities);
            return true;
        }

        public async Task<bool> Update(TEntity entity)
        {
            // Convert all DateTime properties to UTC
            EnsureUtcDates(entity);

            entity.UpdatedAt = _timeService.GetCurrentTime();
            entity.UpdatedBy = _claimsService.GetCurrentUserId;

            _dbSet.Update(entity);
            return true;
        }

        public async Task<bool> UpdateRange(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                EnsureUtcDates(entity);
                entity.UpdatedAt = _timeService.GetCurrentTime();
                entity.UpdatedBy = _claimsService.GetCurrentUserId;
            }

            _dbSet.UpdateRange(entities);
            return true;
        }

        public Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            if (predicate != null) query = query.Where(predicate);
            foreach (var include in includes) query = query.Include(include);

            return query.ToListAsync();
        }

        public async Task<TEntity?> GetByIdAsync(Guid id, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;
            foreach (var include in includes) query = query.Include(include);
            var result = await query.FirstOrDefaultAsync(x => x.Id == id);
            return result;
        }

        public IQueryable<TEntity> GetQueryable()
        {
            return _dbSet;
        }

        public async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> query = _dbSet;

            foreach (var include in includes) query = query.Include(include);

            if (predicate != null) query = query.Where(predicate);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> HardRemove(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                var entities = await _dbSet.Where(predicate).ToListAsync();
                if (entities.Any())
                {
                    _dbSet.RemoveRange(entities);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while performing hard remove: {ex.Message}");
            }
        }

        public async Task<bool> HardRemoveRange(List<TEntity> entities)
        {
            try
            {
                if (entities.Any())
                {
                    _dbSet.RemoveRange(entities);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while performing hard remove range: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures all DateTime properties on the entity are in UTC
        /// </summary>
        private void EnsureUtcDates(TEntity entity)
        {
            var dateTimeProperties = entity.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
                         && p.CanRead && p.CanWrite);

            foreach (var property in dateTimeProperties)
            {
                var value = property.GetValue(entity);

                if (value is DateTime dateTime)
                {
                    // Convert to UTC if not already
                    if (dateTime.Kind == DateTimeKind.Unspecified)
                    {
                        property.SetValue(entity, DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
                    }
                    else if (dateTime.Kind == DateTimeKind.Local)
                    {
                        property.SetValue(entity, dateTime.ToUniversalTime());
                    }
                    // If already UTC, leave it as is
                }
            }
        }
    }
}