using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Northwind.Services.EntityFramework.Entities;

public class Order
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long OrderID { get; set; }

    public string CustomerID { get; set; } = null!;

    public long EmployeeID { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime RequiredDate { get; set; }

    public DateTime? ShippedDate { get; set; }

    public long ShipVia { get; set; }

    public double Freight { get; set; }

    public string ShipName { get; set; } = null!;

    public string ShipAddress { get; set; } = null!;

    public string ShipCity { get; set; } = null!;

    [AllowNull]
    public string? ShipRegion { get; set; }

    public string ShipPostalCode { get; set; } = null!;

    public string ShipCountry { get; set; } = null!;

    [ForeignKey("CustomerID")]
    public Customer Customer { get; set; } = null!;

    [ForeignKey("EmployeeID")]
    public Employee Employee { get; set; } = null!;

    [ForeignKey("ShipVia")]
    public Shipper Shipper { get; set; } = null!;

    public ICollection<OrderDetail> OrderDetails { get; set; } = null!;
}
