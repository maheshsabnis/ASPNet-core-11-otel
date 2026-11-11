using Core11_Otel.Data.Entities;

namespace Core11_Otel.Models;

public sealed record CreateOrderRequest(
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
    public Order ToEntity() => new()
    {
        CustomerId = CustomerId,
        EmployeeId = EmployeeId,
        OrderDate = OrderDate,
        RequiredDate = RequiredDate,
        ShippedDate = ShippedDate,
        ShipVia = ShipVia,
        Freight = Freight,
        ShipName = ShipName,
        ShipAddress = ShipAddress,
        ShipCity = ShipCity,
        ShipRegion = ShipRegion,
        ShipPostalCode = ShipPostalCode,
        ShipCountry = ShipCountry
    };
}
