using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.ViewModels.Account
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "电子邮箱是必填项。")]
        [EmailAddress(ErrorMessage = "请输入有效的电子邮箱地址。")]
        [Display(Name = "电子邮箱")]
        public string Email { get; set; }

        [Required(ErrorMessage = "密码是必填项。")]
        [StringLength(100, ErrorMessage = "{0} 至少需要 {2} 个字符，最多 {1} 个字符。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "确认密码")]
        [Compare("Password", ErrorMessage = "密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; }
    }
}