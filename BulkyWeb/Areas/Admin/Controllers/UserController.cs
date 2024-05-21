
using System.Diagnostics;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
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
         private readonly RoleManager<IdentityRole> roleManager;
         private readonly UserManager<IdentityUser> userManager;
         private readonly IUnitOfWork unitOfWork;

        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger, IUnitOfWork unitOfWork, RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            this.unitOfWork = unitOfWork;
            this.roleManager = roleManager;
            this.userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string userId)
        {
            RoleManagementVM roleManagementVM = new RoleManagementVM()
            {
                ApplicationUser = this.unitOfWork.ApplicationUser.Get(u => u.Id == userId, includeProperties: "Company"),
                RoleList = roleManager.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id
                }),
                CompanyList = this.unitOfWork.Company.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

            roleManagementVM.ApplicationUser.Role = userManager.GetRolesAsync(unitOfWork.ApplicationUser.Get(u => u.Id == userId)).GetAwaiter().GetResult().FirstOrDefault();
            return View(roleManagementVM);
        }

        [HttpPost]
         public IActionResult RoleManagement(RoleManagementVM roleVM)
        {
            string oldRole = userManager.GetRolesAsync(unitOfWork.ApplicationUser.Get(u => u.Id == roleVM.ApplicationUser.Id)).GetAwaiter().GetResult().FirstOrDefault();

            ApplicationUser applicationUser = this.unitOfWork.ApplicationUser.Get(u => u.Id == roleVM.ApplicationUser.Id);

            if (!(roleVM.ApplicationUser.Role == oldRole))
            {
                // a role change has been made
                if (roleVM.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = roleVM.ApplicationUser.CompanyId;
                }

                if (oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }

                this.unitOfWork.ApplicationUser.Update(applicationUser);
                this.unitOfWork.Save();

                this.userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                this.userManager.AddToRoleAsync(applicationUser, roleVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            else{
                // no role change has been made
                if (oldRole == SD.Role_Company && roleVM.ApplicationUser.CompanyId != applicationUser.CompanyId)
                {
                    applicationUser.CompanyId = roleVM.ApplicationUser.CompanyId;
                    this.unitOfWork.ApplicationUser.Update(applicationUser);
                    this.unitOfWork.Save();
                }
            }

            return RedirectToAction("Index");
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> userList = this.unitOfWork.ApplicationUser.GetAll(includeProperties: "Company").ToList();

            foreach (var user in userList)
            {
                user.Role = userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

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
            var objFromDb = this.unitOfWork.ApplicationUser.Get(u => u.Id == id);

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

            this.unitOfWork.ApplicationUser.Update(objFromDb);
            this.unitOfWork.Save();

            return Json(new { success = true, message = "Operation successfully." });
        }

        #endregion
    }
}