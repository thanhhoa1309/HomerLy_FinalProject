using Homerly.DataAccess.Entities;
using HomerLy.DataAccess.Commons;
using Microsoft.EntityFrameworkCore;

namespace HomerLy.DataAccess
{
    public class HomerLyDbContext : DbContext
    {
        public HomerLyDbContext() { }

        public HomerLyDbContext(DbContextOptions<HomerLyDbContext> options)
            : base(options) { }

        // -------------------- DbSets --------------------
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Tenancy> Tenancies { get; set; }
        public DbSet<UtilityReading> UtilityReadings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PropertyReport> PropertyReports { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enum string conversion
            modelBuilder.UseStringForEnums();

            // -------------------- RELATIONSHIPS --------------------

            // Account -> Property (One-to-Many)
            modelBuilder.Entity<Property>()
                .HasOne<Account>()
                .WithMany(a => a.Properties)
                .HasForeignKey(p => p.OwnerId);


            // Tenancy -> Property (Many-to-One)
            modelBuilder.Entity<Tenancy>()
                .HasOne(t => t.Property)
                .WithMany()
                .HasForeignKey(t => t.PropertyId);


            // Tenancy -> Tenant (Many-to-One)
            modelBuilder.Entity<Tenancy>()
                .HasOne(t => t.Tenant)
                .WithMany()
                .HasForeignKey(t => t.TenantId);


            // Tenancy -> Owner (Many-to-One)
            modelBuilder.Entity<Tenancy>()
                .HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerId);


            // UtilityReading -> Property (Many-to-One)
            modelBuilder.Entity<UtilityReading>()
                .HasOne<Property>()
                .WithMany()
                .HasForeignKey(ur => ur.PropertyId);


            // UtilityReading -> Tenancy (Many-to-One)
            modelBuilder.Entity<UtilityReading>()
                .HasOne<Tenancy>()
                .WithMany()
                .HasForeignKey(ur => ur.TenancyId);


            // Payment -> Property (Many-to-One)
            modelBuilder.Entity<Payment>()
                .HasOne<Property>()
                .WithMany()
                .HasForeignKey(p => p.PropertyId);


            // Payment -> Tenancy (Many-to-One)
            modelBuilder.Entity<Payment>()
                .HasOne<Tenancy>()
                .WithMany()
                .HasForeignKey(p => p.TenancyId);


            // Payment -> Payer (Many-to-One)
            modelBuilder.Entity<Payment>()
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(p => p.PayerId);


            // PropertyReport -> Property (Many-to-One)
            modelBuilder.Entity<PropertyReport>()
                .HasOne<Property>()
                .WithMany()
                .HasForeignKey(pr => pr.PropertyId);


            // PropertyReport -> Tenancy (Many-to-One)
            modelBuilder.Entity<PropertyReport>()
                .HasOne<Tenancy>()
                .WithMany()
                .HasForeignKey(pr => pr.TenancyId);


            // PropertyReport -> RequestedBy (Many-to-One)
            modelBuilder.Entity<PropertyReport>()
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(pr => pr.RequestedById);


            // ChatMessage -> Tenancy (Many-to-One)
            modelBuilder.Entity<ChatMessage>()
                .HasOne<Tenancy>()
                .WithMany()
                .HasForeignKey(cm => cm.TenancyId);


            // ChatMessage -> Sender (Many-to-One)
            modelBuilder.Entity<ChatMessage>()
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(cm => cm.SenderId);

        }
    }
}
