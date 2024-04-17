using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private ApplicationDbContext dbContext;

        public OrderHeaderRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            this.dbContext = dbContext;
        }

        public void Update(OrderHeader obj)
        {
            dbContext.OrderHeaders.Update(obj);

        }

        public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
        {
            var orderFromDb = dbContext.OrderHeaders.FirstOrDefault(o => o.Id == id);

            if(orderFromDb != null)
            {
                orderFromDb.OrderStatus = orderStatus;

                if (!string.IsNullOrWhiteSpace(paymentStatus))
                {
                    orderFromDb.PaymentStatus = paymentStatus;
                }
            }
        }

        public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
        {
            var orderFromDb = dbContext.OrderHeaders.FirstOrDefault(o => o.Id == id);

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                orderFromDb.SessionId = sessionId;
            }

            if (!string.IsNullOrWhiteSpace(paymentIntentId))
            {
                orderFromDb.PaymentIntentId = paymentIntentId;
                orderFromDb.PaymentDate = DateTime.Now;
            }
        }
    }
}