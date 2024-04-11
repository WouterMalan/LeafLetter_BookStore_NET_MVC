using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bulky.Models
{
    public class ShoppingCart
    {
            /// <summary>
            /// Gets or sets the ID of the shopping cart item.
            /// </summary>
            [Key]
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the ID of the product associated with the shopping cart item.
            /// </summary>
            public int ProductId { get; set; }

            /// <summary>
            /// Gets or sets the product associated with the shopping cart item.
            /// </summary>
            [ForeignKey("ProductId")]
            [ValidateNever]
            public Product Product { get; set; }

            /// <summary>
            /// Gets or sets the count of the product in the shopping cart item.
            /// </summary>
            [Range(1, 1000, ErrorMessage = "Please enter a value between 1 and 1000")]
            public int Count { get; set; }

            /// <summary>
            /// Gets or sets the ID of the application user associated with the shopping cart item.
            /// </summary>
            public string ApplicationUserId { get; set; }

            /// <summary>
            /// Gets or sets the application user associated with the shopping cart item.
            /// </summary>
            [ForeignKey("ApplicationUserId")]
            [ValidateNever]
            public ApplicationUser ApplicationUser { get; set; }
    }
}