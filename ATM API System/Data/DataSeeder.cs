using System;
using System.Linq;
using System.Threading.Tasks;
using ATM_API_System.Models;
using ATM_API_System.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATM_API_System.Data
{
    public static class DataSeeder
    {
        /// <summary>
        /// Idempotent seeder. Call once on startup.
        /// Usage: await DataSeeder.SeedAsync(app.Services);
        /// </summary>
        public static async Task SeedAsync(IServiceProvider services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var context = provider.GetRequiredService<AppDbContext>();

            // Apply pending migrations (optional/desired)
            try
            {
                await context.Database.MigrateAsync();
            }
            catch
            {
                // if migrations fail (e.g., not desired in dev), continue — but in most setups you want migrations applied
            }

            // ---------- Seed AtmInventory ----------
            if (!await context.AtmInventories.AnyAsync())
            {
                var atm = new AtmInventory
                {
                    AtmId = 1,
                    CashAvailable = 10000m,
                    UpdatedAt = DateTime.UtcNow
                };
                await context.AtmInventories.AddAsync(atm);
                await context.SaveChangesAsync();
            }

            // ---------- Seed Customers, Accounts, Cards ----------
            if (!await context.Customers.AnyAsync())
            {
                // create a sample customer with accounts and a card
                var customer = new Customer
                {
                    Name = "John Doe",
                    Status = "Active"
                };
                await context.Customers.AddAsync(customer);
                await context.SaveChangesAsync(); // to get customer.Id

                // accounts
                var checking = new Account
                {
                    CustomerId = customer.Id,
                    Type = "Checking",
                    Balance = 2000m
                };
                var savings = new Account
                {
                    CustomerId = customer.Id,
                    Type = "Savings",
                    Balance = 5000m
                };
                await context.Accounts.AddRangeAsync(checking, savings);
                await context.SaveChangesAsync();

                // card (pin hashed with PinHasher)
                var card = new Card
                {
                    CardNumber = "4000000000000001",
                    PinHash = PinHasher.HashPin("4826"),
                    Status = "Active",
                    PinRetryCount = 0,
                    DailyWithdrawalLimit = 1000m,
                    DailyWithdrawalUsed = 0m,
                    CustomerId = customer.Id
                };
                await context.Cards.AddAsync(card);
                await context.SaveChangesAsync();

                // associate card with checking account (CardAccount join)
                // if you have CardAccounts table, seed it; if not, ignore
                try
                {
                    if (context.Model.FindEntityType(typeof(CardAccount)) != null)
                    {
                        var ca = new CardAccount
                        {
                            CardId = card.Id,
                            AccountId = checking.Id
                        };
                        await context.AddAsync(ca);
                        await context.SaveChangesAsync();
                    }
                }
                catch
                {
                    // ignore if CardAccount type not present/registered
                }
            }

            // ---------- Seed Transactions / AuditLogs (if none) ----------
            if (!await context.Transactions.AnyAsync())
            {
                // find an account to use for sample transactions
                var account = await context.Accounts.AsNoTracking().FirstOrDefaultAsync();
                if (account != null)
                {
                    var now = DateTime.UtcNow;
                    var t1 = new Transaction
                    {
                        AccountId = account.Id,
                        Type = "Deposit",
                        Amount = 1000m,
                        BalanceAfter = account.Balance,
                        Reference = $"TRX-{now:yyyyMMddHHmmss}-seed-1",
                        CreatedAt = now.AddDays(-3)
                    };
                    var t2 = new Transaction
                    {
                        AccountId = account.Id,
                        Type = "Withdrawal",
                        Amount = 100m,
                        BalanceAfter = account.Balance - 100m,
                        Reference = $"TRX-{now:yyyyMMddHHmmss}-seed-2",
                        CreatedAt = now.AddDays(-2)
                    };
                    await context.Transactions.AddRangeAsync(t1, t2);

                    await context.AuditLogs.AddRangeAsync(
                        new AuditLog { ActorType = "System", ActorId = 0, Action = "Seeded transactions", CreatedAt = DateTime.UtcNow }
                    );

                    await context.SaveChangesAsync();
                }
            }

            // ---------- Seed Operator account (idempotent) ----------
            if (!await context.Operators.AnyAsync())
            {
                // Use PasswordHasher utility to get hash and salt
                var pwdResult = PasswordHasher.HashPassword("Operator!23"); // change this in real deployments
                var op = new Operator
                {
                    Username = "operator",
                    PasswordHash = pwdResult.Hash,
                    PasswordSalt = pwdResult.Salt,
                    Role = "Operator",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                await context.Operators.AddAsync(op);
                await context.SaveChangesAsync();
            }

        }
    }
}
