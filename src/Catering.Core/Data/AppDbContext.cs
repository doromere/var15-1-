using Catering.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Catering.Core.Data;

/// <summary>
/// Контекст данных Entity Framework Core (Pomelo.EntityFrameworkCore.MySql).
/// 7 таблиц по ТЗ, 3НФ, явные внешние ключи (RESTRICT/CASCADE).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<BanquetDish> BanquetDishes => Set<BanquetDish>();
    public DbSet<CateringClient> CateringClients => Set<CateringClient>();
    public DbSet<CateringContract> CateringContracts => Set<CateringContract>();
    public DbSet<ContractMenuItem> ContractMenuItems => Set<ContractMenuItem>();

    /// <summary>Создает контекст, настроенный на локальную MariaDB/MySQL (phpMyAdmin: http://localhost/phpmyadmin/).</summary>
    public static AppDbContext Create(
        string connectionString = "Server=localhost;Port=3306;Database=catering_db;User Id=root;Password=;CharSet=utf8mb4;")
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(10, 11, 0))) // MariaDB 10.11 / MySQL 8.x
            .Options;
        return new AppDbContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Login).HasColumnName("login").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(64).IsRequired();
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Login).IsUnique();

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.ToTable("event_types");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TypeTitle).HasColumnName("type_title").HasMaxLength(150).IsRequired();
            entity.Property(e => e.ServiceMultiplier).HasColumnName("service_multiplier").HasPrecision(5, 2);
            entity.Property(e => e.MinGuests).HasColumnName("min_guests");
        });

        modelBuilder.Entity<BanquetDish>(entity =>
        {
            entity.ToTable("banquet_dishes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DishTitle).HasColumnName("dish_title").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
            entity.Property(e => e.PortionWeightG).HasColumnName("portion_weight_g");
            entity.Property(e => e.CostPerPerson).HasColumnName("cost_per_person").HasPrecision(10, 2);
        });

        modelBuilder.Entity<CateringClient>(entity =>
        {
            entity.ToTable("catering_clients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientName).HasColumnName("client_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(32).IsRequired();
            entity.Property(e => e.EventLocation).HasColumnName("event_location").HasMaxLength(500).IsRequired();
            entity.Property(e => e.IsVip).HasColumnName("is_vip");
        });

        modelBuilder.Entity<CateringContract>(entity =>
        {
            entity.ToTable("catering_contracts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContractNumber).HasColumnName("contract_number").HasMaxLength(40).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CateringClientId).HasColumnName("catering_client_id");
            entity.Property(e => e.EventTypeId).HasColumnName("event_type_id");
            entity.Property(e => e.EventDatetime).HasColumnName("event_datetime");
            entity.Property(e => e.GuestsCount).HasColumnName("guests_count");
            entity.Property(e => e.TotalBudget).HasColumnName("total_budget").HasPrecision(12, 2);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Property(e => e.CancelReason).HasColumnName("cancel_reason").HasMaxLength(1000);
            entity.HasIndex(e => e.ContractNumber).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Contracts)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CateringClient)
                .WithMany(c => c.Contracts)
                .HasForeignKey(e => e.CateringClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.EventType)
                .WithMany(t => t.Contracts)
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContractMenuItem>(entity =>
        {
            entity.ToTable("contract_menu_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.BanquetDishId).HasColumnName("banquet_dish_id");
            entity.Property(e => e.ServingsCount).HasColumnName("servings_count");
            entity.Property(e => e.EquipmentRentFee).HasColumnName("equipment_rent_fee").HasPrecision(10, 2);
            entity.Property(e => e.LineCost).HasColumnName("line_cost").HasPrecision(12, 2);

            entity.HasOne(e => e.Contract)
                .WithMany(c => c.MenuItems)
                .HasForeignKey(e => e.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.BanquetDish)
                .WithMany(d => d.MenuItems)
                .HasForeignKey(e => e.BanquetDishId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
