using Microsoft.AspNetCore.Mvc;
using Northwind.Orders.WebApi.Models;
using Northwind.Services.Repositories;

namespace Northwind.Orders.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderRepository orderRepository;
    private readonly ILogger<OrdersController> logger;
    public OrdersController(IOrderRepository orderRepository, ILogger<OrdersController> logger)
    {
        this.orderRepository = orderRepository;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<FullOrder>> GetOrderAsync(long orderId)
    {
        try
        {
            var order = await this.orderRepository.GetOrderAsync(orderId);
            var backOrder = new FullOrder
            {
                Id = orderId,
                Customer = new Models.Customer
                {
                    Code = order.Customer.Code.Code,
                    CompanyName = order.Customer.CompanyName,
                },
                Employee = new Models.Employee
                {
                    Id = order.Employee.Id,
                    FirstName = order.Employee.FirstName,
                    LastName = order.Employee.LastName,
                    Country = order.Employee.Country,
                },
                OrderDate = order.OrderDate,
                RequiredDate = order.RequiredDate,
                ShippedDate = order.ShippedDate,
                Shipper = new Models.Shipper
                {
                    Id = order.Shipper.Id,
                    CompanyName = order.Shipper.CompanyName,
                },
                Freight = order.Freight,
                ShipName = order.ShipName,
                ShippingAddress = new Models.ShippingAddress
                {
                    Address = order.ShippingAddress.Address,
                    City = order.ShippingAddress.City,
                    Region = order.ShippingAddress.Region,
                    PostalCode = order.ShippingAddress.PostalCode,
                    Country = order.ShippingAddress.Country,
                },
                OrderDetails = order.OrderDetails.Select(od => new FullOrderDetail
                {
                    ProductId = od.Product.Id,
                    ProductName = od.Product.ProductName,
                    CategoryId = od.Product.CategoryId,
                    CategoryName = od.Product.Category,
                    SupplierId = od.Product.SupplierId,
                    SupplierCompanyName = od.Product.Supplier,
                    UnitPrice = od.UnitPrice,
                    Quantity = od.Quantity,
                    Discount = od.Discount,
                }).ToList(),
            };
            return this.Ok(backOrder);
        }
        catch (OrderNotFoundException ex)
        {
            this.logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            return this.NotFound();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            return this.StatusCode(500);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BriefOrder>>> GetOrdersAsync(int? skip, int? count)
    {
        try
        {
            if (count <= 0 || skip < 0)
            {
                throw new ArgumentOutOfRangeException($"Parameter's value {skip} or {count} cannot be below zero!");
            }

            var orders = await this.orderRepository.GetOrdersAsync(skip ?? 0, count ?? 10);

            var briefOrders = orders.Select(order => new BriefOrder
            {
                Id = order.Id,
                CustomerId = order.Customer.Code.Code,
                EmployeeId = order.Employee.Id,
                OrderDate = order.OrderDate,
                RequiredDate = order.RequiredDate,
                ShippedDate = order.ShippedDate,
                ShipperId = order.Shipper.Id,
                Freight = order.Freight,
                ShipName = order.ShipName,
                ShipAddress = order.ShippingAddress.Address,
                ShipCity = order.ShippingAddress.City,
                ShipRegion = order.ShippingAddress.Region,
                ShipPostalCode = order.ShippingAddress.PostalCode,
                ShipCountry = order.ShippingAddress.Country,
                OrderDetails = order.OrderDetails.Select(orderDetail => new BriefOrderDetail
                {
                    ProductId = orderDetail.Product.Id,
                    Quantity = orderDetail.Quantity,
                    UnitPrice = orderDetail.UnitPrice,
                    Discount = orderDetail.Discount,
                }).ToList(),
            }).ToList();

            return this.Ok(briefOrders);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            this.logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            return this.BadRequest();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            return this.StatusCode(500);
        }
    }

    [HttpPost]
    public async Task<ActionResult<AddOrder>> AddOrderAsync(BriefOrder order)
    {
        try
        {
            Order getOrder = new Order(order.Id)
            {
                Customer = new Northwind.Services.Repositories.Customer(new CustomerCode(order.CustomerId)),
                Employee = new Northwind.Services.Repositories.Employee(order.EmployeeId),
                OrderDate = order.OrderDate,
                RequiredDate = order.RequiredDate,
                ShippedDate = order.ShippedDate,
                Shipper = new Northwind.Services.Repositories.Shipper(order.ShipperId),
                Freight = order.Freight,
                ShipName = order.ShipName,
                ShippingAddress = new Northwind.Services.Repositories.ShippingAddress(order.ShipAddress, order.ShipCity, order.ShipRegion, order.ShipPostalCode, order.ShipCountry),
            };
            foreach (var briefOrderDetail in order.OrderDetails)
            {
                var orderDetail = new OrderDetail(getOrder)
                {
                    Product = new Product(briefOrderDetail.ProductId),
                    UnitPrice = briefOrderDetail.UnitPrice,
                    Quantity = briefOrderDetail.Quantity,
                    Discount = briefOrderDetail.Discount,
                };
                getOrder.OrderDetails.Add(orderDetail);
            }

            var orderId = await this.orderRepository.AddOrderAsync(getOrder);
            var addOrder = new AddOrder
            {
                OrderId = orderId,
            };

            return this.Ok(addOrder);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            return this.StatusCode(500);
        }
    }

    [HttpDelete]
    public async Task<ActionResult> RemoveOrderAsync(long orderId)
    {
        try
        {
            await this.orderRepository.RemoveOrderAsync(orderId);
            return this.NoContent();
        }
        catch (OrderNotFoundException ex)
        {
            this.logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            return this.NotFound();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            return this.StatusCode(500);
        }
    }

    [HttpPut]
    public async Task<ActionResult> UpdateOrderAsync(long orderId, BriefOrder order)
    {
        try
        {
            Order getOrder = new Order(orderId)
            {
                Customer = new Northwind.Services.Repositories.Customer(new CustomerCode(order.CustomerId)),
                Employee = new Northwind.Services.Repositories.Employee(order.EmployeeId),
                OrderDate = order.OrderDate,
                RequiredDate = order.RequiredDate,
                ShippedDate = order.ShippedDate,
                Shipper = new Northwind.Services.Repositories.Shipper(order.ShipperId),
                Freight = order.Freight,
                ShipName = order.ShipName,
                ShippingAddress = new Northwind.Services.Repositories.ShippingAddress(order.ShipAddress, order.ShipCity, order.ShipRegion, order.ShipPostalCode, order.ShipCountry),
            };
            foreach (var briefOrderDetail in order.OrderDetails)
            {
                var orderDetail = new OrderDetail(getOrder)
                {
                    Product = new Product(briefOrderDetail.ProductId),
                    UnitPrice = briefOrderDetail.UnitPrice,
                    Quantity = briefOrderDetail.Quantity,
                    Discount = briefOrderDetail.Discount,
                };
                getOrder.OrderDetails.Add(orderDetail);
            }

            await this.orderRepository.UpdateOrderAsync(getOrder);
            return this.NoContent();
        }
        catch (OrderNotFoundException ex)
        {
            this.logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            return this.NotFound();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
            return this.StatusCode(500);
        }
    }
}
