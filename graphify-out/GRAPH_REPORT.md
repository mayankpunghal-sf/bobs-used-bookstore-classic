# Graph Report - 499021d5-d12b-4180-aba8-18445a2fc3dd  (2026-09-22)

## Corpus Check
- 477 files · ~440,381 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 2602 nodes · 3992 edges · 460 communities (140 shown, 21 thin omitted)
- Extraction: 88% EXTRACTED · 12% INFERRED · 0% AMBIGUOUS · INFERRED: 468 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `5c7c0ae3`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- jquery-3.7.1.min.js
- CoreStack
- Scripts/jquery/jquery.min.js
- lib/jquery/jquery.min.js
- jquery-3.7.1.slim.min.js
- lib/jquery/jquery.js
- Scripts/jquery/jquery.js
- Scripts/jquery/jquery.slim.min.js
- Book
- jquery-3.7.1.slim.js
- PaginatedViewModel
- jquery-3.7.1.js
- lib/jquery/jquery.slim.js
- Scripts/jquery/jquery.slim.js
- Bookstore.Domain.Books
- lib/jquery/jquery.slim.min.js
- CheckoutAddressViewModel
- BookService
- Bookstore.Web.Helpers
- Bookstore.Web
- OrderDetailsViewModel
- Bookstore.Domain
- popper.min.js
- ShoppingCart
- ApplicationDbContext
- PaginatedList
- InventoryCreateUpdateViewModel
- OfferRepository
- IOrderRepository
- ReferenceDataItem
- IShoppingCartService
- Address
- Bookstore.Cdk.csproj
- Offer
- UpdateAddressDto
- modernizr-2.8.3.js
- Customer
- Order
- .CreateOrderAsync
- Ee
- domManip
- find
- domManip
- .ConfigureDependencyInjection
- UpdateBookDto
- Bookstore.Domain.Offers
- ReferenceDataItemCreateUpdateViewModel
- PaginatedViewModel
- ResaleCreateViewModel
- CreateBookDto
- ICustomerService
- CreateOfferDto
- IOrderService
- SearchDetailsViewModel
- ShoppingCartIndexItemViewModel
- .GetAsync
- AddressService
- Bookstore.Domain.Orders
- DashboardIndexViewModel
- InventoryDetailsViewModel
- Animation
- AddressIndexItemViewModel
- Animation
- Animation
- IAddressRepository
- WishlistController
- IOfferService
- InventoryIndexListItemViewModel
- OrderDetailsViewModel
- ResaleIndexItemViewModel
- e
- BookstoreConfiguration
- .GetSetting
- SearchController
- CancelOrderDto
- IReferenceDataService
- OfferIndexItemViewModel
- matcherFromTokens
- domManip
- ft
- matcherFromTokens
- LocalAuthenticationMiddleware
- AddressCreateUpdateViewModel
- OrderIndexItemViewModel
- domManip
- domManip
- matcherFromTokens
- domManip
- matcherFromTokens
- OfferFilters
- .GetAllReferenceDataAsync
- OffersController
- lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js
- SearchIndexItemViewModel
- Scripts/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js
- OrderFilters
- Controller
- ReferenceDataFilters
- InventoryIndexViewModel
- WishlistIndexItemViewModel
- DbSecrets
- BookFilters
- Entity
- OrderItem
- OrderIndexListItemViewModel
- ImageTypesAttribute
- find
- UpdateReferenceDataItemDto
- OfferIndexViewModel
- accounting.min.js
- AddressController
- matcherFromGroupMatchers
- Contributing Guidelines
- Bob's Used Books Classic
- AdminAreaRegistration
- ShoppingCartController
- nodeName
- nodeName
- .ResizeImageAsync
- CreateOrUpdateCustomerDto
- IFileService
- OrderStatistics
- c
- AuthenticationController
- .Create
- .GetSelectListForEnum
- d
- LocalFileService
- BookStatistics
- DateTimeExtensions
- .GetDescription
- ErrorController
- OrderIndexViewModel
- underscore-min.js
- getWidthOrHeight
- SearchIndexViewModel
- getWidthOrHeight
- expectSync
- expectSync
- ControllerExtensions.cs
- HttpContextExtensions.cs
- IOwinRequestExtensions.cs
- expectSync
- expectSync
- resolve
- resolve
- Scripts/jquery.validate.min.js
- Inventory/CreateUpdate.cshtml
- Admin/Views/Orders/Details.cshtml
- createCache
- inspectPrefiltersOrTransports
- camelCase
- createCache
- createCache
- inspectPrefiltersOrTransports
- createCache
- createCache
- inspectPrefiltersOrTransports
- camelCase
- createCache
- CODE_OF_CONDUCT.md

## God Nodes (most connected - your core abstractions)
1. `Offer` - 32 edges
2. `Book` - 30 edges
3. `Order` - 29 edges
4. `Bookstore.Domain` - 27 edges
5. `Bookstore.Domain.Books` - 27 edges
6. `InventoryCreateUpdateViewModel` - 27 edges
7. `Bookstore.Domain.Orders` - 26 edges
8. `ReferenceDataItem` - 26 edges
9. `ApplicationDbContext` - 23 edges
10. `Address` - 22 edges

## Surprising Connections (you probably didn't know these)
- `ce()` --indirect_call--> `o()`  [INFERRED]
  app/Bookstore.Web/Content/lib/jquery/jquery.slim.min.js → app/Bookstore.Web/Content/assets/js/popper.min.js
- `ce()` --indirect_call--> `o()`  [INFERRED]
  app/Bookstore.Web/Scripts/jquery/jquery.slim.min.js → app/Bookstore.Web/Scripts/jquery-3.7.1.min.js
- `ft()` --indirect_call--> `e()`  [INFERRED]
  app/Bookstore.Web/Content/lib/jquery/jquery.min.js → app/Bookstore.Web/Content/assets/js/popper.min.js
- `t()` --indirect_call--> `e()`  [INFERRED]
  app/Bookstore.Web/Content/lib/jquery/jquery.min.js → app/Bookstore.Web/Content/assets/js/popper.min.js
- `l()` --indirect_call--> `o()`  [INFERRED]
  app/Bookstore.Web/Content/lib/jquery/jquery.min.js → app/Bookstore.Web/Content/assets/js/popper.min.js

## Import Cycles
- None detected.

## Communities (460 total, 21 thin omitted)

### Community 0 - "jquery-3.7.1.min.js"
Cohesion: 0.07
Nodes (42): Ae(), B(), Be(), c(), $e(), ee(), F(), fe() (+34 more)

### Community 1 - "CoreStack"
Cohesion: 0.06
Nodes (32): CoreStack, ImageBucket, WebAppUserPool, Bucket, UserPool, DatabaseStack, Database, DatabaseStackProps (+24 more)

### Community 2 - "Scripts/jquery/jquery.min.js"
Cohesion: 0.07
Nodes (33): A(), b(), be(), ce(), ct(), _e(), Ee(), ft() (+25 more)

### Community 3 - "lib/jquery/jquery.min.js"
Cohesion: 0.08
Nodes (23): A(), b(), ce(), Ee(), he(), je(), jt(), Ke() (+15 more)

### Community 4 - "jquery-3.7.1.slim.min.js"
Cohesion: 0.11
Nodes (30): o(), qe(), Ae(), at(), b(), Be(), c(), Ct() (+22 more)

### Community 5 - "lib/jquery/jquery.js"
Cohesion: 0.06
Nodes (13): computeStyleTests(), dataAttr(), finalPropName(), getData(), Identity(), NOTE: This can be skipped if there are no unmatched elements (i.e.,…, TODO: Now that all calls to _data and _removeData have been replaced, TODO: identify versions (+5 more)

### Community 6 - "Scripts/jquery/jquery.js"
Cohesion: 0.06
Nodes (13): computeStyleTests(), dataAttr(), finalPropName(), getData(), Identity(), NOTE: This can be skipped if there are no unmatched elements (i.e.,…, TODO: Now that all calls to _data and _removeData have been replaced, TODO: identify versions (+5 more)

### Community 7 - "Scripts/jquery/jquery.slim.min.js"
Cohesion: 0.09
Nodes (27): be(), C(), ce(), d(), Ee(), et(), ge(), I() (+19 more)

### Community 8 - "Book"
Cohesion: 0.09
Nodes (25): BookRepository, ApplicationDbContext, Task, Book, Author, BookType, BookTypeId, Condition (+17 more)

### Community 9 - "jquery-3.7.1.slim.js"
Cohesion: 0.06
Nodes (17): camelCase(), computeStyleTests(), dataAttr(), fcamelCase(), finalPropName(), getData(), getDefaultDisplay(), Identity() (+9 more)

### Community 10 - "PaginatedViewModel"
Cohesion: 0.06
Nodes (28): HomeController, ActionResult, Task, ErrorViewModel, RequestId, ShowRequestId, HomeIndexItemViewModel, BookId (+20 more)

### Community 11 - "jquery-3.7.1.js"
Cohesion: 0.06
Nodes (13): computeStyleTests(), dataAttr(), finalPropName(), getData(), Identity(), leverageNative(), NOTE: This can be skipped if there are no unmatched elements (i.e.,…, TODO: Now that all calls to _data and _removeData have been replaced (+5 more)

### Community 12 - "lib/jquery/jquery.slim.js"
Cohesion: 0.06
Nodes (12): computeStyleTests(), dataAttr(), finalPropName(), getData(), getDefaultDisplay(), NOTE: This can be skipped if there are no unmatched elements (i.e.,…, TODO: Now that all calls to _data and _removeData have been replaced, TODO: identify versions (+4 more)

### Community 13 - "Scripts/jquery/jquery.slim.js"
Cohesion: 0.06
Nodes (12): computeStyleTests(), dataAttr(), finalPropName(), getData(), getDefaultDisplay(), NOTE: This can be skipped if there are no unmatched elements (i.e.,…, TODO: Now that all calls to _data and _removeData have been replaced, TODO: identify versions (+4 more)

### Community 14 - "Bookstore.Domain.Books"
Cohesion: 0.08
Nodes (16): ReferenceDataType, BookType, Condition, Genre, Publisher, ReferenceDataIndexListItemViewModel, Id, ReferenceDataType (+8 more)

### Community 15 - "lib/jquery/jquery.slim.min.js"
Cohesion: 0.10
Nodes (22): z(), be(), C(), ce(), d(), et(), ge(), ke() (+14 more)

### Community 16 - "CheckoutAddressViewModel"
Cohesion: 0.07
Nodes (30): CheckoutFinishedItemViewModel, BookId, Bookname, Price, Quantity, Url, CheckoutFinishedViewModel, Items (+22 more)

### Community 17 - "BookService"
Cohesion: 0.17
Nodes (13): BookResult, ErrorMessage, IsSuccess, BookService, IBookService, Book, IEnumerable, IFileService (+5 more)

### Community 18 - "Bookstore.Web.Helpers"
Cohesion: 0.11
Nodes (10): ReferenceDataType, IntExtensions, Bookstore.Web.Areas.Admin.Models.Orders.OrderIndexViewModel, Bookstore.Web.Areas.Admin.Models.ReferenceData.ReferenceDataIndexViewModel, Bookstore.Web.Areas.Admin.Models.ReferenceData.ReferenceDataItemCreateUpdateViewModel, Bookstore.Web.Controllers, Bookstore.Domain.Carts, Bookstore.Web.Helpers (+2 more)

### Community 19 - "Bookstore.Web"
Cohesion: 0.09
Nodes (12): BundleConfig, ConfigurationSetup, FilterConfig, LoggingSetup, RouteConfig, MvcApplication, BundleCollection, BobsBookstoreClassic.Data (+4 more)

### Community 20 - "OrderDetailsViewModel"
Cohesion: 0.07
Nodes (27): OrderDetailsItemViewModel, Author, BookType, Condition, Genre, Name, Price, Publisher (+19 more)

### Community 21 - "Bookstore.Domain"
Cohesion: 0.11
Nodes (12): ImageResizeService, RekognitionImageValidationService, IAmazonRekognition, Stream, Task, IImageResizeService, Bookstore.Web.Areas.Admin.Models.Offers.OfferIndexViewModel, Bookstore.Domain (+4 more)

### Community 22 - "popper.min.js"
Cohesion: 0.23
Nodes (22): a(), b(), c(), d(), e(), f(), g(), h() (+14 more)

### Community 23 - "ShoppingCart"
Cohesion: 0.11
Nodes (16): ShoppingCart, CorrelationId, ShoppingCartItems, ShoppingCartItemFilter, ExcludeOutOfStockItems, IncludeOutOfStockItems, IEnumerable, List (+8 more)

### Community 24 - "ApplicationDbContext"
Cohesion: 0.11
Nodes (22): ApplicationDbContext, Address, Book, Customer, Offer, Order, OrderItem, ReferenceData (+14 more)

### Community 25 - "PaginatedList"
Cohesion: 0.10
Nodes (17): PaginatedList, HasNextPage, HasPreviousPage, PageIndex, TotalPages, IEnumerable, Task, IPaginatedList (+9 more)

### Community 26 - "InventoryCreateUpdateViewModel"
Cohesion: 0.09
Nodes (22): InventoryCreateUpdateViewModel, Author, BookConditions, BookTypes, CoverImage, CoverImageUrl, Genres, Id (+14 more)

### Community 27 - "OfferRepository"
Cohesion: 0.14
Nodes (12): OfferRepository, ApplicationDbContext, IEnumerable, Task, IOfferRepository, IEnumerable, IPaginatedList, Task (+4 more)

### Community 28 - "IOrderRepository"
Cohesion: 0.16
Nodes (10): OrderRepository, ApplicationDbContext, Book, IEnumerable, Task, IOrderRepository, Book, IEnumerable (+2 more)

### Community 29 - "ReferenceDataItem"
Cohesion: 0.16
Nodes (11): ReferenceDataRepository, ApplicationDbContext, IEnumerable, Task, IReferenceDataRepository, IEnumerable, IPaginatedList, Task (+3 more)

### Community 30 - "IShoppingCartService"
Cohesion: 0.16
Nodes (12): AddToWishlistDto, BookId, CorrelationId, DeleteShoppingCartItemDto, CorrelationId, ShoppingCartItemId, MoveWishlistItemToShoppingCartDto, CorrelationId (+4 more)

### Community 31 - "Address"
Cohesion: 0.13
Nodes (15): AddressRepository, ApplicationDbContext, IEnumerable, Task, Address, AddressLine1, AddressLine2, City (+7 more)

### Community 32 - "Bookstore.Cdk.csproj"
Cohesion: 0.13
Nodes (12): Microsoft.NET.Sdk, Microsoft.NET.Sdk, net6.0, netstandard2.0, Amazon.CDK.Lib (2.188.0), Amazon.Jsii.Analyzers (*), AWSSDK.Rekognition, AWSSDK.S3 (+4 more)

### Community 33 - "Offer"
Cohesion: 0.11
Nodes (19): Offer, Author, BookName, BookPrice, BookType, BookTypeId, Comment, Condition (+11 more)

### Community 34 - "UpdateAddressDto"
Cohesion: 0.11
Nodes (17): CreateAddressDto, AddressLine1, AddressLine2, City, Country, CustomerSub, State, ZipCode (+9 more)

### Community 35 - "modernizr-2.8.3.js"
Cohesion: 0.24
Nodes (16): addStyleSheet(), contains(), createDocumentFragment(), createElement(), getElements(), getExpandoData(), is(), isEventSupported() (+8 more)

### Community 36 - "Customer"
Cohesion: 0.14
Nodes (13): CustomerRepository, ApplicationDbContext, Task, Customer, DateOfBirth, Email, FirstName, FullName (+5 more)

### Community 37 - "Order"
Cohesion: 0.12
Nodes (15): Order, Address, AddressId, Customer, CustomerId, DeliveryDate, OrderItems, SubTotal (+7 more)

### Community 38 - ".CreateOrderAsync"
Cohesion: 0.15
Nodes (11): Book, OrderItem, CreateOrderDto, AddressId, CorrelationId, CustomerSub, Order, CheckoutController (+3 more)

### Community 39 - "Ee"
Cohesion: 0.16
Nodes (14): be(), p(), $e(), Ee(), I(), je(), l(), R() (+6 more)

### Community 40 - "domManip"
Cohesion: 0.13
Nodes (17): boxModelAdjustment(), buildFragment(), buildParams(), cloneCopyEvent(), curCSS(), disableScript(), DOMEval(), domManip() (+9 more)

### Community 41 - "find"
Cohesion: 0.18
Nodes (17): addCombinator(), assert(), compile(), condense(), createPositionalPseudo(), elementMatcher(), find(), markFunction() (+9 more)

### Community 42 - "domManip"
Cohesion: 0.13
Nodes (17): boxModelAdjustment(), buildFragment(), buildParams(), cloneCopyEvent(), curCSS(), disableScript(), DOMEval(), domManip() (+9 more)

### Community 43 - ".ConfigureDependencyInjection"
Cohesion: 0.13
Nodes (12): AmazonRekognitionClient, AmazonS3Client, LocalImageValidationService, Stream, Task, IImageValidationService, Stream, Task (+4 more)

### Community 44 - "UpdateBookDto"
Cohesion: 0.12
Nodes (16): UpdateBookDto, Author, BookId, BookTypeId, ConditionId, CoverImage, CoverImageFileName, GenreId (+8 more)

### Community 45 - "Bookstore.Domain.Offers"
Cohesion: 0.13
Nodes (9): OfferStatus, Approved, Paid, PendingApproval, Received, Rejected, Bookstore.Web.ViewModel.Resale, Bookstore.Domain.Offers (+1 more)

### Community 46 - "ReferenceDataItemCreateUpdateViewModel"
Cohesion: 0.17
Nodes (13): ReferenceDataController, ActionResult, HttpPost, ReferenceDataType, Task, ReferenceDataItemCreateUpdateViewModel, DataTypes, Id (+5 more)

### Community 47 - "PaginatedViewModel"
Cohesion: 0.12
Nodes (14): ErrorViewModel, RequestId, ShowRequestId, PaginatedViewModel, HasNextPage, HasPreviousPage, PageCount, PageIndex (+6 more)

### Community 48 - "ResaleCreateViewModel"
Cohesion: 0.12
Nodes (15): ResaleCreateViewModel, Author, BookName, BookPrice, BookTypes, Conditions, Genres, ISBN (+7 more)

### Community 49 - "CreateBookDto"
Cohesion: 0.13
Nodes (14): CreateBookDto, Author, BookTypeId, ConditionId, CoverImage, CoverImageFileName, GenreId, ISBN (+6 more)

### Community 50 - "ICustomerService"
Cohesion: 0.25
Nodes (6): CustomerService, ICustomerService, Customer, Task, ICustomerRepository, Task

### Community 51 - "CreateOfferDto"
Cohesion: 0.13
Nodes (14): OfferStatus, CreateOfferDto, Author, BookName, BookPrice, BookTypeId, ConditionId, CustomerSub (+6 more)

### Community 52 - "IOrderService"
Cohesion: 0.24
Nodes (8): IOrderService, OrderService, IEnumerable, IPaginatedList, Task, DashboardController, ActionResult, Task

### Community 53 - "SearchDetailsViewModel"
Cohesion: 0.13
Nodes (15): SearchDetailsViewModel, Author, BookId, BookName, ConditionName, CurrentPage, GenreName, ISBN (+7 more)

### Community 54 - "ShoppingCartIndexItemViewModel"
Cohesion: 0.14
Nodes (14): ShoppingCartIndexItemViewModel, BookId, BookName, HasLowStockLevels, ImageUrl, IsOutOfStock, Price, ShoppingCartItemId (+6 more)

### Community 55 - ".GetAsync"
Cohesion: 0.24
Nodes (6): ShoppingCartRepository, ApplicationDbContext, Task, IShoppingCartRepository, Task, ShoppingCart

### Community 56 - "AddressService"
Cohesion: 0.29
Nodes (5): AddressService, IAddressService, Address, IEnumerable, Task

### Community 57 - "Bookstore.Domain.Orders"
Cohesion: 0.16
Nodes (9): OrderStatus, Cancelled, Delivered, Ordered, Pending, Shipped, Bookstore.Web.Areas.Admin.Models.Dashboard.DashboardIndexViewModel, Bookstore.Domain.Orders (+1 more)

### Community 58 - "DashboardIndexViewModel"
Cohesion: 0.14
Nodes (13): DashboardIndexViewModel, LowStock, OffersThisMonth, OffersTotal, OrdersThisMonth, OrdersTotal, OutOfStock, PastDueOffers (+5 more)

### Community 59 - "InventoryDetailsViewModel"
Cohesion: 0.14
Nodes (14): InventoryDetailsViewModel, Author, BookType, Condition, CoverImageUrl, Genre, Id, ISBN (+6 more)

### Community 60 - "Animation"
Cohesion: 0.15
Nodes (14): adoptValue(), ajaxConvert(), ajaxHandleResponses(), Animation(), camelCase(), createFxNow(), createTween(), defaultPrefilter() (+6 more)

### Community 61 - "AddressIndexItemViewModel"
Cohesion: 0.15
Nodes (12): AddressIndexItemViewModel, AddressLine1, AddressLine2, City, Country, Id, State, ZipCode (+4 more)

### Community 62 - "Animation"
Cohesion: 0.15
Nodes (14): adoptValue(), ajaxConvert(), ajaxHandleResponses(), Animation(), camelCase(), createFxNow(), createTween(), defaultPrefilter() (+6 more)

### Community 63 - "Animation"
Cohesion: 0.15
Nodes (14): adoptValue(), ajaxConvert(), ajaxHandleResponses(), Animation(), camelCase(), createFxNow(), createTween(), defaultPrefilter() (+6 more)

### Community 64 - "IAddressRepository"
Cohesion: 0.21
Nodes (6): DeleteAddressDto, AddressId, CustomerSub, IAddressRepository, IEnumerable, Task

### Community 65 - "WishlistController"
Cohesion: 0.29
Nodes (6): MoveAllWishlistItemsToShoppingCartDto, CorrelationId, WishlistController, ActionResult, HttpPost, Task

### Community 66 - "IOfferService"
Cohesion: 0.32
Nodes (4): IOfferService, OfferService, Offer, Task

### Community 67 - "InventoryIndexListItemViewModel"
Cohesion: 0.15
Nodes (13): InventoryIndexListItemViewModel, Author, BookType, Condition, Genre, Id, Name, Price (+5 more)

### Community 68 - "OrderDetailsViewModel"
Cohesion: 0.15
Nodes (13): OrderDetailsItemViewModel, BookId, BookName, ImageUrl, Price, OrderDetailsViewModel, DeliveryDate, OrderId (+5 more)

### Community 69 - "ResaleIndexItemViewModel"
Cohesion: 0.15
Nodes (13): ResaleIndexItemViewModel, Author, BookName, BookType, Condition, Genre, ISBN, OfferStatus (+5 more)

### Community 70 - "e"
Cohesion: 0.19
Nodes (10): e(), F(), L(), Qe(), j(), je(), f(), p() (+2 more)

### Community 71 - "BookstoreConfiguration"
Cohesion: 0.17
Nodes (6): BookstoreConfiguration, Instance, Dictionary, Startup, IAppBuilder, Lazy

### Community 72 - ".GetSetting"
Cohesion: 0.27
Nodes (6): S3FileService, Stream, Task, AuthenticationConfig, IAppBuilder, TransferUtility

### Community 73 - "SearchController"
Cohesion: 0.27
Nodes (7): AddToShoppingCartDto, BookId, CorrelationId, Quantity, SearchController, ActionResult, Task

### Community 74 - "CancelOrderDto"
Cohesion: 0.26
Nodes (7): CancelOrderDto, CustomerSub, OrderId, OrdersController, ActionResult, HttpPost, Task

### Community 75 - "IReferenceDataService"
Cohesion: 0.32
Nodes (4): IReferenceDataService, ReferenceDataService, IEnumerable, Task

### Community 76 - "OfferIndexItemViewModel"
Cohesion: 0.17
Nodes (12): OfferIndexItemViewModel, Author, BookName, Condition, CustomerName, Genre, OfferDate, OfferId (+4 more)

### Community 77 - "matcherFromTokens"
Cohesion: 0.20
Nodes (12): addCombinator(), condense(), createPositionalPseudo(), elementMatcher(), markFunction(), matcherFromGroupMatchers(), matcherFromTokens(), multipleContexts() (+4 more)

### Community 78 - "domManip"
Cohesion: 0.20
Nodes (12): buildFragment(), buildParams(), cloneCopyEvent(), disableScript(), DOMEval(), domManip(), getAll(), isArrayLike() (+4 more)

### Community 79 - "ft"
Cohesion: 0.21
Nodes (12): ct(), _e(), ft(), l(), M(), mt(), R(), t() (+4 more)

### Community 80 - "matcherFromTokens"
Cohesion: 0.20
Nodes (12): addCombinator(), condense(), createPositionalPseudo(), elementMatcher(), markFunction(), matcherFromGroupMatchers(), matcherFromTokens(), multipleContexts() (+4 more)

### Community 81 - "LocalAuthenticationMiddleware"
Cohesion: 0.23
Nodes (7): ClaimsPrincipalExtensions, LocalAuthenticationMiddleware, Task, ClaimsIdentity, IOwinContext, IPrincipal, OwinMiddleware

### Community 82 - "AddressCreateUpdateViewModel"
Cohesion: 0.17
Nodes (12): AddressCreateUpdateViewModel, AddressLine1, AddressLine2, City, Country, Id, ReturnUrl, State (+4 more)

### Community 83 - "OrderIndexItemViewModel"
Cohesion: 0.18
Nodes (10): OrderIndexItemViewModel, DeliveryDate, Id, OrderStatus, SubTotal, OrderIndexViewModel, OrderItems, DateTime (+2 more)

### Community 84 - "domManip"
Cohesion: 0.20
Nodes (12): buildFragment(), buildParams(), cloneCopyEvent(), disableScript(), DOMEval(), domManip(), getAll(), isArrayLike() (+4 more)

### Community 85 - "domManip"
Cohesion: 0.20
Nodes (12): buildFragment(), buildParams(), cloneCopyEvent(), disableScript(), DOMEval(), domManip(), getAll(), isArrayLike() (+4 more)

### Community 86 - "matcherFromTokens"
Cohesion: 0.20
Nodes (12): addCombinator(), condense(), createPositionalPseudo(), elementMatcher(), markFunction(), matcherFromGroupMatchers(), matcherFromTokens(), multipleContexts() (+4 more)

### Community 87 - "domManip"
Cohesion: 0.20
Nodes (12): buildFragment(), buildParams(), cloneCopyEvent(), disableScript(), DOMEval(), domManip(), getAll(), isArrayLike() (+4 more)

### Community 88 - "matcherFromTokens"
Cohesion: 0.20
Nodes (12): addCombinator(), condense(), createPositionalPseudo(), elementMatcher(), markFunction(), matcherFromGroupMatchers(), matcherFromTokens(), multipleContexts() (+4 more)

### Community 89 - "OfferFilters"
Cohesion: 0.24
Nodes (8): OfferFilters, Author, BookName, ConditionId, GenreId, OfferStatus, IEnumerable, IPaginatedList

### Community 90 - ".GetAllReferenceDataAsync"
Cohesion: 0.53
Nodes (4): InventoryController, ActionResult, HttpPost, Task

### Community 91 - "OffersController"
Cohesion: 0.49
Nodes (5): OffersController, ActionResult, HttpPost, OfferStatus, Task

### Community 92 - "lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js"
Cohesion: 0.22
Nodes (4): escapeAttributeValue(), onError(), onReset(), validationInfo()

### Community 93 - "SearchIndexItemViewModel"
Cohesion: 0.18
Nodes (11): SearchIndexItemViewModel, Author, BookId, BookName, ConditionName, GenreName, ImageUrl, Price (+3 more)

### Community 94 - "Scripts/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js"
Cohesion: 0.22
Nodes (4): escapeAttributeValue(), onError(), onReset(), validationInfo()

### Community 95 - "OrderFilters"
Cohesion: 0.20
Nodes (9): OrderStatus, UpdateOrderStatusDto, OrderId, OrderStatus, OrderFilters, OrderDateFromFilter, OrderDateToFilter, OrderStatusFilter (+1 more)

### Community 96 - "Controller"
Cohesion: 0.24
Nodes (7): AdminAreaControllerBase, OrdersController, ActionResult, HttpPost, Task, Controller, OrderDetailsViewModel

### Community 97 - "ReferenceDataFilters"
Cohesion: 0.24
Nodes (7): ReferenceDataFilters, ReferenceDataType, IPaginatedList, ReferenceDataIndexViewModel, Filters, Items, List

### Community 98 - "InventoryIndexViewModel"
Cohesion: 0.20
Nodes (10): InventoryIndexViewModel, BookConditions, BookTypes, Filters, Genres, Items, Publishers, IEnumerable (+2 more)

### Community 99 - "WishlistIndexItemViewModel"
Cohesion: 0.22
Nodes (9): WishlistIndexItemViewModel, BookName, ImageUrl, Price, ShoppingCartItemId, WishlistIndexViewModel, WishlistItems, List (+1 more)

### Community 100 - "DbSecrets"
Cohesion: 0.22
Nodes (8): DbSecrets, DbInstanceIdentifier, Engine, Host, Password, Port, Username, Bookstore.Domain.AdminUser

### Community 101 - "BookFilters"
Cohesion: 0.22
Nodes (8): BookFilters, Author, BookTypeId, ConditionId, GenreId, LowStock, Name, PublisherId

### Community 102 - "Entity"
Cohesion: 0.22
Nodes (7): Entity, CreatedBy, CreatedOn, Id, RowVersion, UpdatedOn, DateTime

### Community 103 - "OrderItem"
Cohesion: 0.22
Nodes (8): OrderItem, Book, BookId, Order, OrderId, Quantity, Book, Entity

### Community 104 - "OrderIndexListItemViewModel"
Cohesion: 0.22
Nodes (9): OrderIndexListItemViewModel, CustomerName, DeliveryDate, Id, OrderDate, OrderStatus, Total, DateTime (+1 more)

### Community 105 - "ImageTypesAttribute"
Cohesion: 0.22
Nodes (3): ImageTypesAttribute, MaxFileSizeAttribute, ValidationAttribute

### Community 106 - "find"
Cohesion: 0.36
Nodes (9): addCombinator(), compile(), elementMatcher(), find(), matcherFromTokens(), select(), testContext(), tokenize() (+1 more)

### Community 107 - "UpdateReferenceDataItemDto"
Cohesion: 0.29
Nodes (7): CreateReferenceDataItemDto, ReferenceDataType, Text, UpdateReferenceDataItemDto, Id, ReferenceDataType, Text

### Community 108 - "OfferIndexViewModel"
Cohesion: 0.25
Nodes (8): OfferIndexViewModel, BookConditions, Filters, Genres, Items, IEnumerable, List, SelectListItem

### Community 110 - "AddressController"
Cohesion: 0.54
Nodes (4): AddressController, ActionResult, HttpPost, Task

### Community 111 - "matcherFromGroupMatchers"
Cohesion: 0.29
Nodes (8): assert(), condense(), createPositionalPseudo(), markFunction(), matcherFromGroupMatchers(), multipleContexts(), setDocument(), setMatcher()

### Community 112 - "Contributing Guidelines"
Cohesion: 0.25
Nodes (7): Code of Conduct, Contributing Guidelines, Contributing via Pull Requests, Finding contributions to work on, Licensing, Reporting Bugs/Feature Requests, Security issue notifications

### Community 113 - "Bob's Used Books Classic"
Cohesion: 0.25
Nodes (7): Amazon Cognito first run, Bob's Used Books Classic, Deleting the resources, Deployment, Getting started, Overview, Prerequisites

### Community 114 - "AdminAreaRegistration"
Cohesion: 0.29
Nodes (5): AdminAreaRegistration, AreaName, AreaRegistration, AreaRegistrationContext, Bookstore.Web.Areas

### Community 115 - "ShoppingCartController"
Cohesion: 0.43
Nodes (4): ShoppingCartController, ActionResult, HttpPost, Task

### Community 116 - "nodeName"
Cohesion: 0.29
Nodes (7): boxModelAdjustment(), createButtonPseudo(), createInputPseudo(), curCSS(), getWidthOrHeight(), manipulationTarget(), nodeName()

### Community 117 - "nodeName"
Cohesion: 0.29
Nodes (7): boxModelAdjustment(), createButtonPseudo(), createInputPseudo(), curCSS(), getWidthOrHeight(), manipulationTarget(), nodeName()

### Community 118 - ".ResizeImageAsync"
Cohesion: 0.33
Nodes (4): Stream, Task, Stream, Task

### Community 119 - "CreateOrUpdateCustomerDto"
Cohesion: 0.33
Nodes (5): CreateOrUpdateCustomerDto, CustomerSub, FirstName, LastName, Username

### Community 120 - "IFileService"
Cohesion: 0.40
Nodes (3): IFileService, Stream, Task

### Community 121 - "OrderStatistics"
Cohesion: 0.33
Nodes (5): OrderStatistics, OrdersThisMonth, OrdersTotal, PastDueOrders, PendingOrders

### Community 122 - "c"
Cohesion: 0.40
Nodes (3): c(), b(), d()

### Community 124 - ".Create"
Cohesion: 0.47
Nodes (4): ResaleController, ActionResult, HttpPost, Task

### Community 125 - ".GetSelectListForEnum"
Cohesion: 0.33
Nodes (4): MvcHelpers, IEnumerable, SelectListItem, HtmlHelper

### Community 126 - "d"
Cohesion: 0.40
Nodes (3): c(), b(), d()

### Community 127 - "LocalFileService"
Cohesion: 0.50
Nodes (3): LocalFileService, Stream, Task

### Community 128 - "BookStatistics"
Cohesion: 0.40
Nodes (4): BookStatistics, LowStock, OutOfStock, StockTotal

### Community 130 - ".GetDescription"
Cohesion: 0.40
Nodes (3): EnumExtensions, DescriptionAttribute, Enum

### Community 131 - "ErrorController"
Cohesion: 0.60
Nodes (3): ErrorController, ActionResult, Route

### Community 132 - "OrderIndexViewModel"
Cohesion: 0.40
Nodes (5): OrderIndexViewModel, Filters, Items, List, PaginatedViewModel

### Community 133 - "underscore-min.js"
Cohesion: 0.70
Nodes (4): n(), t(), r(), t()

### Community 134 - "getWidthOrHeight"
Cohesion: 0.40
Nodes (5): boxModelAdjustment(), curCSS(), getWidthOrHeight(), manipulationTarget(), nodeName()

### Community 135 - "SearchIndexViewModel"
Cohesion: 0.40
Nodes (5): SearchIndexViewModel, Books, SearchString, SortBy, List

### Community 136 - "getWidthOrHeight"
Cohesion: 0.40
Nodes (5): boxModelAdjustment(), curCSS(), getWidthOrHeight(), manipulationTarget(), nodeName()

### Community 137 - "expectSync"
Cohesion: 0.50
Nodes (4): expectSync(), leverageNative(), returnTrue(), safeActiveElement()

### Community 138 - "expectSync"
Cohesion: 0.50
Nodes (4): expectSync(), leverageNative(), returnTrue(), safeActiveElement()

### Community 142 - "expectSync"
Cohesion: 0.50
Nodes (4): expectSync(), leverageNative(), returnTrue(), safeActiveElement()

### Community 143 - "expectSync"
Cohesion: 0.50
Nodes (4): expectSync(), leverageNative(), returnTrue(), safeActiveElement()

### Community 145 - "resolve"
Cohesion: 0.67
Nodes (3): Identity(), resolve(), Thrower()

### Community 148 - "resolve"
Cohesion: 0.67
Nodes (3): Identity(), resolve(), Thrower()

## Knowledge Gaps
- **525 isolated node(s):** `net6.0`, `Amazon.CDK.Lib (2.188.0)`, `Cdklabs.CdkNag (2.35.66)`, `Constructs (10.4.2)`, `Amazon.Jsii.Analyzers (*)` (+520 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1275 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **21 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Bookstore.Domain.Books` connect `Bookstore.Domain.Books` to `BookStatistics`, `BookFilters`, `OrderItem`, `PaginatedViewModel`, `CreateBookDto`, `BookService`, `Bookstore.Web.Helpers`, `Bookstore.Domain`, `ShoppingCart`, `IOrderRepository`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._
- **Why does `Bookstore.Domain.Orders` connect `Bookstore.Domain.Orders` to `.CreateOrderAsync`, `OrderItem`, `Bookstore.Domain.Offers`, `Bookstore.Domain.Books`, `CheckoutAddressViewModel`, `BookService`, `Bookstore.Web.Helpers`, `OrderIndexItemViewModel`, `Bookstore.Domain`, `OrderStatistics`, `OfferRepository`, `IOrderRepository`, `OrderFilters`?**
  _High betweenness centrality (0.033) - this node is a cross-community bridge._
- **Why does `Bookstore.Domain` connect `Bookstore.Domain` to `DateTimeExtensions`, `.GetDescription`, `Entity`, `.ConfigureDependencyInjection`, `Bookstore.Domain.Offers`, `Bookstore.Domain.Books`, `OrderIndexItemViewModel`, `IFileService`, `PaginatedList`, `Bookstore.Domain.Orders`?**
  _High betweenness centrality (0.029) - this node is a cross-community bridge._
- **What connects `net6.0`, `Amazon.CDK.Lib (2.188.0)`, `Cdklabs.CdkNag (2.35.66)` to the rest of the system?**
  _525 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `jquery-3.7.1.min.js` be split into smaller, more focused modules?**
  _Cohesion score 0.07017543859649122 - nodes in this community are weakly interconnected._
- **Should `CoreStack` be split into smaller, more focused modules?**
  _Cohesion score 0.058823529411764705 - nodes in this community are weakly interconnected._
- **Should `Scripts/jquery/jquery.min.js` be split into smaller, more focused modules?**
  _Cohesion score 0.07312925170068027 - nodes in this community are weakly interconnected._