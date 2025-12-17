using Homerly.Business.Utils;
using Homerly.BusinessObject.Enums;
using Homerly.DataAccess.Entities;
using HomerLy.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Homerly.Presentation.Helper
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(HomerLyDbContext context)
        {
            await SeedAccountsAsync(context);
            await SeedPropertiesAsync(context);
            await SeedTenanciesAsync(context);
            await SeedUtilityReadingsAsync(context);
        }

        public static async Task SeedAccountsAsync(HomerLyDbContext context)
        {
            await context.Database.MigrateAsync();

            var passwordHasher = new PasswordHasher();

            // Seed Admin Account
            if (!await context.Accounts.AnyAsync(a => a.Role == RoleType.Admin && !a.IsDeleted))
            {
                var admin = new Account
                {
                    FullName = "Administrator",
                    Email = "admin@homerly.com",
                    PhoneNumber = "0901234567",
                    CccdNumber = "001234567890",
                    PasswordHash = passwordHasher.HashPassword("Admin@123"),
                    Role = RoleType.Admin,
                    IsOwnerApproved = false
                };

                await context.Accounts.AddAsync(admin);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Admin account seeded: admin@homerly.com / Admin@123");
            }

            // Seed Owner Accounts
            if (!await context.Accounts.AnyAsync(a => a.Role == RoleType.Owner && !a.IsDeleted))
            {
                var owners = new List<Account>
                {
                    new Account
                    {
                        FullName = "Nguyen Van A",
                        Email = "owner1@homerly.com",
                        PhoneNumber = "0912345678",
                        CccdNumber = "012345678901",
                        PasswordHash = passwordHasher.HashPassword("Owner@123"),
                        Role = RoleType.Owner,
                        IsOwnerApproved = true // Approved owner
                    },
                    new Account
                    {
                        FullName = "Tran Thi B",
                        Email = "owner2@homerly.com",
                        PhoneNumber = "0923456789",
                        CccdNumber = "012345678902",
                        PasswordHash = passwordHasher.HashPassword("Owner@123"),
                        Role = RoleType.Owner,
                        IsOwnerApproved = true // Approved owner
                    },
                    new Account
                    {
                        FullName = "Le Van C",
                        Email = "owner3@homerly.com",
                        PhoneNumber = "0934567890",
                        CccdNumber = "012345678903",
                        PasswordHash = passwordHasher.HashPassword("Owner@123"),
                        Role = RoleType.Owner,
                        IsOwnerApproved = false // Pending approval
                    }
                };

                await context.Accounts.AddRangeAsync(owners);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Owner accounts seeded (3 owners)");
            }

            // Seed User Accounts
            if (!await context.Accounts.AnyAsync(a => a.Role == RoleType.User && !a.IsDeleted))
            {
                var users = new List<Account>
                {
                    new Account
                    {
                        FullName = "Pham Minh D",
                        Email = "user1@homerly.com",
                        PhoneNumber = "0945678901",
                        CccdNumber = "012345678904",
                        PasswordHash = passwordHasher.HashPassword("User@123"),
                        Role = RoleType.User,
                        IsOwnerApproved = false
                    },
                    new Account
                    {
                        FullName = "Hoang Thi E",
                        Email = "user2@homerly.com",
                        PhoneNumber = "0956789012",
                        CccdNumber = "012345678905",
                        PasswordHash = passwordHasher.HashPassword("User@123"),
                        Role = RoleType.User,
                        IsOwnerApproved = false
                    },
                    new Account
                    {
                        FullName = "Vu Van F",
                        Email = "user3@homerly.com",
                        PhoneNumber = "0967890123",
                        CccdNumber = "012345678906",
                        PasswordHash = passwordHasher.HashPassword("User@123"),
                        Role = RoleType.User,
                        IsOwnerApproved = false
                    },
                    new Account
                    {
                        FullName = "Dang Thi G",
                        Email = "user4@homerly.com",
                        PhoneNumber = "0978901234",
                        CccdNumber = "012345678907",
                        PasswordHash = passwordHasher.HashPassword("User@123"),
                        Role = RoleType.User,
                        IsOwnerApproved = false
                    },
                    new Account
                    {
                        FullName = "Bui Van H",
                        Email = "user5@homerly.com",
                        PhoneNumber = "0989012345",
                        CccdNumber = "012345678908",
                        PasswordHash = passwordHasher.HashPassword("User@123"),
                        Role = RoleType.User,
                        IsOwnerApproved = false
                    }
                };

                await context.Accounts.AddRangeAsync(users);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ User accounts seeded (5 users)");
            }

            Console.WriteLine("🎉 Account seeding completed!");
            Console.WriteLine("📋 Test Accounts:");
            Console.WriteLine("   Admin: admin@homerly.com / Admin@123");
            Console.WriteLine("   Owner: owner1@homerly.com / Owner@123");
            Console.WriteLine("   User:  user1@homerly.com / User@123");
        }

        public static async Task SeedPropertiesAsync(HomerLyDbContext context)
        {
            if (await context.Properties.AnyAsync(p => !p.IsDeleted))
            {
                Console.WriteLine("⏭️ Properties already seeded, skipping...");
                return;
            }

            // Get approved owners
            var owners = await context.Accounts
                .Where(a => a.Role == RoleType.Owner && a.IsOwnerApproved && !a.IsDeleted)
                .ToListAsync();

            if (!owners.Any())
            {
                Console.WriteLine("❌ No approved owners found. Cannot seed properties.");
                return;
            }

            var properties = new List<Property>
            {
                // Owner 1's Properties
                new Property
                {
                    OwnerId = owners[0].Id,
                    Title = "Modern Apartment in District 1",
                    Description = "Beautiful 2-bedroom apartment with city view, fully furnished, near metro station.",
                    Address = "123 Nguyen Hue Street, District 1, Ho Chi Minh City",
                    MonthlyPrice = 15000000m,
                    AreaSqm = 75,
                    Status = PropertyStatus.available,
                    ImageUrl = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2070&q=80"
                },
                new Property
                {
                    OwnerId = owners[0].Id,
                    Title = "Cozy Studio in District 3",
                    Description = "Compact studio apartment perfect for students or young professionals. Includes basic furniture.",
                    Address = "456 Vo Van Tan Street, District 3, Ho Chi Minh City",
                    MonthlyPrice = 8000000m,
                    AreaSqm = 35,
                    Status = PropertyStatus.available,
                    ImageUrl = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2070&q=80"
                },
                new Property
                {
                    OwnerId = owners[0].Id,
                    Title = "Family House in Binh Thanh",
                    Description = "3-bedroom house with garden, parking space, quiet neighborhood, suitable for families.",
                    Address = "789 Xo Viet Nghe Tinh Street, Binh Thanh District, Ho Chi Minh City",
                    MonthlyPrice = 25000000m,
                    AreaSqm = 120,
                    Status = PropertyStatus.occupied,
                    ImageUrl = "https://images.unsplash.com/photo-1570129477492-45c003edd2be?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2070&q=80"
                },

                // Owner 2's Properties  
                new Property
                {
                    OwnerId = owners[1].Id,
                    Title = "Luxury Condo in District 7",
                    Description = "High-end condominium with swimming pool, gym, security 24/7. River view from balcony.",
                    Address = "321 Nguyen Van Linh Boulevard, District 7, Ho Chi Minh City",
                    MonthlyPrice = 30000000m,
                    AreaSqm = 95,
                    Status = PropertyStatus.available,
                    ImageUrl = "https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2070&q=80"
                },
                new Property
                {
                    OwnerId = owners[1].Id,
                    Title = "Budget Room in District 10",
                    Description = "Affordable single room with shared kitchen and bathroom. Great for budget-conscious tenants.",
                    Address = "654 Su Van Hanh Street, District 10, Ho Chi Minh City",
                    MonthlyPrice = 4500000m,
                    AreaSqm = 20,
                    Status = PropertyStatus.occupied,
                    ImageUrl = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2058&q=80"
                },
                new Property
                {
                    OwnerId = owners[1].Id,
                    Title = "Penthouse in District 2",
                    Description = "Stunning penthouse with panoramic city view, rooftop terrace, premium finishes throughout.",
                    Address = "987 Mai Chi Tho Street, District 2, Ho Chi Minh City",
                    MonthlyPrice = 50000000m,
                    AreaSqm = 150,
                    Status = PropertyStatus.available,
                    ImageUrl = "https://images.unsplash.com/photo-1613490493576-7fde63acd811?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2071&q=80"
                }
            };

            await context.Properties.AddRangeAsync(properties);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Properties seeded ({properties.Count} properties)");
        }

        public static async Task SeedTenanciesAsync(HomerLyDbContext context)
        {
            if (await context.Tenancies.AnyAsync(t => !t.IsDeleted))
            {
                Console.WriteLine("⏭️ Tenancies already seeded, skipping...");
                return;
            }

            // Get occupied properties
            var occupiedProperties = await context.Properties
                .Where(p => p.Status == PropertyStatus.occupied && !p.IsDeleted)
                .ToListAsync();

            // Get users (potential tenants)
            var users = await context.Accounts
                .Where(a => a.Role == RoleType.User && !a.IsDeleted)
                .ToListAsync();

            if (!occupiedProperties.Any() || !users.Any())
            {
                Console.WriteLine("❌ No occupied properties or users found. Cannot seed tenancies.");
                return;
            }

            var tenancies = new List<Tenancy>();
            var random = new Random();

            for (int i = 0; i < Math.Min(occupiedProperties.Count, users.Count); i++)
            {
                var property = occupiedProperties[i];
                var tenant = users[i];
                var startDate = DateTime.Now.AddMonths(-random.Next(1, 12)); // Started 1-12 months ago
                var endDate = startDate.AddMonths(12); // 12-month contract

                var tenancy = new Tenancy
                {
                    PropertyId = property.Id,
                    TenantId = tenant.Id,
                    OwnerId = property.OwnerId,
                    StartDate = startDate,
                    EndDate = endDate,
                    ContractUrl = $"https://contracts.homerly.com/contract_{property.Id}_{tenant.Id}.pdf",
                    Status = DateTime.Now < endDate ? TenancyStatus.active : TenancyStatus.expired,
                    IsTenantConfirmed = true,
                    ElectricUnitPrice = 3500m, // 3,500 VND per kWh
                    WaterUnitPrice = 25000m,   // 25,000 VND per m³
                    ElectricOldIndex = random.Next(100, 500),
                    WaterOldIndex = random.Next(50, 200)
                };

                tenancies.Add(tenancy);
            }

            await context.Tenancies.AddRangeAsync(tenancies);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Tenancies seeded ({tenancies.Count} tenancies)");
        }

        public static async Task SeedUtilityReadingsAsync(HomerLyDbContext context)
        {
            if (await context.UtilityReadings.AnyAsync(ur => !ur.IsDeleted))
            {
                Console.WriteLine("⏭️ Utility readings already seeded, skipping...");
                return;
            }

            // Get active tenancies
            var activeTenancies = await context.Tenancies
                .Where(t => t.Status == TenancyStatus.active && !t.IsDeleted)
                .ToListAsync();

            if (!activeTenancies.Any())
            {
                Console.WriteLine("❌ No active tenancies found. Cannot seed utility readings.");
                return;
            }

            var utilityReadings = new List<UtilityReading>();
            var random = new Random();

            foreach (var tenancy in activeTenancies)
            {
                // Create 2-3 utility readings for each tenancy (monthly readings)
                var readingCount = random.Next(2, 4);
                
                for (int i = 0; i < readingCount; i++)
                {
                    var readingDate = tenancy.StartDate.AddMonths(i + 1);
                    
                    // Calculate usage based on previous reading or initial index
                    int electricOldIndex, waterOldIndex;
                    
                    if (i == 0)
                    {
                        // First reading uses tenancy's initial indexes
                        electricOldIndex = tenancy.ElectricOldIndex;
                        waterOldIndex = tenancy.WaterOldIndex;
                    }
                    else
                    {
                        // Subsequent readings use previous month's new index as old index
                        var previousReading = utilityReadings
                            .LastOrDefault(ur => ur.TenancyId == tenancy.Id);
                        electricOldIndex = previousReading?.ElectricNewIndex ?? tenancy.ElectricOldIndex;
                        waterOldIndex = previousReading?.WaterNewIndex ?? tenancy.WaterOldIndex;
                    }

                    var utilityReading = new UtilityReading
                    {
                        PropertyId = tenancy.PropertyId,
                        TenancyId = tenancy.Id,
                        ReadingDate = readingDate,
                        ElectricOldIndex = electricOldIndex,
                        ElectricNewIndex = electricOldIndex + random.Next(80, 150), // 80-150 kWh usage
                        WaterOldIndex = waterOldIndex,
                        WaterNewIndex = waterOldIndex + random.Next(8, 20), // 8-20 m³ usage
                        IsCharged = i < readingCount - 1, // All but the latest reading are charged
                        CreatedById = tenancy.OwnerId // Owner creates the readings
                    };

                    utilityReadings.Add(utilityReading);
                }
            }

            await context.UtilityReadings.AddRangeAsync(utilityReadings);
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Utility readings seeded ({utilityReadings.Count} readings)");
            Console.WriteLine("🎉 Database seeding completed!");
            Console.WriteLine("📋 Test Accounts:");
            Console.WriteLine("   Admin: admin@homerly.com / Admin@123");
            Console.WriteLine("   Owner: owner1@homerly.com / Owner@123");
            Console.WriteLine("   User:  user1@homerly.com / User@123");
        }
    }
}
