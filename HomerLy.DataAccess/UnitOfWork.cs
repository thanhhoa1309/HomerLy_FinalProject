using Homerly.DataAccess.Entities;
using HomerLy.DataAccess;
using HomerLy.DataAccess.Interfaces;

namespace HomerLy.DataAccess
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HomerLyDbContext _dbContext;

        public UnitOfWork(HomerLyDbContext dbContext,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<Property> propertyRepository,
            IGenericRepository<Tenancy> tenancyRepository,
            IGenericRepository<UtilityReading> utilityReadingRepository,
            IGenericRepository<Payment> paymentRepository,
            IGenericRepository<PropertyReport> propertyReportRepository,
            IGenericRepository<ChatMessage> chatMessageRepository)
        {
            _dbContext = dbContext;
            Account = accountRepository;
            Property = propertyRepository;
            Tenancy = tenancyRepository;
            UtilityReading = utilityReadingRepository;
            Payment = paymentRepository;
            PropertyReport = propertyReportRepository;
            ChatMessage = chatMessageRepository;
        }

        public IGenericRepository<Account> Account { get; set; }
        public IGenericRepository<Property> Property { get; set; }
        public IGenericRepository<Tenancy> Tenancy { get; set; }
        public IGenericRepository<UtilityReading> UtilityReading { get; set; }
        public IGenericRepository<Payment> Payment { get; set; }
        public IGenericRepository<PropertyReport> PropertyReport { get; set; }
        public IGenericRepository<ChatMessage> ChatMessage { get; set; }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
