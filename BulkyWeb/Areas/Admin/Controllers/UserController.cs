
using System.Diagnostics;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
         private readonly ApplicationDbContext dbContext;
         private readonly UserManager<IdentityUser> userManager;

        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger,ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            this.dbContext = dbContext;
            this.userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string userId)
        {
            string roleId = dbContext.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;

            RoleManagementVM roleManagementVM = new RoleManagementVM()
            {
                ApplicationUser = this.dbContext.ApplicationUsers.FirstOrDefault(u => u.Id == userId),
                RoleList = dbContext.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id
                }),
                CompanyList = this.dbContext.Companies.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

            roleManagementVM.ApplicationUser.Role = this.dbContext.Roles.FirstOrDefault(u => u.Id == roleId).Name;
            return View(roleManagementVM);
        }

        [HttpPost]
         public IActionResult RoleManagement(RoleManagementVM roleVM)
        {
            string roleId = dbContext.UserRoles.FirstOrDefault(u => u.UserId == roleVM.ApplicationUser.Id).RoleId;
            string oldRole = dbContext.Roles.FirstOrDefault(u => u.Id == roleId).Name;

            if (!(roleVM.ApplicationUser.Role == oldRole))
            {
                // a role change has been made
                ApplicationUser applicationUser = this.dbContext.ApplicationUsers.FirstOrDefault(u => u.Id == roleVM.ApplicationUser.Id);

                if (roleVM.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = roleVM.ApplicationUser.CompanyId;
                }

                if (oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }

                this.dbContext.SaveChanges();

                this.userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                this.userManager.AddToRoleAsync(applicationUser, roleVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }

            return RedirectToAction("Index");
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> userList = dbContext.ApplicationUsers.Include(x => x.Company).ToList();

            var userRole = dbContext.UserRoles.ToList();
            var roles = dbContext.Roles.ToList();

            foreach (var user in userList)
            {
                var roleId = userRole.FirstOrDefault(x => x.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(x => x.Id == roleId).Name;

                // if the user does not have a company, create a new company with an empty name
                if (user.CompanyId == null)
                {
                    user.Company = new Company() { Name = "" };
                }
            }

            return Json(new { data = userList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var objFromDb = this.dbContext.ApplicationUsers.FirstOrDefault(u => u.Id == id);

            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                //user is currently locked, we will unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1);
            }

            this.dbContext.SaveChanges();

            return Json(new { success = true, message = "Operation successfully." });
        }

        #endregion
    }
}