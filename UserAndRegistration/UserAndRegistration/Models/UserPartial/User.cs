using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace UserAndRegistration.Models
{
    [MetadataType(typeof(UserMetaData))]
    public partial class User
    {
        public string ConfirmPassword { get; set; }
        
    }

    public class UserMetaData
    {
        [Display(Name = "First Name ")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "First Name Required ")]
        public string First_Name { get; set; }

        [Display(Name = "Last Name ")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Last Name Required ")]
        public string Last_Name { get; set; }

        [Display(Name = "Phone No ")]
        public string Mobile_No { get; set; }

        [Display(Name = "Company Name ")]
        public string Company_Name { get; set; }

        [Display(Name = "Email Id ")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email Id Required ")]
        public string EmailID { get; set; }

        [Display(Name = "Password ")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Password is Required ")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Minimum 6 character required")]
        public string Password { get; set; }

        [Display(Name = "Confirm Password ")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password and confirm Password does not match ")]
        public string ConfirmPassword { get; set; }
        public System.Guid ActivationCode { get; set; }
        public bool EmailVerified{ get; set; }
    }
}