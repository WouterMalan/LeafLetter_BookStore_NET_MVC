
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
         private readonly ApplicationDbContext dbContext;

        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger,ApplicationDbContext dbContext)
        {
            _logger = logger;
            this.dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
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

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
         

            return Json(new { success = true, message = "Company deleted successfully." });
        }

        #endregion
    }
}