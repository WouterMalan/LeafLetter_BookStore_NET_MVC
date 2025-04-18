using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace BookStoreWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IWebHostEnvironment webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            this.unitOfWork = unitOfWork;
            this.webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Product> productList = unitOfWork.Product.GetAll(includeProperties: "Category").OrderBy(x => x.Description).ToList();

            return View(productList);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = unitOfWork.Category.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                })
            };

            if (id == null || id == 0)
            {
                //Create
                return View(productVM);
            }
            else
            {
                //Update
                productVM.Product = unitOfWork.Product.Get(x => x.Id == id, includeProperties: "ProductImages");
                if (productVM.Product == null)
                {
                    return NotFound();
                }

                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                if (productVM.Product.Id != 0)
                {
                    unitOfWork.Product.Update(productVM.Product);
                    TempData["Success"] = "Product updated successfully";
                }
                else
                {
                    unitOfWork.Product.Add(productVM.Product);
                    TempData["Success"] = "Product created successfully";
                }
                unitOfWork.Save();

                string webRootPath = webHostEnvironment.WebRootPath;

                if (files != null)
                {
                    foreach (IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\product\product-" + productVM.Product.Id;
                        string finalPath = Path.Combine(webRootPath, productPath);

                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }

                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImage productImages = new ProductImage
                        {
                            ProductId = productVM.Product.Id,
                            ImageUrl = @"\" + productPath + @"\" + fileName
                        };

                        if (productVM.Product.ProductImages == null)
                        {
                            productVM.Product.ProductImages = new List<ProductImage>();
                        }

                        productVM.Product.ProductImages.Add(productImages);
                    }

                    unitOfWork.Product.Update(productVM.Product);
                    unitOfWork.Save();
                }

                TempData["Success"] = "Product created/updated successfully";
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = unitOfWork.Category.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                });

                return View(productVM);
            }
        }

        public IActionResult DeleteImage(int imageId)
        {
            ProductImage productImage = unitOfWork.ProductImage.Get(x => x.Id == imageId);

            if (productImage != null)
            {
                if (!string.IsNullOrEmpty(productImage.ImageUrl))
                {
                    string oldImagePath = Path.Combine(webHostEnvironment.WebRootPath, productImage.ImageUrl.TrimStart('\\'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
                }
                

                unitOfWork.ProductImage.Delete(productImage);
                unitOfWork.Save();

                TempData["Success"] = "Image deleted successfully";
            }

            return RedirectToAction("Upsert", new { id = productImage.ProductId });
        }

        // [HttpPost,
        //  ActionName("Delete")]
        //   public IActionResult DeletePost(int? id)
        // {
        //     if (id == 0)
        //     {
        //         return NotFound();
        //     }

        //     Product productFromDb = unitOfWork.Product.Get(x => x.Id == id);

        //     if (productFromDb == null)
        //     {
        //         return NotFound();
        //     }

        //     unitOfWork.Product.Delete(productFromDb);
        //     unitOfWork.Save();
        //     TempData["Success"] = "Product deleted successfully";
        //     return RedirectToAction("Index");
        // }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> allObjProduct = unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = allObjProduct });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productFromDbToBeDeleted = unitOfWork.Product.Get(x => x.Id == id);

            if (productFromDbToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

                        string productPath = @"images\product\product-" + id;
                        string finalPath = Path.Combine(webHostEnvironment.WebRootPath, productPath);

                        if (Directory.Exists(finalPath))
                        {
                            string[] filePaths = Directory.GetFiles(finalPath);
                            foreach (string files in filePaths)
                            {
                                System.IO.File.Delete(files);
                            }
                            
                            Directory.Delete(finalPath);
                        }

            unitOfWork.Product.Delete(productFromDbToBeDeleted);

            unitOfWork.Save();

            return Json(new { success = true, message = "Delete successful" });
        }

        #endregion
    }
}