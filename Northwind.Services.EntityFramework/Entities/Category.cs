using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Northwind.Services.EntityFramework.Entities;

[Table("Categories")]
public class Category
{
    [Key]
    public long CategoryID { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }
}
