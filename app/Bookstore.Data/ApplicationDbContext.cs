using System.ComponentModel.DataAnnotations.Schema;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using BobsBookstoreClassic.Data;
using Bookstore.Domain;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using Npgsql;

namespace Bookstore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(string connectionString) : base(connectionString) { }

        // dual-db-port seam: provider is decided by the connection the composition
        // root supplies (SqlConnection or NpgsqlConnection), not by sniffing strings.
        public ApplicationDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection) { }

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

            // dual-db-port (R8): provider-conditional model fork. The SQL Server
            // branch keeps the pre-port model byte-identical; the PostgreSQL
            // branch carries the store-type forks decided in plan.md.
            bool isPostgreSql = Database.Connection is NpgsqlConnection
                || BookstoreConfiguration.GetDatabaseProvider() == DatabaseProvider.PostgreSql;

            if (isPostgreSql)
            {
                // nvarchar has no PostgreSQL equivalent — use varchar (§5 fork).
                modelBuilder.Entity<Customer>().Property(x => x.Sub).HasColumnType("varchar").HasMaxLength(450);

                // R9 option (b): xmin-backed optimistic concurrency replaces the
                // SQL Server rowversion token on the PostgreSQL branch only.
                modelBuilder.Entity<Entity>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<Entity>().Property(x => x.xmin)
                    .IsConcurrencyToken()
                    .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);

                // §5.9: UTC-everywhere timestamps map to timestamptz.
                modelBuilder.Types<Entity>().Configure(c =>
                {
                    c.Property(e => e.CreatedOn).HasColumnType("timestamp with time zone");
                    c.Property(e => e.UpdatedOn).HasColumnType("timestamp with time zone");
                });
                modelBuilder.Entity<Order>().Property(x => x.DeliveryDate).HasColumnType("timestamp with time zone");
            }
            else
            {
                modelBuilder.Entity<Customer>().Property(x => x.Sub).HasColumnType("nvarchar").HasMaxLength(450);

                // xmin token is PostgreSQL-only; keep the SQL Server model identical.
                modelBuilder.Entity<Entity>().Ignore(x => x.xmin);
            }

            modelBuilder.Entity<Customer>().HasIndex(x => x.Sub).IsUnique();

            modelBuilder.Entity<Book>().HasRequired(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Book>().HasRequired(x => x.BookType).WithMany().HasForeignKey(x => x.BookTypeId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Book>().HasRequired(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Book>().HasRequired(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Offer>().HasRequired(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Offer>().HasRequired(x => x.BookType).WithMany().HasForeignKey(x => x.BookTypeId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Offer>().HasRequired(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId).WillCascadeOnDelete(false);
            modelBuilder.Entity<Offer>().HasRequired(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId).WillCascadeOnDelete(false);

            modelBuilder.Entity<Order>().HasRequired(x => x.Customer).WithMany().WillCascadeOnDelete(false);

            // Update the Refernce Data Table to Match the modern version
            modelBuilder.Entity<ReferenceDataItem>().ToTable("ReferenceData");

            modelBuilder.Entity<ShoppingCartItem>().HasKey(x => new { x.Id, x.ShoppingCartId });
            modelBuilder.Entity<ShoppingCartItem>().Property(x => x.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            Database.SetInitializer(new BookstoreDbInitializer());
        }
    }
}