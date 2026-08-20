namespace Bookstore.Web.Helpers
{
    /// <summary>
    /// HTTP request helpers for ASP.NET Core (replaces the OWIN IOwinRequest extensions).
    /// </summary>
    public static class HttpRequestExtensions
    {
        /// <summary>Returns the absolute URI used as the OIDC callback (redirect) URI.</summary>
        public static string GetReturnUrl(this HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}/signin-oidc";
        }
    }
}
