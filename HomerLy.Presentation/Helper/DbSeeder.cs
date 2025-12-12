using Homerly.Business.Utils;
using Homerly.BusinessObject.Enums;
using Homerly.DataAccess.Entities;
using HomerLy.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Homerly.Presentation.Helper
{
    public static class DbSeeder
    {
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

            Console.WriteLine("🎉 Database seeding completed!");
            Console.WriteLine("📋 Test Accounts:");
            Console.WriteLine("   Admin: admin@homerly.com / Admin@123");
            Console.WriteLine("   Owner: owner1@homerly.com / Owner@123");
            Console.WriteLine("   User:  user1@homerly.com / User@123");
        }
    }
}
