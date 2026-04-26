using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.ViewModels.Account
{
    public class HobbyOption
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }

    public class CompleteProfileViewModel
    {
        [Required(ErrorMessage = "请输入您的昵称。")]
        [Display(Name = "昵称")]
        [StringLength(50)]
        public string Nickname { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "注册邮箱")]
        public string Email { get; set; }

        [Display(Name = "出生年月")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "性别")]
        public string Gender { get; set; }

        [Display(Name = "我的戏曲爱好（可多选）")]
        public List<HobbyOption> HobbyOptions { get; set; }
        // ---------------------------

        [Display(Name = "籍贯")]
        public string SelectedProvince { get; set; }
        public List<SelectListItem> ProvinceOptions { get; set; }

        [Display(Name = "个人图像")]
        public IFormFile? AvatarImage { get; set; } // 接收上传的文件
        public string? ExistingAvatarUrl { get; set; } // 用于回显已有图片

        [Display(Name = "个人简介")]
        [DataType(DataType.MultilineText)]
        [StringLength(300)]
        public string Bio { get; set; }

        public CompleteProfileViewModel()
        {
            Gender = "保密";
            HobbyOptions = new List<HobbyOption>();
            ProvinceOptions = new List<SelectListItem>();
        }
    }
}