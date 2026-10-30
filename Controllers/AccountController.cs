using GoPlay.Web.Data;
using GoPlay.Web.Models;
using GoPlay.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GoPlay.Web.Controllers;

// Xử lý đăng ký/đăng nhập/đăng xuất (tương đương AuthController bên Java,
// nhưng thay JWT bằng Identity cookie auth).
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    // GET: /Account/Register — hiện form đăng ký.
    [HttpGet]
    public IActionResult Register() => View();

    // POST: /Account/Register — xử lý tạo tài khoản (role mặc định Customer).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true // v1 không gửi mail xác nhận, đánh dấu luôn.
        };

        // CreateAsync tự băm mật khẩu (tương đương BCryptPasswordEncoder).
        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Tài khoản mới luôn là Customer.
            await userManager.AddToRoleAsync(user, IdentityRoles.Customer);
            // Đăng nhập luôn sau khi đăng ký để UX mượt (tương tự Java).
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        // Đưa lỗi từ Identity vào ModelState để hiện trên form.
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    // GET: /Account/Login — hiện form đăng nhập.
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Lưu returnUrl để sau đăng nhập quay lại đúng trang bị chặn.
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login — kiểm tra mật khẩu và tạo cookie.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Đăng nhập bằng email + mật khẩu. lockoutOnFailure: true để Identity
        // tự khoá tài khoản khi sai mật khẩu nhiều lần (bảo mật).
        var result = await signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Chỉ chuyển hướng nội bộ, tránh open redirect.
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // Gộp chung lỗi để tránh lộ tài khoản có tồn tại hay không.
        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
        return View(model);
    }

    // POST: /Account/Logout — xoá cookie, phải dùng POST để tránh bị gọi khi refresh.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}