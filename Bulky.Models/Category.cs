using System.ComponentModel.DataAnnotations;

namespace Bulky.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        [Display(Name = "Category Name")]
        public string Name { get; set; }   

        [Display(Name = "Display Order")]
        [Range(1, int.MaxValue, ErrorMessage = "Display Order for category must be greater than 0")]
                                                     public int DisplayOrder { get; set; }
    }
}