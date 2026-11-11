using Core11_Otel.Data.Entities;

namespace Core11_Otel.Models;

public sealed record OrderDto(
    int OrderId,
    string? CustomerId,
    int? EmployeeId,
    DateTime? OrderDate,
    DateTime? RequiredDate,
    DateTime? ShippedDate,
    int? ShipVia,
    decimal? Freight,
    string? ShipName,
    string? ShipAddress,
    string? ShipCity,
    string? ShipRegion,
    string? ShipPostalCode,
    string? ShipCountry)
{
    public static OrderDto FromEntity(Order order) => new(
        order.OrderId,
        order.CustomerId,
        order.EmployeeId,
        order.OrderDate,
        order.RequiredDate,
        order.ShippedDate,
        order.ShipVia,
        order.Freight,
        order.ShipName,
        order.ShipAddress,
        order.ShipCity,
        order.ShipRegion,
        order.ShipPostalCode,
        order.ShipCountry);
}
