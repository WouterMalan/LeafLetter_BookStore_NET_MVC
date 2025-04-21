using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookStoreWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.RoleAdmin)]
    public class UserController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger, IUnitOfWork unitOfWork,
            RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string userId)
        {
            RoleManagementVM roleManagementVm = new RoleManagementVM()
            {
                ApplicationUser =
                    _unitOfWork.ApplicationUser.Get(u => u.Id == userId, includeProperties: "Company"),
                RoleList = _roleManager.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name  
                }),
                CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

            roleManagementVm.ApplicationUser.Role = _userManager
                .GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == userId))
                .GetAwaiter()
                .GetResult()
                .FirstOrDefault();
            
            return View(roleManagementVm);
        }
        
        [HttpPost]
        public IActionResult RoleManagement(RoleManagementVM roleVM)
        {
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser
                .Get(u => u.Id == roleVM.ApplicationUser.Id, includeProperties: "Company");

            if (applicationUser == null)
            {
                return NotFound();
            }

            // Get the current role of the user
            string oldRole = _userManager
                .GetRolesAsync(applicationUser)
                .GetAwaiter()
                .GetResult()
                .FirstOrDefault();

            if (roleVM.ApplicationUser.Role != oldRole)
            {
                if (roleVM.ApplicationUser.Role == SD.RoleCompany)
                {
                    applicationUser.CompanyId = roleVM.ApplicationUser.CompanyId;
                }
                else if (oldRole == SD.RoleCompany)
                {
                    applicationUser.CompanyId = null;
                }

                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();

                if (!string.IsNullOrEmpty(oldRole))
                {
                    _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                }

                _userManager.AddToRoleAsync(applicationUser, roleVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }
            else
            {
                if (oldRole == SD.RoleCompany && roleVM.ApplicationUser.CompanyId != applicationUser.CompanyId)
                {
                    applicationUser.CompanyId = roleVM.ApplicationUser.CompanyId;
                    _unitOfWork.ApplicationUser.Update(applicationUser);
                    _unitOfWork.Save();
                }
            }

            return RedirectToAction("Index");
        }


        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                List<ApplicationUser> userList =
                    _unitOfWork
                        .ApplicationUser.GetAll(includeProperties: "Company")
                        .ToList();

                foreach (var user in userList)
                {
                    user.Role = _userManager
                        .GetRolesAsync(user)
                        .GetAwaiter()
                        .GetResult()
                        .FirstOrDefault();

                    // if the user does not have a company, create a new company with an empty name
                    if (user.CompanyId == null)
                    {
                        user.Company = new Company() { Name = "" };
                    }
                }

                return Json(new { data = userList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error while loading users" });
            }
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var objFromDb =
                _unitOfWork
                    .ApplicationUser
                    .Get(u => u.Id == id);

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
                objFromDb.LockoutEnd = DateTime.Now.AddDays(7);
            }

            _unitOfWork.ApplicationUser.Update(objFromDb);

            _unitOfWork.Save();

            return Json(new { success = true, message = "Operation successfully." });
        }

        #endregion
    }
}