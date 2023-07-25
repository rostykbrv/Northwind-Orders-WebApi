using Microsoft.EntityFrameworkCore;
using Northwind.Services.EntityFramework.Entities;
using Northwind.Services.Repositories;
using RepositoryCustomer = Northwind.Services.Repositories.Customer;
using RepositoryEmployee = Northwind.Services.Repositories.Employee;
using RepositoryOrder = Northwind.Services.Repositories.Order;
using RepositoryOrderDetail = Northwind.Services.Repositories.OrderDetail;
using RepositoryProduct = Northwind.Services.Repositories.Product;
using RepositoryShipper = Northwind.Services.Repositories.Shipper;

namespace Northwind.Services.EntityFramework.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly NorthwindContext context;

    public OrderRepository(NorthwindContext context)
    {
        this.context = context;
    }

    public async Task<RepositoryOrder> GetOrderAsync(long orderId)
    {
        var order = await this.context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Shipper)
            .Include(o => o.Employee)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .ThenInclude(p => p.Supplier)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(o => o.OrderID == orderId) ?? throw new OrderNotFoundException("Order not found!");
        var repositoryOrder = new RepositoryOrder(order.OrderID)
        {
            OrderDate = order.OrderDate,
            RequiredDate = order.RequiredDate,
            ShippedDate = order.ShippedDate,
            Freight = order.Freight,
            ShipName = order.ShipName,
            Shipper = new RepositoryShipper(order.ShipVia)
            {
                CompanyName = order.Shipper.CompanyName,
            },
            Customer = new RepositoryCustomer(new CustomerCode(order.CustomerID))
            {
                CompanyName = order.Customer.CompanyName,
            },
            Employee = new RepositoryEmployee(order.EmployeeID)
            {
                LastName = order.Employee.LastName,
                FirstName = order.Employee.FirstName,
                Country = order.Employee.Country,
            },
            ShippingAddress = new ShippingAddress(order.ShipAddress, order.ShipCity, order.ShipRegion, order.ShipPostalCode, order.ShipCountry),
        };

        foreach (var orderDetail in order.OrderDetails)
        {
            var repositoryOrderDetail = new RepositoryOrderDetail(repositoryOrder)
            {
                Product = new RepositoryProduct(orderDetail.ProductID)
                {
                    ProductName = orderDetail.Product.ProductName,
                    Category = orderDetail.Product.Category.CategoryName,
                    CategoryId = orderDetail.Product.CategoryID,
                    SupplierId = orderDetail.Product.SupplierID,
                    Supplier = orderDetail.Product.Supplier.CompanyName,
                },
                Quantity = orderDetail.Quantity,
                UnitPrice = orderDetail.UnitPrice,
                Discount = orderDetail.Discount,
            };

            repositoryOrder.OrderDetails.Add(repositoryOrderDetail);
        }

        return repositoryOrder;
    }

    public async Task<IList<RepositoryOrder>> GetOrdersAsync(int skip, int count)
    {
        if (count <= 0 || skip < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter's value {skip} or {count} cannot be below zero!");
        }

        var orders = await this.context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Shipper)
            .Include(o => o.Employee)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .ThenInclude(p => p.Supplier)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .ThenInclude(p => p.Category)
            .OrderBy(o => o.OrderID)
            .Skip(skip)
            .Take(count)
            .ToListAsync();
        var ordersList = orders.Select(order => new RepositoryOrder(order.OrderID)
        {
            OrderDate = order.OrderDate,
            RequiredDate = order.RequiredDate,
            ShippedDate = order.ShippedDate,
            Freight = order.Freight,
            ShipName = order.ShipName,
            Shipper = new RepositoryShipper(order.ShipVia)
            {
                CompanyName = order.Shipper.CompanyName,
            },
            Customer = new RepositoryCustomer(new CustomerCode(order.CustomerID))
            {
                CompanyName = order.Customer.CompanyName,
            },
            Employee = new RepositoryEmployee(order.EmployeeID)
            {
                LastName = order.Employee.LastName,
                FirstName = order.Employee.FirstName,
                Country = order.Employee.Country,
            },
            ShippingAddress = new ShippingAddress(order.ShipAddress, order.ShipCity, order.ShipRegion, order.ShipPostalCode, order.ShipCountry),
        }).ToList();

        return ordersList;
    }

    public async Task<long> AddOrderAsync(RepositoryOrder order)
    {
        try
        {
            var existingCustomer = await this.context.Customers.FirstAsync(c => c.CustomerID == order.Customer.Code.Code);
            var existingEmployee = await this.context.Employees.FirstAsync(e => e.EmployeeID == order.Employee.Id);
            var existingShipper = await this.context.Shippers.FirstAsync(s => s.ShipperID == order.Shipper.Id);
            var newOrder = new Entities.Order
            {
                CustomerID = existingCustomer.CustomerID,
                Customer = existingCustomer,
                EmployeeID = existingEmployee.EmployeeID,
                Employee = existingEmployee,
                ShipVia = existingShipper.ShipperID,
                Shipper = existingShipper,
                OrderDate = order.OrderDate,
                RequiredDate = order.RequiredDate,
                ShippedDate = order.ShippedDate,
                Freight = order.Freight,
                ShipName = order.ShipName,
                ShipAddress = order.ShippingAddress.Address,
                ShipCity = order.ShippingAddress.City,
                ShipRegion = order.ShippingAddress.Region,
                ShipPostalCode = order.ShippingAddress.PostalCode,
                ShipCountry = order.ShippingAddress.Country,
                OrderDetails = new List<Entities.OrderDetail>(),
            };

            foreach (var orderDetail in order.OrderDetails)
            {
                var existingProduct = await this.context.Products.FirstAsync(p => p.ProductID == orderDetail.Product.Id);
                var newOrderDetails = new Entities.OrderDetail
                {
                    OrderID = newOrder.OrderID,
                    ProductID = existingProduct.ProductID,
                    Product = existingProduct,
                    Quantity = orderDetail.Quantity,
                    Discount = orderDetail.Discount,
                    UnitPrice = orderDetail.UnitPrice,
                };

                newOrder.OrderDetails.Add(newOrderDetails);
            }

            _ = await this.context.Orders.AddAsync(newOrder);
            _ = await this.context.SaveChangesAsync();
            return newOrder.OrderID;
        }
        catch (Exception ex)
        {
            throw new RepositoryException(ex.Message);
        }
    }

    public async Task RemoveOrderAsync(long orderId)
    {
        var order = await this.context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Shipper)
            .Include(o => o.Employee)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .ThenInclude(p => p.Supplier)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(o => o.OrderID == orderId) ?? throw new OrderNotFoundException("Order not found!");
        this.context.OrderDetails.RemoveRange(order.OrderDetails);

        _ = this.context.Orders.Remove(order);

        _ = await this.context.SaveChangesAsync();
    }

    public async Task UpdateOrderAsync(RepositoryOrder order)
    {
        var orderBefore = await this.context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Shipper)
            .Include(o => o.Employee)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .ThenInclude(p => p.Supplier)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(o => o.OrderID == order.Id) ?? throw new OrderNotFoundException("Order not found!");
        orderBefore.CustomerID = order.Customer.Code.Code;
        orderBefore.Customer.CompanyName = order.Customer.CompanyName;
        orderBefore.EmployeeID = order.Employee.Id;
        orderBefore.Employee.LastName = order.Employee.LastName;
        orderBefore.Employee.FirstName = order.Employee.FirstName;
        orderBefore.Employee.Country = order.Employee.Country;
        orderBefore.ShipVia = order.Shipper.Id;
        orderBefore.Shipper.CompanyName = order.Shipper.CompanyName;
        orderBefore.OrderDate = order.OrderDate;
        orderBefore.RequiredDate = order.RequiredDate;
        orderBefore.ShippedDate = order.ShippedDate;
        orderBefore.Freight = order.Freight;
        orderBefore.ShipName = order.ShipName;
        orderBefore.ShipAddress = order.ShippingAddress.Address;
        orderBefore.ShipCity = order.ShippingAddress.City;
        orderBefore.ShipRegion = order.ShippingAddress.Region;
        orderBefore.ShipPostalCode = order.ShippingAddress.PostalCode;
        orderBefore.ShipCountry = order.ShippingAddress.Country;

        if (order.OrderDetails != null)
        {
            this.context.OrderDetails.RemoveRange(orderBefore.OrderDetails);
            foreach (var orderDetail in order.OrderDetails)
            {
                var newOrderDetails = new Entities.OrderDetail
                {
                    OrderID = order.Id,
                    ProductID = orderDetail.Product.Id,
                    Quantity = orderDetail.Quantity,
                    Discount = orderDetail.Discount,
                    UnitPrice = orderDetail.UnitPrice,
                };

                orderBefore.OrderDetails.Add(newOrderDetails);
            }
        }

        _ = await this.context.SaveChangesAsync();
    }
}
