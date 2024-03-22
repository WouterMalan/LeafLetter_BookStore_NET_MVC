using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        public ProductController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Product> productList = unitOfWork.Product.GetAll().OrderBy(x => x.Description).ToList();
            IEnumerable<SelectListItem> categoryList = unitOfWork.Category.GetAll().Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });
            
            return View(productList);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                unitOfWork.Product.Add(product);
                unitOfWork.Save();
                TempData["Success"] = "Product created successfully";
                return RedirectToAction("Index");
            }

            return View(product);
        }

        public IActionResult Edit(int? id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            Product productFromDb = unitOfWork.Product.Get(x => x.Id == id);

            if (productFromDb == null)
            {
                return NotFound();
            }

            return View(productFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                unitOfWork.Product.Update(product);
                unitOfWork.Save();
                TempData["Success"] = "Product updated successfully";
                return RedirectToAction("Index");
            }

            return View(product);
        }

        public IActionResult Delete(int? id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            Product productFromDb = unitOfWork.Product.Get(x => x.Id == id);

            if (productFromDb == null)
            {
                return NotFound();
            }

            return View(productFromDb);
        }

        [HttpPost,
         ActionName("Delete")]
          public IActionResult DeletePost(int? id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            Product productFromDb = unitOfWork.Product.Get(x => x.Id == id);

            if (productFromDb == null)
            {
                return NotFound();
            }

            unitOfWork.Product.Remove(productFromDb);
            unitOfWork.Save();
            TempData["Success"] = "Product deleted successfully";
            return RedirectToAction("Index");
        }
    }
}