using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KMG.Repository.Models;

public partial class SwpkoiFarmShopContext : DbContext
{
    public SwpkoiFarmShopContext()
    {
    }

    public SwpkoiFarmShopContext(DbContextOptions<SwpkoiFarmShopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Consignment> Consignments { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Fish> Fishes { get; set; }

    public virtual DbSet<Koi> Kois { get; set; }

    public virtual DbSet<KoiType> KoiTypes { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderFish> OrderFishes { get; set; }

    public virtual DbSet<OrderKoi> OrderKois { get; set; }

    public virtual DbSet<Point> Points { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PurchaseHistory> PurchaseHistories { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public static string GetConnectionString(string connectionStringName)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        string connectionString = config.GetConnectionString(connectionStringName);
        return connectionString;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(GetConnectionString("DefaultConnection"));

    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseSqlServer("Data Source=MSI\\SQLEXPRESS;Initial Catalog=SWPKoiFarmShop;Persist Security Info=True;User ID=sa;Password=12345;Encrypt=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Consignment>(entity =>
        {
            entity.HasKey(e => e.ConsignmentId).HasName("PK__Consignm__A2E2B54764FC7A08");

            entity.ToTable("Consignment");

            entity.Property(e => e.ConsignmentId).HasColumnName("consignmentID");
            entity.Property(e => e.ConsignmentDate).HasColumnName("consignmentDate");
            entity.Property(e => e.ConsignmentPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("consignmentPrice");
            entity.Property(e => e.ConsignmentType)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("consignmentType");
            entity.Property(e => e.KoiId).HasColumnName("koiID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.UserImage).HasColumnName("userImage");

            entity.HasOne(d => d.Koi).WithMany(p => p.Consignments)
                .HasForeignKey(d => d.KoiId)
                .HasConstraintName("FK__Consignme__koiID__31EC6D26");

            entity.HasOne(d => d.User).WithMany(p => p.Consignments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Consignme__userI__30F848ED");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__2613FDC434D6B30E");

            entity.ToTable("Feedback", tb => tb.HasTrigger("SetFeedbackDetails"));

            entity.Property(e => e.FeedbackId).HasColumnName("feedbackID");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.FeedbackDate).HasColumnName("feedbackDate");
            entity.Property(e => e.FishesId).HasColumnName("fishesID");
            entity.Property(e => e.KoiId).HasColumnName("koiID");
            entity.Property(e => e.OrderId).HasColumnName("orderID");
            entity.Property(e => e.OrderIdFishes).HasColumnName("orderID_fishes");
            entity.Property(e => e.OrderIdKoi).HasColumnName("orderID_koi");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.Order).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Feedback__orderI__2E1BDC42");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Feedback__userID__2D27B809");
        });

        modelBuilder.Entity<Fish>(entity =>
        {
            entity.HasKey(e => e.FishesId).HasName("PK__Fishes__4859C522F207A16A");

            entity.Property(e => e.FishesId).HasColumnName("fishesID");
            entity.Property(e => e.ImageFishes).HasColumnName("imageFishes");
            entity.Property(e => e.KoiTypeId).HasColumnName("koiTypeID");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.KoiType).WithMany(p => p.Fish)
                .HasForeignKey(d => d.KoiTypeId)
                .HasConstraintName("FK__Fishes__koiTypeI__1B0907CE");
        });

        modelBuilder.Entity<Koi>(entity =>
        {
            entity.HasKey(e => e.KoiId).HasName("PK__Koi__915924EF1D7D4E17");

            entity.ToTable("Koi");

            entity.Property(e => e.KoiId).HasColumnName("koiID");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.AwardCertificates)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("awardCertificates");
            entity.Property(e => e.Breed)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("breed");
            entity.Property(e => e.FeedingAmount)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("feedingAmount");
            entity.Property(e => e.FilterRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("filterRate");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("gender");
            entity.Property(e => e.HealthStatus)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("healthStatus");
            entity.Property(e => e.ImageCertificate).HasColumnName("imageCertificate");
            entity.Property(e => e.ImageKoi).HasColumnName("imageKoi");
            entity.Property(e => e.KoiTypeId).HasColumnName("koiTypeID");
            entity.Property(e => e.Origin)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("origin");
            entity.Property(e => e.Personality)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("personality");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Size)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("size");

            entity.HasOne(d => d.KoiType).WithMany(p => p.Kois)
                .HasForeignKey(d => d.KoiTypeId)
                .HasConstraintName("FK__Koi__koiTypeID__173876EA");
        });

        modelBuilder.Entity<KoiType>(entity =>
        {   
            entity.HasKey(e => e.KoiTypeId).HasName("PK__KoiType__F26246F7B8563B8F");

            entity.ToTable("KoiType");

            entity.Property(e => e.KoiTypeId).HasColumnName("koiTypeID");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.HasMany(kt => kt.Kois) // KoiType có nhiều Koi
         .WithOne(k => k.KoiType) // Mỗi Koi thuộc về một KoiType
         .HasForeignKey(k => k.KoiTypeId); // Khóa ngoại trong bảng Koi

            // Thiết lập mối quan hệ với Fishes
            entity.HasMany(kt => kt.Fish) // KoiType có nhiều Fish
                  .WithOne(f => f.KoiType) // Mỗi Fish thuộc về một KoiType
                  .HasForeignKey(f => f.KoiTypeId); // Khóa ngoại trong bảng Fishes
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Order__0809337D442730E9");

            entity.ToTable("Order", tb =>
                {
                    tb.HasTrigger("CalculateDiscountOnPromotion");
                    tb.HasTrigger("UpdateTotalPoints");
                });

            entity.Property(e => e.OrderId).HasColumnName("orderID");
            entity.Property(e => e.DeliveryStatus)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("deliveryStatus");
            entity.Property(e => e.DiscountMoney)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discountMoney");
            entity.Property(e => e.EarnedPoints).HasColumnName("earnedPoints");
            entity.Property(e => e.FinalMoney)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("finalMoney");
            entity.Property(e => e.OrderDate).HasColumnName("orderDate");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("orderStatus");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("paymentMethod");
            entity.Property(e => e.PromotionId).HasColumnName("promotionID");
            entity.Property(e => e.ShippingDate).HasColumnName("shippingDate");
            entity.Property(e => e.TotalMoney)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("totalMoney");
            entity.Property(e => e.UsedPoints).HasColumnName("usedPoints");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.Promotion).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK__Order__promotion__20C1E124");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Order__userID__1FCDBCEB");
        });

        modelBuilder.Entity<OrderFish>(entity =>
        {
            entity.HasKey(e => new { e.OrderId, e.FishesId }).HasName("PK__Order_Fi__3C8CAF2F80C5C4E2");

            entity.ToTable("Order_Fishes");

            entity.Property(e => e.OrderId).HasColumnName("orderID");
            entity.Property(e => e.FishesId).HasColumnName("fishesID");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Fishes).WithMany(p => p.OrderFishes)
                .HasForeignKey(d => d.FishesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Order_Fis__fishe__2A4B4B5E");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderFishes)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Order_Fis__order__29572725");
        });

        modelBuilder.Entity<OrderKoi>(entity =>
        {
            entity.HasKey(e => new { e.OrderId, e.KoiId }).HasName("PK__Order_Ko__E11CA133A5E2D903");

            entity.ToTable("Order_Koi");

            entity.Property(e => e.OrderId).HasColumnName("orderID");
            entity.Property(e => e.KoiId).HasColumnName("koiID");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Koi).WithMany(p => p.OrderKois)
                .HasForeignKey(d => d.KoiId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Order_Koi__koiID__267ABA7A");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderKois)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Order_Koi__order__25869641");
        });

        modelBuilder.Entity<Point>(entity =>
        {
            entity.HasKey(e => e.PointsId).HasName("PK__Points__1EA67E48B88E9AE4");

            entity.Property(e => e.PointsId).HasColumnName("pointsID");
            entity.Property(e => e.TotalPoints)
                .HasDefaultValue(0)
                .HasColumnName("totalPoints");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.User).WithMany(p => p.Points)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Points__userID__36B12243");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__99EB690EE74DC7CF");

            entity.ToTable("Promotion");

            entity.Property(e => e.PromotionId).HasColumnName("promotionID");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DiscountRate)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("discountRate");
            entity.Property(e => e.EndDate).HasColumnName("endDate");
            entity.Property(e => e.PromotionName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("promotionName");
            entity.Property(e => e.StartDate).HasColumnName("startDate");
        });

        modelBuilder.Entity<PurchaseHistory>(entity =>
        {
            entity.HasKey(e => new { e.OrderId, e.UserId }).HasName("PK__Purchase__E4B092B09C2699A5");

            entity.ToTable("PurchaseHistory");

            entity.Property(e => e.OrderId).HasColumnName("orderID");
            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.DeliveryStatus)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("deliveryStatus");
            entity.Property(e => e.DiscountMoney)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discountMoney");
            entity.Property(e => e.EarnedPoints).HasColumnName("earnedPoints");
            entity.Property(e => e.FinalMoney)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("finalMoney");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("orderStatus");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("paymentMethod");
            entity.Property(e => e.PromotionId).HasColumnName("promotionID");
            entity.Property(e => e.PurchaseDate).HasColumnName("purchaseDate");
            entity.Property(e => e.ShippingDate).HasColumnName("shippingDate");
            entity.Property(e => e.TotalMoney)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("totalMoney");
            entity.Property(e => e.UsedPoints).HasColumnName("usedPoints");

            entity.HasOne(d => d.Order).WithMany(p => p.PurchaseHistories)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PurchaseH__order__3A81B327");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PurchaseHistories)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK__PurchaseH__promo__3E52440B");

            entity.HasOne(d => d.User).WithMany(p => p.PurchaseHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PurchaseH__userI__3B75D760");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__CB9A1CDF219739E3");

            entity.ToTable("User");

            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.RegisterDate).HasColumnName("registerDate");
            entity.Property(e => e.Role)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalPoints)
                .HasDefaultValue(0)
                .HasColumnName("totalPoints");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("userName");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
