using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Amazon.Rekognition;
using Amazon.S3;
using Autofac;
using Autofac.Integration.Mvc;
using BobsBookstoreClassic.Data;
using Bookstore.Data;
using Bookstore.Data.FileServices;
using Bookstore.Data.ImageResizeService;
using Bookstore.Data.ImageValidationServices;
using Bookstore.Data.Repositories;
using Bookstore.Domain;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Bookstore.Web.Helpers;
using Owin;

namespace Bookstore.Web
{
    public static class DependencyInjectionSetup
    {
        public static void ConfigureDependencyInjection(IAppBuilder app)
        {
            var builder = new ContainerBuilder();

            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            builder.RegisterType<BookService>().As<IBookService>();
            builder.RegisterType<OrderService>().As<IOrderService>();
            builder.RegisterType<ReferenceDataService>().As<IReferenceDataService>();
            builder.RegisterType<OfferService>().As<IOfferService>();
            builder.RegisterType<CustomerService>().As<ICustomerService>();
            builder.RegisterType<AddressService>().As<IAddressService>();
            builder.RegisterType<ShoppingCartService>().As<IShoppingCartService>();
            builder.RegisterType<ImageResizeService>().As<IImageResizeService>();

            // Resolve the target database engine once at startup: one config switch selects
            // both the named connection string and the ADO.NET/EF6 provider. No default
            // engine, no connection-string sniffing (dual-db-port R7).
            var databaseProvider = new DatabaseProviderAccessor(DatabaseProviderAccessor.Parse(
                BookstoreConfiguration.GetSetting(DatabaseProviderAccessor.ConfigKey)));

            var connectionString = BookstoreConfiguration.GetConnectionString(databaseProvider.ConnectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{databaseProvider.ConnectionStringName}' for database provider " +
                    $"'{databaseProvider.Provider}' is missing or empty. Add a non-empty value to <connectionStrings>.");
            }

            var dbConnectionFactory = DbProviderFactories.GetFactory(databaseProvider.ProviderInvariant);

            builder.Register(c =>
            {
                DbConnection connection = dbConnectionFactory.CreateConnection();

                if (connection == null)
                {
                    throw new InvalidOperationException(
                        $"DbProviderFactory for invariant '{databaseProvider.ProviderInvariant}' did not create a connection.");
                }

                connection.ConnectionString = connectionString;

                return new ApplicationDbContext(connection, databaseProvider.Provider);
            }).InstancePerRequest();

            builder.RegisterType<CustomerRepository>().As<ICustomerRepository>();
            builder.RegisterType<AddressRepository>().As<IAddressRepository>();
            builder.RegisterType<BookRepository>().As<IBookRepository>();
            builder.RegisterType<OfferRepository>().As<IOfferRepository>();
            builder.RegisterType<ShoppingCartRepository>().As<IShoppingCartRepository>();
            builder.RegisterType<OrderRepository>().As<IOrderRepository>();
            builder.RegisterType<ReferenceDataRepository>().As<IReferenceDataRepository>();

            builder.RegisterGeneric(typeof(PaginatedList<>)).As(typeof(IPaginatedList<>)).InstancePerLifetimeScope();

            if (BookstoreConfiguration.GetSetting("Services/FileService") == "aws")
            {
                builder.RegisterType<AmazonS3Client>().As<IAmazonS3>();
                builder.RegisterType<S3FileService>().As<IFileService>();
            }
            else
            {
                var webRootPath = HttpRuntime.AppDomainAppVirtualPath != null ?
                    Path.Combine(HttpRuntime.AppDomainAppPath, "Content") :
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                builder.RegisterInstance(new LocalFileService(webRootPath)).As<IFileService>();
            }

            if (BookstoreConfiguration.GetSetting("Services/ImageValidationService") == "aws")
            {
                builder.RegisterType<AmazonRekognitionClient>().As<IAmazonRekognition>();
                builder.RegisterType<RekognitionImageValidationService>().As<IImageValidationService>();
            }
            else
            {
                builder.RegisterType<LocalImageValidationService>().As<IImageValidationService>();
            }

            if (BookstoreConfiguration.GetSetting("Services/Authentication") != "aws")
            {
                builder.RegisterType<LocalAuthenticationMiddleware>();
            }

            var container = builder.Build();

            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

            app.UseAutofacMiddleware(container);
        }
    }
}