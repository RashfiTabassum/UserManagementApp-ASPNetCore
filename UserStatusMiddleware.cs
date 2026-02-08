using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;

    public UserStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context,
                             UserManager<ApplicationUser> userManager,
                             SignInManager<ApplicationUser> signInManager)
    {
        var path = context.Request.Path.Value.ToLower();

        // Skip login, register, verify pages, and static files
        if (path.Contains("/account/login") ||
            path.Contains("/account/register") ||
            path.Contains("/account/verify") ||
            path.StartsWith("/css") ||
            path.StartsWith("/js") ||
            path.StartsWith("/lib"))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity.IsAuthenticated)
        {
            var user = await userManager.GetUserAsync(context.User);

            if (user == null || user.Status == UserStatus.Blocked)
            {
                // logout immediately and redirect
                await signInManager.SignOutAsync();
                context.Response.Redirect("/Account/Login");
                return;
            }

            if (user.Status == UserStatus.Unverified && path.StartsWith("/admin"))
            {
                // Redirect unverified users trying to access admin pages
                // Set TempData message
               // context.Items["VerifyMessage"] = "Please verify your email first to access the user management page.";
                context.Response.Redirect("/Account/Verify?message=verifyfirst");
                return;
            }
        }

        await _next(context);
    }
}
