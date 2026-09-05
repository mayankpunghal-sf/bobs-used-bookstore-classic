using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Bookstore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(string connectionString) : base(connectionString) { }

        // Connection is supplied by IDbConnectionFactory, chosen by the startup-resolved
        // DatabaseProvider (never sniffed from the connection string).
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

            // Single model, forked per provider on the startup-resolved value (R7).
            // The SQL Server branch stays byte-identical to the pre-port model so the
            // EF model hash does not change and DropCreateDatabaseIfModelChanges does
            // not drop existing SQL Server databases (R1).
            var isPostgreSql = DatabaseProviderAccessor.Instance.Provider == DatabaseProvider.PostgreSql;

            if (isPostgreSql)
            {
                // 'nvarchar' is not a valid PostgreSQL store type; MaxLength alone maps to varchar(450).
                modelBuilder.Entity<Customer>().Property(x => x.Sub).HasMaxLength(450);

                // rowversion ([Timestamp]) has no PostgreSQL equivalent. Optimistic-concurrency
                // checking is therefore disabled on PostgreSQL pending a decision on one of the
                // documented options (xmin mapping / shared int version column / timestamptz).
                // See DUAL_DB_PORT_REPORT.md flag section.
                modelBuilder.Entity<Address>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<Book>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<Customer>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<Order>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<OrderItem>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<Offer>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<ReferenceDataItem>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<ShoppingCart>().Ignore(x => x.RowVersion);
                modelBuilder.Entity<ShoppingCartItem>().Ignore(x => x.RowVersion);
            }
            else
            {
                modelBuilder.Entity<Customer>().Property(x => x.Sub).HasColumnType("nvarchar").HasMaxLength(450);
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