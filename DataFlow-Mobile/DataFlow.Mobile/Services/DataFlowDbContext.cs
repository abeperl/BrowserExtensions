using Microsoft.EntityFrameworkCore;
using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services;

public class DataFlowDbContext : DbContext
{
    public DataFlowDbContext(DbContextOptions<DataFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Page> Pages { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<PageAction> Actions { get; set; }
    public DbSet<AuthenticationConfig> AuthenticationConfigs { get; set; }
    public DbSet<AppSettings> Settings { get; set; }
    public DbSet<AudioConfigModel> AudioConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Page entity
        modelBuilder.Entity<Page>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ApiEndpoint).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ApiMethod).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Name).IsUnique();

            // Configure relationship with Template
            entity.HasOne(e => e.Template)
                  .WithMany(t => t.Pages)
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Configure relationship with Actions
            entity.HasMany(e => e.Actions)
                  .WithOne(a => a.Page)
                  .HasForeignKey(a => a.PageId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Template entity
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure PageAction entity
        modelBuilder.Entity<PageAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ActionType).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Page)
                  .WithMany(p => p.Actions)
                  .HasForeignKey(e => e.PageId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuthenticationConfig entity
        modelBuilder.Entity<AuthenticationConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AuthenticationType).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Page)
                  .WithOne()
                  .HasForeignKey<AuthenticationConfig>(e => e.PageId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AppSettings entity
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // Configure AudioConfig entity
        modelBuilder.Entity<AudioConfigModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AudioFileName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Volume).HasPrecision(3, 2);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Seed default data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default app settings
        modelBuilder.Entity<AppSettings>().HasData(
            new AppSettings { Id = 1, Key = "AudioEnabled", Value = "true", DataType = "Boolean", Category = "Audio" },
            new AppSettings { Id = 2, Key = "AudioVolume", Value = "0.8", DataType = "Double", Category = "Audio" },
            new AppSettings { Id = 3, Key = "DefaultPageRefreshInterval", Value = "30", DataType = "Integer", Category = "Data" },
            new AppSettings { Id = 4, Key = "EnableHapticFeedback", Value = "true", DataType = "Boolean", Category = "UI" },
            new AppSettings { Id = 5, Key = "ThemeMode", Value = "System", DataType = "String", Category = "UI" }
        );

        // Seed default template
        modelBuilder.Entity<Template>().HasData(
            new Template
            {
                Id = 1,
                Name = "Default List Template",
                Description = "Standard list layout for displaying API data",
                LayoutType = "List",
                ColorScheme = "Default",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}