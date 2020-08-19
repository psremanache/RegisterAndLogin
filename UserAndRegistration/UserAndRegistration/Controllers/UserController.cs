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

        //checks the activation code matches the data base activation code and save 
        //changes in database
        public ActionResult ActivateAccount(string id)
        {
            using(LoginDatabaseEntities2 lde = new LoginDatabaseEntities2())
            {
                var v = lde.Users.Where(a => a.ActivationCode.ToString() == id).FirstOrDefault();
               if(v!=null)
                {
                              
                        ViewBag.Message = "Account is Successfully Activated";
                        v.EmailVerified = true;
                        lde.Configuration.ValidateOnSaveEnabled = false; //v.ConfirmPassword = v.Password;//table does not have confirm password
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

        //sends verification link for registration or forget password
        public bool sendVerificationLink(string toEmail,string activationCode,string forEmail="ActivateAccount")
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
                string subject = "";
                string emailBody = "";
                if (forEmail == "ActivateAccount")
                {
                    var verifyUrl = this.Url.Action("ActivateAccount", "User", new { id = activationCode }, this.Request.Url.Scheme);
                    subject = "Registration link for verification";
                    emailBody = "<div>We are excited to tell you that your account is successfully created!!<br />Please click below link for verify your account.<br /><a href='" + verifyUrl + "'>Activation Link</a></div>";
                }
                else if(forEmail=="ResetPassword")
                {
                    var verifyUrl = this.Url.Action(forEmail, "User", new { id = activationCode }, this.Request.Url.Scheme);
                    subject = "Forgot Password Link";
                    emailBody = "<div>Click on below link for setting new password for your account!!<br /><a href='" + verifyUrl + "'>Activation Link</a></div>";
                
                }
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
        
        [HttpGet]
        //Opens page for asking email ID for sending verification link
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        //recieves email ID and checks it matches with data base
        //Generate forget password code
        public ActionResult ForgotPassword(string emailID)
        {
            
            using(LoginDatabaseEntities2 lde = new LoginDatabaseEntities2())
            {
                var user = lde.Users.Where(a => a.EmailID == emailID).FirstOrDefault();
                if (user != null)
                {             
                    string resetCode = Guid.NewGuid().ToString();
                    bool status=sendVerificationLink(emailID, resetCode, "ResetPassword");
                    user.ResetPasswordCode = resetCode;
                    lde.Configuration.ValidateOnSaveEnabled = false;
                    lde.SaveChanges();
                    if(status==true)
                        ViewBag.Message = "Verification link is sent to your email ID";
                    else
                        ViewBag.Message = "Failed to send Verification link to your email ID";
                }
                else
                {
                    ViewBag.Message = "No account with this email ID";                 
                }
            }
            return View();
        }

        //recieves verification code and checks it matches with database verf code
        //& it opens a page for password change
        public ActionResult ResetPassword(string id)
        {
            using(LoginDatabaseEntities2 lde = new LoginDatabaseEntities2())
            {
                var user = lde.Users.Where(a => a.ResetPasswordCode == id).FirstOrDefault();
                if(user!=null)
                {
                    ResetPasswordModel model = new ResetPasswordModel();
                    model.resetCode = id;
                    return View(model);
                }
                else
                {
                    return HttpNotFound();
                }
                
            }         
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        //here actual password is saved in database
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            string message = "";
            using(LoginDatabaseEntities2 lde = new LoginDatabaseEntities2())
            {
                var user = lde.Users.Where(a => a.ResetPasswordCode == model.resetCode).FirstOrDefault();
                if(user!=null)
                {
                    user.Password = model.newPassword;
                    user.ResetPasswordCode = "";//this is done so that link can be used only once
                    lde.Configuration.ValidateOnSaveEnabled = false;
                    lde.SaveChanges();
                    message = "Password is successfully changed";
                }
                else
                {
                    message = "Error occured";
                }
            }
            ViewBag.Message = message;
            return View(model);
        }
    }
}