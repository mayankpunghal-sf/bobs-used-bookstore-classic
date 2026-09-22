using System.ComponentModel.DataAnnotations.Schema;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Bookstore.Domain;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using BobsBookstoreClassic.Data;

namespace Bookstore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(string connectionString) : base(connectionString) { }

        public ApplicationDbContext(DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection, contextOwnsConnection) { }

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

            if (BookstoreConfiguration.GetDatabaseProvider() == DatabaseProvider.PostgreSql)
            {
                ConfigurePostgreSqlModel(modelBuilder);
            }
            else
            {
                ConfigureSqlServerModel(modelBuilder);
            }

            Database.SetInitializer(new BookstoreDbInitializer());
        }

        private static void ConfigureSqlServerModel(DbModelBuilder modelBuilder)
        {
            // Pre-port SQL Server model — byte-identical behavior. The additive
            // Entity.xmin CLR property (PostgreSQL-only concurrency token) is not
            // part of the SQL Server schema and is ignored here.
            ConfigureXminIgnored(modelBuilder.Entity<Address>());
            ConfigureXminIgnored(modelBuilder.Entity<Book>());
            ConfigureXminIgnored(modelBuilder.Entity<Customer>());
            ConfigureXminIgnored(modelBuilder.Entity<Offer>());
            ConfigureXminIgnored(modelBuilder.Entity<Order>());
            ConfigureXminIgnored(modelBuilder.Entity<OrderItem>());
            ConfigureXminIgnored(modelBuilder.Entity<ReferenceDataItem>());
            ConfigureXminIgnored(modelBuilder.Entity<ShoppingCart>());
            ConfigureXminIgnored(modelBuilder.Entity<ShoppingCartItem>());

            modelBuilder.Entity<Customer>().Property(x => x.Sub).HasColumnType("nvarchar").HasMaxLength(450);
        }

        private static void ConfigurePostgreSqlModel(DbModelBuilder modelBuilder)
        {
            // PostgreSQL-only model fork (R8). The SQL Server branch above keeps
            // the pre-port model byte-identical.
            ConfigureXminConcurrency(modelBuilder.Entity<Address>());
            ConfigureXminConcurrency(modelBuilder.Entity<Book>());
            ConfigureXminConcurrency(modelBuilder.Entity<Customer>());
            ConfigureXminConcurrency(modelBuilder.Entity<Offer>());
            ConfigureXminConcurrency(modelBuilder.Entity<Order>());
            ConfigureXminConcurrency(modelBuilder.Entity<OrderItem>());
            ConfigureXminConcurrency(modelBuilder.Entity<ReferenceDataItem>());
            ConfigureXminConcurrency(modelBuilder.Entity<ShoppingCart>());
            ConfigureXminConcurrency(modelBuilder.Entity<ShoppingCartItem>());

            modelBuilder.Entity<Customer>().Property(x => x.Sub).HasColumnType("varchar").HasMaxLength(450);
            modelBuilder.Entity<Order>().Property(x => x.DeliveryDate).HasColumnType("timestamptz");
        }

        private static void ConfigureXminIgnored<TEntity>(EntityTypeConfiguration<TEntity> entity) where TEntity : Entity
        {
            entity.Ignore(x => x.xmin);
        }

        private static void ConfigureXminConcurrency<TEntity>(EntityTypeConfiguration<TEntity> entity) where TEntity : Entity
        {
            // rowversion has no PostgreSQL equivalent — optimistic concurrency is
            // backed by the xmin system column instead (R9 option b). EF6 has no
            // IsRowVersion() on PrimitivePropertyConfiguration and no shadow
            // properties, hence the additive uint Entity.xmin CLR property.
            entity.Ignore(x => x.RowVersion);
            entity.Property(x => x.CreatedOn).HasColumnType("timestamptz");
            entity.Property(x => x.UpdatedOn).HasColumnType("timestamptz");
            entity.Property(x => x.xmin).IsConcurrencyToken().HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
        }
    }
}