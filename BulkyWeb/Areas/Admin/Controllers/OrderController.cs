using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

         #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders = unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            
            switch (status)
    {
        case "pending":
           objOrderHeaders = objOrderHeaders.Where(x => x.PaymentStatus == SD.PaymentStatusDelayedPayment);
            break;
        case "inprocess":
            objOrderHeaders = objOrderHeaders.Where(x => x.OrderStatus == SD.StatusInProcess);
            break;
        case "completed":
            objOrderHeaders = objOrderHeaders.Where(x => x.OrderStatus == SD.StatusShipped);
            break;
        case "approved":
            objOrderHeaders = objOrderHeaders.Where(x => x.OrderStatus == SD.StatusApproved);
            break;
        default:
            break;
    }
            
            return Json(new { data = objOrderHeaders });
        }

        #endregion
    }
}