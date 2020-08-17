using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Policy;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using UserAndRegistration.Models;

namespace UserAndRegistration.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration(User user)
        {
            if(ModelState.IsValid)
            {
                //Email already exist             
                var isExist = IsEmailExist(user.EmailID);
                if (isExist)
                {
                    ViewBag.Message = "EmailID already exist!!";
                }
                else
                {
                    //Generate Activation 
                    user.ActivationCode = Guid.NewGuid();
                    
                    //verification of emailID is not done
                    user.EmailVerified = false;
                    
                    //Saving data into database
                   using (LoginDatabaseEntities2 lde = new LoginDatabaseEntities2())
                    {
                        lde.Users.Add(user);
                        lde.SaveChanges();
                        ModelState.Clear();
                        ViewBag.Message = "Registered Successfully";
                    }
                 
                    //Send verification email link                  
                    bool status=sendVerificationLink(user.EmailID,user.ActivationCode.ToString());
                    if(status==true)
                    {
                        ViewBag.Message = "Verification link has been sent to your email";
                    }
                    else
                    {
                        return View("Registration", user);
                    }                   
                }
                user = null;
            }
            return View();
        }
        //Login
        [HttpGet]
        public ActionResult Login()
        {          
            return View();
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User user,UserLogin userLogin)
        {
           
                using(LoginDatabaseEntities2 lde = new LoginDatabaseEntities2())
                {
                    var v = lde.Users.Where(a => a.EmailID.Equals(user.EmailID) && a.Password.Equals(user.Password)).FirstOrDefault();
                    if(v!=null)
                    {
                        int timeout = userLogin.RememberMe ? 30 : 1;//30 days
                        var cookie = FormsAuthentication.GetAuthCookie(userLogin.EmailID, userLogin.RememberMe);
                        // Since they want to be remembered, set the expiration for 30 days
                        cookie.Expires = DateTime.Now.AddDays(timeout);
                        // Store the cookie in the Response
                        Response.Cookies.Add(cookie);
                        Session["LoggedUserID"] = user.EmailID.ToString();
                        return RedirectToAction("AfterLogin");
                    }
                    else
                    {
                        userLogin.logingErrorMessage = "Invalid UserID or Password";
                        return View("Login",userLogin);
                    }
                }          
        }
        [Authorize]
        public ActionResult AfterLogin()
        {
            if(Session["LoggedUserID"]!=null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index");
            }
        }
        public ActionResult Logout()
        {
            Session.Abandon();
            FormsAuthentication.SignOut();
            return RedirectToAction("Login","User");
        }
        public ActionResult ActivateAccount(string id)
        {
            using(LoginDatabaseEntities2 lde = new LoginDatabaseEntities2())
            {
                var v = lde.Users.Where(a => a.ActivationCode.ToString() == id).FirstOrDefault();
               if(v!=null)
                {
                              
                        ViewBag.Message = "Account is Successfully Activated";
                        v.EmailVerified = true;
                        v.ConfirmPassword = v.Password;//table does not have confirm passwoed
                        lde.SaveChanges();
                        return View("AfterRegistration");
                }
                ViewBag.Message = "Account is not Activated";
            }
            return RedirectToAction("Registration", "User");
        }
        [NonAction]
        bool IsEmailExist(string emailID)
        {
            using (LoginDatabaseEntities2 lde = new LoginDatabaseEntities2())
            {
                var v = lde.Users.Where(a => a.EmailID == emailID).FirstOrDefault();
                return v == null ? false : true;
            }
        }
        public bool sendVerificationLink(string toEmail,string activationCode)
        {
            try
            {
                string senderEmail = System.Configuration.ConfigurationManager.AppSettings["SenderEmail"].ToString();
                string senderPassword= System.Configuration.ConfigurationManager.AppSettings["SenderPassword"].ToString();

                SmtpClient client = new SmtpClient("smtp.gmail.com",587);
                client.EnableSsl = true;
                client.Timeout = 100000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                //var verifyUrl = "https:/localhost:44344/User/ActivateAccount/" + activationCode;
               // var verifyUrl = Url.Action("ActivateAccount","User",new {id = activationCode });
                var verifyUrl= this.Url.Action("ActivateAccount", "User", new { id = activationCode }, this.Request.Url.Scheme);
                string subject = "Registration link for verification";
                string emailBody = "<div>We are excited to tell you that your account is successfully created!!<br />Please click below link for verify your account.<br /><a href='"+verifyUrl+"'>Activation Link</a></div>";
                MailMessage mailMessage = new MailMessage(senderEmail, toEmail, subject, emailBody);
                mailMessage.IsBodyHtml = true;
                mailMessage.BodyEncoding = UTF8Encoding.UTF8;
                client.Send(mailMessage);
                return true;
            }
            catch(Exception ex)
            {
                ViewBag.Message = ex.Message;
                return false;
            }
            
        }
    }
}