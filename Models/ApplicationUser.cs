using Microsoft.AspNetCore.Identity;

namespace GoPlay.Web.Models;

// Người dùng: kế thừa IdentityUser để tái sử dụng sẵn Id/Email/PasswordHash/...
// (tương đương model/User.java nhưng thay cơ chế auth thủ công bằng ASP.NET Core Identity).
public class ApplicationUser : IdentityUser
{
    // Họ tên hiển thị, thêm vào ngoài các cột có sẵn của IdentityUser.
    public string FullName { get; set; } = string.Empty;
}