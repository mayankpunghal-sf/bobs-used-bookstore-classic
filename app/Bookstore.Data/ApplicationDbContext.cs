using System.ComponentModel.DataAnnotations.Schema;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Bookstore.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly DatabaseProvider databaseProvider;

        public ApplicationDbContext(string connectionString) : base(connectionString)
        {
            databaseProvider = DatabaseProvider.SqlServer;
        }

        // EF6 resolves the database provider from the connection itself, so the runtime
        // engine switch passes a provider-created DbConnection (SqlConnection or
        // NpgsqlConnection) instead of a bare connection string.
        public ApplicationDbContext(DbConnection existingConnection, DatabaseProvider databaseProvider)
            : base(existingConnection, contextOwnsConnection: true)
        {
            this.databaseProvider = databaseProvider;
        }

        public DbSet<Address> Address { get; set; }

        public DbSet<Book> Book { get; set; }

        public DbSet<Customer> Customer { get; set; }

        public DbSet<Order> Order { get; set; }

        public DbSet<ShoppingCart> ShoppingCart { get; set; }

        public DbSet<OrderItem> OrderItem { get; set; }

        public DbSet<Offer> Offer { get; set; }

        public DbSet<ReferenceDataItem> ReferenceData { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Update to remove the pluralization to match the modern version
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            if (databaseProvider == DatabaseProvider.SqlServer)
            {
                // SQL Server keeps its original provider-specific mapping unchanged.
                modelBuilder.Entity<Customer>().Property(x => x.Sub).HasColumnType("nvarchar").HasMaxLength(450);
            }
            else
            {
                // PostgreSQL has no "nvarchar" type; the default string mapping keeps the same length cap.
                modelBuilder.Entity<Customer>().Property(x => x.Sub).HasMaxLength(450);

                // rowversion has no PostgreSQL equivalent and EF6 cannot map an xmin concurrency
                // token, so the column is left unmapped on this engine to keep the logical schema
                // frozen. The lost concurrency check is flagged in DUAL_DB_PORT_REPORT.md (§8).
                modelBuilder.Entity<Address>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<Book>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<Customer>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<Offer>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<Order>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<OrderItem>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<ReferenceDataItem>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<ShoppingCart>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<ShoppingCartItem>().Ignore(x => x.RowVersion);
            }

            modelBuilder.Entity<Book>().HasRequired(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Book>().HasRequired(x => x.BookType).WithMany().HasForeignKey(x => x.BookTypeId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Book>().HasRequired(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Book>().HasRequired(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Offer>().HasRequired(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Offer>().HasRequired(x => x.BookType).WithMany().HasForeignKey(x => x.BookTypeId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Offer>().HasRequired(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Offer>().HasRequired(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Order>().HasRequired(x => x.Customer).WithMany().WillCascadeOnDelete(false);

            // Pin decimal precision: SQL Server's implicit default was already (18,2), so its
            // DDL is unchanged; PostgreSQL is pinned to numeric(18,2) instead of an unconstrained
            // or provider-default numeric.
            modelBuilder.Entity<Book>().Property(x => x.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Offer>().Property(x => x.BookPrice).HasPrecision(18, 2);

            // Update the Refernce Data Table to Match the modern version
            modelBuilder.Entity<ReferenceDataItem>().ToTable("ReferenceData");

            modelBuilder.Entity<ShoppingCartItem>().HasKey(x => new { x.Id, x.ShoppingCartId });
            modelBuilder.Entity<ShoppingCartItem>().Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            Database.SetInitializer(new BookstoreDbInitializer());
        }
    }
}
