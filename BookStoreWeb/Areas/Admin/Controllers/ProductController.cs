using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookStoreWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            this._webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            List<Product> productList =
                _unitOfWork
                    .Product
                    .GetAll(includeProperties: "Category")
                    .OrderBy(x => x.Description)
                    .ToList();

            return View(productList);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        public IActionResult Upsert(int? id)
        {
            ProductVM productVm = new ProductVM()
            {
                Product = new Product(),
                CategoryList = _unitOfWork
                    .Category
                    .GetAll()
                    .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                })
            };

            if (id == null || id == 0)
            {
                //Create
                return View(productVm);
            }
            else
            {
                //Update
                productVm.Product = _unitOfWork
                    .Product
                    .Get(x => x.Id == id, includeProperties: "ProductImages");
                
                if (productVm.Product == null)
                {
                    return NotFound();
                }

                return View(productVm);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVm, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                if (productVm.Product.Id != 0)
                {
                    _unitOfWork.Product.Update(productVm.Product);
                    TempData["Success"] = "Product updated successfully";
                }
                else
                {
                    _unitOfWork.Product.Add(productVm.Product);
                    TempData["Success"] = "Product created successfully";
                }

                _unitOfWork.Save();

                // Save images to the root folder
                string webRootPath = _webHostEnvironment.WebRootPath;

                if (files != null)
                {
                    foreach (IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\product\product-" + productVm.Product.Id;
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
                            ProductId = productVm.Product.Id,
                            ImageUrl = @"\" + productPath + @"\" + fileName
                        };

                        if (productVm.Product.ProductImages == null)
                        {
                            productVm.Product.ProductImages = new List<ProductImage>();
                        }

                        productVm.Product.ProductImages.Add(productImages);
                    }

                    _unitOfWork.Product.Update(productVm.Product);
                    _unitOfWork.Save();
                }

                TempData["Success"] = "Product created/updated successfully";
                return RedirectToAction("Index");
            }
            else
            {
                productVm.CategoryList = _unitOfWork.Category.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                });

                return View(productVm);
            }
        }

        public IActionResult DeleteImage(int imageId)
        {
            ProductImage productImage = _unitOfWork.ProductImage.Get(x => x.Id == imageId);

            if (productImage != null)
            {
                if (!string.IsNullOrEmpty(productImage.ImageUrl))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        productImage.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }


                _unitOfWork.ProductImage.Delete(productImage);
                _unitOfWork.Save();

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

        //     Product productFromDb = _unitOfWork.Product.Get(x => x.Id == id);

        //     if (productFromDb == null)
        //     {
        //         return NotFound();
        //     }

        //     _unitOfWork.Product.Delete(productFromDb);
        //     _unitOfWork.Save();
        //     TempData["Success"] = "Product deleted successfully";
        //     return RedirectToAction("Index");
        // }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> allObjProduct = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = allObjProduct });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productFromDbToBeDeleted = _unitOfWork.Product.Get(x => x.Id == id);

            if (productFromDbToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            string productPath = @"images\product\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if (Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach (string files in filePaths)
                {
                    System.IO.File.Delete(files);
                }

                Directory.Delete(finalPath);
            }

            _unitOfWork.Product.Delete(productFromDbToBeDeleted);

            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete successful" });
        }

        #endregion
    }
}