using Bookstore.Domain.Books;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Web.Areas.Admin.Models.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Areas.Admin.Controllers
{
    public class DashboardController : AdminAreaControllerBase
    {
        private readonly IOrderService orderService;
        private readonly IOfferService offerService;
        private readonly IBookService bookService;

        public DashboardController(IOrderService orderService, IOfferService offerService, IBookService bookService)
        {
            this.orderService = orderService;
            this.offerService = offerService;
            this.bookService = bookService;
        }

        public async Task<IActionResult> Index()
        {
            var orderStats = await orderService.GetStatisticsAsync();
            var offerStats = await offerService.GetStatisticsAsync();
            var inventoryStats = await bookService.GetStatisticsAsync();

            var model = new DashboardIndexViewModel
            {
                PastDueOrders  = orderStats?.PastDueOrders ?? 0,
                PendingOrders  = orderStats?.PendingOrders ?? 0,
                OrdersThisMonth = orderStats?.OrdersThisMonth ?? 0,
                OrdersTotal    = orderStats?.OrdersTotal ?? 0,

                PendingOffers   = offerStats?.PendingOffers ?? 0,
                OffersThisMonth = offerStats?.OffersThisMonth ?? 0,
                OffersTotal     = offerStats?.OffersTotal ?? 0,

                LowStock   = inventoryStats?.LowStock ?? 0,
                OutOfStock = inventoryStats?.OutOfStock ?? 0,
                StockTotal = inventoryStats?.StockTotal ?? 0
            };

            return View(model);
        }
    }
}
