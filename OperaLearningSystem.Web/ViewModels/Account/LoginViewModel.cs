using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.ViewModels.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "电子邮箱是必填项。")]
        [EmailAddress(ErrorMessage = "请输入有效的电子邮箱地址。")]
        [Display(Name = "电子邮箱")]
        public string Email { get; set; }

        [Required(ErrorMessage = "密码是必填项。")]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; }

        [Display(Name = "记住我?")]
        public bool RememberMe { get; set; }
    }
}