using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Northwind.Services.EntityFramework.Entities;

[Table("OrderDetails")]
[PrimaryKey(nameof(OrderID), nameof(ProductID))]
public class OrderDetail
{
    public long OrderID { get; set; }

    public long ProductID { get; set; }

    public double UnitPrice { get; set; }

    public long Quantity { get; set; }

    public double Discount { get; set; }

    public Product Product { get; set; } = null!;
}
