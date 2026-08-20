namespace Bookstore.Web.Helpers
{
    /// <summary>
    /// Provides the shopping-cart correlation cookie from an ASP.NET Core HttpContext.
    /// </summary>
    public static class HttpContextExtensions
    {
        private const string CookieKey = "ShoppingCartId";

        public static string GetShoppingCartCorrelationId(this HttpContext context)
        {
            string shoppingCartClientId = context.Request.Cookies[CookieKey];

            if (string.IsNullOrWhiteSpace(shoppingCartClientId))
            {
                shoppingCartClientId = context.User.Identity?.IsAuthenticated == true
                    ? context.User.GetSub()
                    : Guid.NewGuid().ToString();
            }

            context.Response.Cookies.Append(CookieKey, shoppingCartClientId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/"
            });

            return shoppingCartClientId;
        }
    }
}
