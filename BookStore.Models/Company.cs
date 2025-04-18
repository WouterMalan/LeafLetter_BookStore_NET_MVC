using System.ComponentModel.DataAnnotations;

namespace Bulky.Models
{
    public class Company
    {   
            /// <summary>
            /// Gets or sets the ID of the company.
            /// </summary>
            [Key]
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the name of the company.
            /// </summary>
            [Required]
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the street address of the company.
            /// </summary>
            [Display(Name = "Street Address")]
            public string StreetAddress { get; set; }

            /// <summary>
            /// Gets or sets the city of the company.
            /// </summary>
            public string City { get; set; }

            /// <summary>
            /// Gets or sets the state of the company.
            /// </summary>
            public string State { get; set; }

            /// <summary>
            /// Gets or sets the postal code of the company.
            /// </summary>
            [Display(Name = "Postal Code")]
            public string PostalCode { get; set; }

            /// <summary>
            /// Gets or sets the phone number of the company.
            /// </summary>
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }
        }
}