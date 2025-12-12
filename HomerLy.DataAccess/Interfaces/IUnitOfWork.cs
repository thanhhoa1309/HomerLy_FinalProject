using Homerly.DataAccess.Entities;

namespace HomerLy.DataAccess.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Account> Account { get; }
        IGenericRepository<Property> Property { get; }
        IGenericRepository<Tenancy> Tenancy { get; }
        IGenericRepository<UtilityReading> UtilityReading { get; }
        Task<int> SaveChangesAsync();
    }
}
