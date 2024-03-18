using BulkyWeb.Data;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Controllers
{
    public class CategoryController : Controller
    {

    public CategoryController(ApplicationDbContext db)
    {
        dbContext = db;
    }

        public IActionResult Index()
        {
            List<Category> objCategoryList = dbContext.Categories.ToList();

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
            }

            if (ModelState.IsValid)
            {
                dbContext.Categories.Add(obj);
                dbContext.SaveChanges();
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

            Category? categoryFromDb = dbContext.Categories.Find(id);

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
                dbContext.Categories.Update(obj);
                dbContext.SaveChanges();
                return RedirectToAction("Index");
            }
            
            return View(obj);
          
        }

    private readonly ApplicationDbContext dbContext;

    }
}
