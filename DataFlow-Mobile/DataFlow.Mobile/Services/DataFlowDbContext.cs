using Microsoft.EntityFrameworkCore;
using DataFlow.Mobile.Models;

namespace DataFlow.Mobile.Services;

public class DataFlowDbContext : DbContext
{
    public DataFlowDbContext(DbContextOptions<DataFlowDbContext> options) : base(options)
    {
    }

    public DbSet<DataPage> Pages { get; set; }
    public DbSet<Template> Templates { get; set; }
    public DbSet<PageAction> Actions { get; set; }
    public DbSet<AuthenticationConfig> AuthenticationConfigs { get; set; }
    public DbSet<AppSettings> Settings { get; set; }
    public DbSet<AudioConfigModel> AudioConfigs { get; set; }
    public DbSet<TemplateColumn> TemplateColumns { get; set; }
    public DbSet<ColorScheme> ColorSchemes { get; set; }
    public DbSet<LayoutTemplate> LayoutTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DataPage entity
        modelBuilder.Entity<DataPage>(entity =>
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

            // Configure relationships
            entity.HasOne(e => e.ColorScheme)
                  .WithMany(cs => cs.Templates)
                  .HasForeignKey(e => e.ColorSchemeId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.LayoutTemplate)
                  .WithMany(lt => lt.Templates)
                  .HasForeignKey(e => e.LayoutTemplateId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Columns)
                  .WithOne(tc => tc.Template)
                  .HasForeignKey(tc => tc.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);
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

        // Configure TemplateColumn entity
        modelBuilder.Entity<TemplateColumn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PropertyName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DataType).IsRequired().HasMaxLength(50);

            entity.HasOne(e => e.Template)
                  .WithMany(t => t.Columns)
                  .HasForeignKey(e => e.TemplateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TemplateId, e.PropertyName }).IsUnique();
        });

        // Configure ColorScheme entity
        modelBuilder.Entity<ColorScheme>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PrimaryColor).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BackgroundColor).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure LayoutTemplate entity
        modelBuilder.Entity<LayoutTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LayoutType).IsRequired().HasMaxLength(50);
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
                ColorSchemeId = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}