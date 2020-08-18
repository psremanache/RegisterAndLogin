using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UserAndRegistration.Models
{
    public class ResetPasswordModel
    {
        [Required(ErrorMessage ="New Password Required",AllowEmptyStrings =false)]
        [DataType(DataType.Password)]
        public string newPassword { get; set; }

        [Compare("newPassword",ErrorMessage ="New Paasword and confirm password does not match")]
        [DataType(DataType.Password)]
        public string confirmPassword { get; set; }

        [Required]
        public string resetCode { get; set; }
    }
}