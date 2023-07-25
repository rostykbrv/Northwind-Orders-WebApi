using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Northwind.Services.EntityFramework.Entities;

[Table("Products")]
public class Product
{
    [Key]
    public long ProductID { get; set; }

    public string ProductName { get; set; } = null!;

    public long SupplierID { get; set; }

    public long CategoryID { get; set; }

    public string? QuantityPerUnit { get; set; }

    public double? UnitPrice { get; set; }

    public int? UnitsInStock { get; set; }

    public int? UnitsOnOrder { get; set; }

    public int? ReorderLevel { get; set; }

    public int? Discontinued { get; set; }

    [ForeignKey("SupplierID")]
    public Supplier Supplier { get; set; } = null!;

    [ForeignKey("CategoryID")]
    public Category Category { get; set; } = null!;
}
