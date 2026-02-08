using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // GET: /Admin/Users
    public async Task<IActionResult> Users()
    {
        // Get all users, sorted by LastLoginTime descending
        var users = await _userManager.Users
            .OrderByDescending(u => u.LastLoginTime)
            .ToListAsync();

        return View(users);
    }

    // POST: /Admin/UserAction
    [HttpPost]
    public async Task<IActionResult> UserAction(string actionType, List<string> selectedUserIds)
    {
        switch (actionType)
        {
            case "Block":
            case "Unblock":
            case "Delete":
                if (selectedUserIds == null || selectedUserIds.Count == 0)
                {
                    TempData["Message"] = "No users selected";
                    return RedirectToAction("Users");
                }
                foreach (var id in selectedUserIds)
                {
                    var user = await _userManager.FindByIdAsync(id);
                    if (user == null) continue;

                    if (actionType == "Block")
                        user.Status = UserStatus.Blocked;
                    else if (actionType == "Unblock")
                        user.Status = UserStatus.Active;
                    else if (actionType == "Delete")
                        await _userManager.DeleteAsync(user);

                    if (actionType != "Delete") // Update status if not deleted
                        await _userManager.UpdateAsync(user);
                }
                break;

            case "DeleteUnverified":
                // Delete all users whose status is Unverified
                var unverifiedUsers = await _userManager.Users
                                        .Where(u => u.Status == UserStatus.Unverified)
                                        .ToListAsync();
                foreach (var user in unverifiedUsers)
                {
                    await _userManager.DeleteAsync(user);
                }
                break;
        }

        TempData["Message"] = $"Action '{actionType}' applied successfully!";
        return RedirectToAction("Users");
    }
}

