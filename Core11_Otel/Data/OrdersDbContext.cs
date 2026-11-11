using Core11_Otel.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Core11_Otel.Data;

public partial class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public virtual DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(order => order.OrderId);
            entity.Property(order => order.OrderId)
                .HasColumnName("OrderID")
                .ValueGeneratedOnAdd();
            entity.Property(order => order.CustomerId).HasColumnName("CustomerID");
            entity.Property(order => order.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(order => order.OrderDate).HasColumnName("OrderDate");
            entity.Property(order => order.RequiredDate).HasColumnName("RequiredDate");
            entity.Property(order => order.ShippedDate).HasColumnName("ShippedDate");
            entity.Property(order => order.ShipVia).HasColumnName("ShipVia");
            entity.Property(order => order.Freight)
                .HasColumnName("Freight")
                .HasColumnType("money");
            entity.Property(order => order.ShipName).HasColumnName("ShipName");
            entity.Property(order => order.ShipAddress).HasColumnName("ShipAddress");
            entity.Property(order => order.ShipCity).HasColumnName("ShipCity");
            entity.Property(order => order.ShipRegion).HasColumnName("ShipRegion");
            entity.Property(order => order.ShipPostalCode).HasColumnName("ShipPostalCode");
            entity.Property(order => order.ShipCountry).HasColumnName("ShipCountry");
        });
    }
}
