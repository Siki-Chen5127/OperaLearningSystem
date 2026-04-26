using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "请输入您的注册邮箱")]
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        public string Email { get; set; }
    }
}