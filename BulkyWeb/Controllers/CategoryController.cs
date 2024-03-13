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

            return View();
        }

    private readonly ApplicationDbContext dbContext;

    }
}
