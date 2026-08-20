using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Address> Address { get; set; } = null!;
        public DbSet<Book> Book { get; set; } = null!;
        public DbSet<Customer> Customer { get; set; } = null!;
        public DbSet<Order> Order { get; set; } = null!;
        public DbSet<ShoppingCart> ShoppingCart { get; set; } = null!;
        public DbSet<OrderItem> OrderItem { get; set; } = null!;
        public DbSet<Offer> Offer { get; set; } = null!;
        public DbSet<ReferenceDataItem> ReferenceData { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Map entity type names to singular table names (matching EF6 behaviour
            // where PluralizingTableNameConvention was removed)
            modelBuilder.Entity<Address>().ToTable("Address");
            modelBuilder.Entity<Book>().ToTable("Book");
            modelBuilder.Entity<Customer>().ToTable("Customer");
            modelBuilder.Entity<Order>().ToTable("Order");
            modelBuilder.Entity<ShoppingCart>().ToTable("ShoppingCart");
            modelBuilder.Entity<OrderItem>().ToTable("OrderItem");
            modelBuilder.Entity<Offer>().ToTable("Offer");
            modelBuilder.Entity<ReferenceDataItem>().ToTable("ReferenceData");

            // Customer index
            modelBuilder.Entity<Customer>()
                .Property(x => x.Sub).HasColumnType("nvarchar").HasMaxLength(450);
            modelBuilder.Entity<Customer>()
                .HasIndex(x => x.Sub).IsUnique();

            // Book foreign-key constraints (no cascade delete)
            modelBuilder.Entity<Book>()
                .HasOne(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Book>()
                .HasOne(x => x.BookType).WithMany().HasForeignKey(x => x.BookTypeId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Book>()
                .HasOne(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Book>()
                .HasOne(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId)
                .OnDelete(DeleteBehavior.NoAction);

            // Offer foreign-key constraints (no cascade delete)
            modelBuilder.Entity<Offer>()
                .HasOne(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Offer>()
                .HasOne(x => x.BookType).WithMany().HasForeignKey(x => x.BookTypeId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Offer>()
                .HasOne(x => x.Genre).WithMany().HasForeignKey(x => x.GenreId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Offer>()
                .HasOne(x => x.Condition).WithMany().HasForeignKey(x => x.ConditionId)
                .OnDelete(DeleteBehavior.NoAction);

            // Order: no cascade on Customer
            modelBuilder.Entity<Order>()
                .HasOne(x => x.Customer).WithMany().OnDelete(DeleteBehavior.NoAction);

            // ShoppingCartItem composite key
            modelBuilder.Entity<ShoppingCartItem>()
                .HasKey(x => new { x.Id, x.ShoppingCartId });
            modelBuilder.Entity<ShoppingCartItem>()
                .Property(x => x.Id).ValueGeneratedOnAdd();
        }
    }
}
