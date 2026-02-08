using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Security.Claims;




public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly EmailService _emailService;

    public AccountController(UserManager<ApplicationUser> userManager,
                         SignInManager<ApplicationUser> signInManager,
                         EmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
    }


    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Check if email already exists BEFORE trying to create user
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            // Add model-level error so it shows only in validation summary
            ModelState.AddModelError(string.Empty, "This email is already registered. Please log in instead.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            Status = UserStatus.Unverified,
            LastLoginTime = null
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Generate email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Build confirmation link
            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, token },
                Request.Scheme);

            // Send email
            await _emailService.SendEmailAsync(
                user.Email,
                "Confirm your account",
                $"Please confirm your account by clicking <a href='{confirmationLink}'>here</a>.");

            TempData["ConfirmUserId"] = user.Id;
            TempData["ConfirmToken"] = token;

            return RedirectToAction("Verify");

        }

        // If there are other errors, show them in validation summary
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }


    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError("", "Email not found. Please register first.");
            return View(model);
        }

        if (user.Status == UserStatus.Blocked)
        {
            ModelState.AddModelError("", "Your account is blocked");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, false, false);

        if (result.Succeeded)
        {
            user.LastLoginTime = DateTime.Now;
            await _userManager.UpdateAsync(user);

            // ✅ Sign in with status claim
            await _signInManager.SignOutAsync(); // ensure fresh login
            var claims = new List<Claim>
        {
            new Claim("UserStatus", user.Status.ToString())
        };
            await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, claims);

            TempData["StatusMessage"] = $"Your account status is: {user.Status}";
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", "Invalid email or password");
        return View(model);
    }



    // GET: /Account/Logout
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    // GET: /Account/ConfirmEmail
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return RedirectToAction("Login");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return RedirectToAction("Login");

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            TempData["Message"] = "Email confirmation failed.";
            return RedirectToAction("Login");
        }

        // Update status
        user.Status = UserStatus.Active;
        await _userManager.UpdateAsync(user);

        //// Auto-login after verification
        //await _signInManager.SignInAsync(user, isPersistent: false);
        //TempData["Message"] = "Email confirmed! You are now logged in.";
        //return RedirectToAction("Index", "Home");

        // ✅ Sign in the user with the updated claim
        await _signInManager.SignOutAsync(); // ensure fresh login
        var claims = new List<Claim>
        {
            new Claim("UserStatus", user.Status.ToString())
        };
        await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, claims);

        TempData["Message"] = "Email confirmed! You are now logged in.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Verify(string message = null)
    {
        ViewBag.UserId = TempData["ConfirmUserId"];
        ViewBag.Token = TempData["ConfirmToken"];
        ViewBag.Message = message;

        // Keep TempData for next request
        TempData.Keep("ConfirmUserId");
        TempData.Keep("ConfirmToken");

        return View();
    }





}
