using ATM_API_System.Models;
using Microsoft.EntityFrameworkCore;

namespace ATM_API_System.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<CardAccount> CardAccounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AtmInventory> AtmInventories { get; set; }
        public DbSet<Operator> Operators { get; set; }
        public DbSet<AtmReconciliation> AtmReconciliations { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----------------------------
            // CUSTOMER -> ACCOUNTS (1 - m)
            // ----------------------------
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Accounts)
                .WithOne(a => a.Customer)
                .HasForeignKey(a => a.CustomerId)
                // keep cascade for convenience (deleting a customer removes their accounts)
                .OnDelete(DeleteBehavior.Cascade);

            // ----------------------------
            // CUSTOMER -> CARDS (1 - m)
            // ----------------------------
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Cards)
                .WithOne(card => card.Customer)
                .HasForeignKey(card => card.CustomerId)
                // keep cascade for convenience (deleting a customer removes their cards)
                .OnDelete(DeleteBehavior.Cascade);

            // ----------------------------
            // ACCOUNT configuration
            // ----------------------------
            modelBuilder.Entity<Account>(b =>
            {
                b.Property(a => a.Balance).HasPrecision(18, 2);

                // Account -> Transactions (1 - m)
                b.HasMany(a => a.Transactions)
                 .WithOne(t => t.Account)
                 .HasForeignKey(t => t.AccountId)
                 .OnDelete(DeleteBehavior.Cascade); // deleting account removes its transactions

                // many-to-many relation set up via CardAccount
                b.HasMany(a => a.CardAccounts)
                 .WithOne(ca => ca.Account)
                 .HasForeignKey(ca => ca.AccountId)
                 // IMPORTANT: do NOT cascade delete from Account -> CardAccounts to avoid multiple cascade paths
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ----------------------------
            // CARD configuration
            // ----------------------------
            modelBuilder.Entity<Card>(b =>
            {
                b.Property(c => c.DailyWithdrawalLimit).HasPrecision(18, 2);
                b.Property(c => c.DailyWithdrawalUsed).HasPrecision(18, 2);

                b.HasMany(c => c.CardAccounts)
                 .WithOne(ca => ca.Card)
                 .HasForeignKey(ca => ca.CardId)
                 // IMPORTANT: do NOT cascade delete from Card -> CardAccounts to avoid multiple cascade paths
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ----------------------------
            // TRANSACTION configuration
            // ----------------------------
            modelBuilder.Entity<Transaction>(b =>
            {
                b.Property(t => t.Amount).HasPrecision(18, 2);
                b.Property(t => t.BalanceAfter).HasPrecision(18, 2);
                b.Property(t => t.Reference).IsRequired();
                b.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                // Add index for fast mini-statement lookups
                b.HasIndex(t => new { t.AccountId, t.CreatedAt }).HasDatabaseName("IX_Transactions_AccountId_CreatedAt");
            });

            // ----------------------------
            // CARDACCOUNT (join table) for many-to-many
            // ----------------------------
            modelBuilder.Entity<CardAccount>(b =>
            {
                b.HasKey(ca => new { ca.CardId, ca.AccountId });

                b.HasOne(ca => ca.Card)
                 .WithMany(c => c.CardAccounts)
                 .HasForeignKey(ca => ca.CardId)
                 .OnDelete(DeleteBehavior.Restrict); // explicit: no cascade

                b.HasOne(ca => ca.Account)
                 .WithMany(a => a.CardAccounts)
                 .HasForeignKey(ca => ca.AccountId)
                 .OnDelete(DeleteBehavior.Restrict); // explicit: no cascade
            });

            // ----------------------------
            // ATM INVENTORY
            // ----------------------------
            modelBuilder.Entity<AtmInventory>(b =>
            {
                b.Property(ai => ai.CashAvailable).HasPrecision(18, 2);
                b.Property(ai => ai.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // ----------------------------
            // AUDIT LOG
            // ----------------------------
            modelBuilder.Entity<AuditLog>(b =>
            {
                b.Property(a => a.Action).IsRequired();
                b.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}
