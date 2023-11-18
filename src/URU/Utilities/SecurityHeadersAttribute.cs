using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace URU.Utilities
{
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ViewResult)
            {
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    context.HttpContext.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
                if (!context.HttpContext.Response.Headers.ContainsKey("Referrer-Policy"))
                {
                    context.HttpContext.Response.Headers.Append("Referrer-Policy", "no-referrer");
                }

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
                if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy"))
                {
                    var csp = "default-src 'self'; object-src 'none'; script-src https://*.spotify.com/ 'self'; frame-src https://*.spotify.com/; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-downloads allow-scripts allow-popups; base-uri 'self'; require-trusted-types-for 'script';";
                    context.HttpContext.Response.Headers.Append("Content-Security-Policy", csp);
                }
            }
        }
    }
}