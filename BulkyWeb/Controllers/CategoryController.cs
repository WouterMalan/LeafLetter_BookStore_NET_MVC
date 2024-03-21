using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Controllers
{
    public class CategoryController : Controller
    {

    public CategoryController(ICategoryRepository categoryRepository)
    {
        this.categoryRepository = categoryRepository;
    }

        public IActionResult Index()
        {
            List<Category> objCategoryList = categoryRepository.GetAll().OrderBy(x => x.DisplayOrder).ToList();

            return View(objCategoryList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Name", "Category Name and Display Order should not be the same");
                TempData["Error"] = "Category Name and Display Order should not be the same";
            }

            if (ModelState.IsValid)
            {
                categoryRepository.Add(obj);
                categoryRepository.Save();
                TempData["Success"] = "Category created successfully";
                return RedirectToAction("Index");
            }
            
            return View(obj);
          
        }

        public IActionResult Edit(int? id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            Category? categoryFromDb = categoryRepository.Get(x => x.Id == id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }

            return View(categoryFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Category obj)
        {
            if (ModelState.IsValid)
            {
                categoryRepository.Update(obj);
                categoryRepository.Save();
                TempData["Success"] = "Category updated successfully";
                return RedirectToAction("Index");
            }
            
            return View(obj);
          
        }

        public IActionResult Delete(int? id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            Category? categoryFromDb = categoryRepository.Get(x => x.Id == id);

            if (categoryFromDb == null)
            {
                return NotFound();
            }

            return View(categoryFromDb);
        }

        [HttpPost,
        ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            Category? category = categoryRepository.Get(x => x.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            categoryRepository.Remove(category);
            categoryRepository.Save();
            TempData["Success"] = "Category deleted successfully";
            return RedirectToAction("Index");
        }

    private readonly ICategoryRepository categoryRepository;

    }
}
