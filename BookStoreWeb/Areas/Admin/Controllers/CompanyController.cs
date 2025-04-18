using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookStoreWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
         private readonly IUnitOfWork unitOfWork;

        private readonly ILogger<CompanyController> _logger;

        public CompanyController(ILogger<CompanyController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            this.unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Company> companyList = unitOfWork.Company.GetAll().OrderBy(x => x.Name).ToList();

            return View(companyList);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

         public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                //Create
                return View(new Company());
            }
            else
            {
                //Update
                Company company = unitOfWork.Company.Get(x => x.Id == id);
                if (company == null)
                {
                    return NotFound();
                }

                return View(company);
            }
        }

        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if(company.Id ==0)
                {
                    unitOfWork.Company.Add(company);
                }
                else
                {
                    unitOfWork.Company.Update(company);
                }

                unitOfWork.Save();
                TempData["Success"] = "Company created successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(company);
        }


        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> companyList = unitOfWork.Company.GetAll().ToList();

            return Json(new { data = companyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var companyToBeDeleted = unitOfWork.Company.Get(x => x.Id == id);

            if (companyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting." });
            }

            unitOfWork.Company.Delete(companyToBeDeleted);
            unitOfWork.Save();

            return Json(new { success = true, message = "Company deleted successfully." });
        }

        #endregion
    }
}