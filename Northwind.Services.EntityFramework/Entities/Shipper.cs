using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Northwind.Services.EntityFramework.Entities;

[Table("Shippers")]
public class Shipper
{
    [Key]
    public long ShipperID { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? Phone { get; set; }
}
