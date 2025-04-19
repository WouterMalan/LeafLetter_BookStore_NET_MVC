using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace BookStoreWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty] public OrderVM OrderVm { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            OrderVm = new OrderVM()
            {
                OrderHeader = _unitOfWork
                    .OrderHeader
                    .Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetails =
                    _unitOfWork
                        .OrderDetail
                        .GetAll(o => o.OrderHeaderId == orderId, includeProperties: "Product")
            };

            return View(OrderVm);
        }

        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
        public IActionResult UpdateOrderDetail()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Invalid order details";
                    return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
                }

                var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id)
                                        ?? throw new InvalidOperationException(
                                            $"Order {OrderVm.OrderHeader.Id} not found");

                UpdateOrderBasicDetails(orderHeaderFromDb);

                UpdateShippingDetails(orderHeaderFromDb);

                _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
                _unitOfWork.Save();

                TempData["Success"] = "Order details updated successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to update order details";
            }

            return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVm.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();

            TempData["Success"] = "Order is in process";

            return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVm.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();

            _unitOfWork.OrderHeader.UpdateStatus(OrderVm.OrderHeader.Id, SD.StatusShipped);
            
            _unitOfWork.Save();

            TempData["Success"] = "Order is shipped";

            return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);

            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                // Stripe refund logic
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled);
            }

            _unitOfWork.Save();
            TempData["Success"] = "Order has been cancelled";

            return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
        }

        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            OrderVm.OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id,
                includeProperties: "ApplicationUser");
            OrderVm.OrderDetails = _unitOfWork.OrderDetail.GetAll(o => o.OrderHeaderId == OrderVm.OrderHeader.Id,
                includeProperties: "Product");

            //regular customer and need to capture payment (stripe logic)
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";

            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"Admin/Order/PaymentConfirmation?orderHeaderId={OrderVm.OrderHeader.Id}",
                CancelUrl = domain + $"Admin/Order/details?orderHeader={OrderVm.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderVm.OrderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100), // 2.50 -> 250
                        Currency = "zar",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title,
                        },
                    },
                    Quantity = item.Count,
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            _unitOfWork.OrderHeader.UpdateStripePaymentId(OrderVm.OrderHeader.Id, session.Id, session.PaymentIntentId);
           
            _unitOfWork.Save();

            Response.Headers.Append("Location", session.Url);

            return StatusCode(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unitOfWork
                .OrderHeader
                .Get(u => u.Id == orderHeaderId);

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //order by a company
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork
                        .OrderHeader
                        .UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                   
                    _unitOfWork.
                        OrderHeader
                        .UpdateStatus(orderHeaderId, orderHeader.OrderStatus,
                        SD.PaymentStatusApproved);
                   
                    _unitOfWork.Save();
                }
            }

            return View(orderHeaderId);
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
            IEnumerable<OrderHeader> objOrderHeaders;

            if (User.IsInRole(SD.RoleAdmin) || User.IsInRole(SD.RoleEmployee))
            {
                objOrderHeaders = _unitOfWork
                    .OrderHeader
                    .GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                
                if (userId == null)
                {
                    return Json(new { data = new List<OrderHeader>() });
                }

                objOrderHeaders = _unitOfWork
                    .OrderHeader
                    .GetAll(u => u.ApplicationUserId == userId.Value,
                    includeProperties: "ApplicationUser");
            }

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

        private void UpdateOrderBasicDetails(OrderHeader orderHeader)
        {
            orderHeader.Name = OrderVm.OrderHeader.Name?.Trim();
            orderHeader.PhoneNumber = OrderVm.OrderHeader.PhoneNumber?.Trim();
            orderHeader.StreetAddress = OrderVm.OrderHeader.StreetAddress?.Trim();
            orderHeader.City = OrderVm.OrderHeader.City?.Trim();
            orderHeader.State = OrderVm.OrderHeader.State?.Trim();
            orderHeader.PostalAddress = OrderVm.OrderHeader.PostalAddress?.Trim();
        }

        private void UpdateShippingDetails(OrderHeader orderHeader)
        {
            if (!string.IsNullOrWhiteSpace(OrderVm.OrderHeader.Carrier))
            {
                orderHeader.Carrier = OrderVm.OrderHeader.Carrier.Trim();
            }

            if (!string.IsNullOrWhiteSpace(OrderVm.OrderHeader.TrackingNumber))
            {
                orderHeader.TrackingNumber = OrderVm.OrderHeader.TrackingNumber.Trim();
            }
        }
    }
}