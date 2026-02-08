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


        // Check if user is blocked
        if (user.Status == UserStatus.Blocked)
        {
            ModelState.AddModelError("", "Your account is blocked");
            return View(model);
        }

        // unverified users can still login
        //if (!await _userManager.IsEmailConfirmedAsync(user))
        //{
        //    ModelState.AddModelError("", "Please confirm your email before logging in.");
        //    return View(model);
        //}

        var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, false, false);

        if (result.Succeeded)
        {
            user.LastLoginTime = DateTime.Now;
            await _userManager.UpdateAsync(user);

            // Add Status as a claim
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("UserStatus", user.Status.ToString())
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

        // ✅ CHANGE IS HERE (no condition needed)
        user.Status = UserStatus.Active;
        await _userManager.UpdateAsync(user);

        TempData["Message"] = "Email confirmed! Your account is now verified.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Verify()
    {
        ViewBag.UserId = TempData["ConfirmUserId"];
        ViewBag.Token = TempData["ConfirmToken"];
        return View();
    }



}
