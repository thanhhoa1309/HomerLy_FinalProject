using Homerly.DataAccess.Entities;
using HomerLy.DataAccess.Entities;

namespace HomerLy.DataAccess.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<Account> Account { get; }
        IGenericRepository<Property> Property { get; }
        IGenericRepository<Tenancy> Tenancy { get; }
        IGenericRepository<UtilityReading> UtilityReading { get; }
        IGenericRepository<Invoice> Invoice { get; }
        IGenericRepository<PropertyReport> PropertyReport { get; }
        Task<int> SaveChangesAsync();
    }
}
