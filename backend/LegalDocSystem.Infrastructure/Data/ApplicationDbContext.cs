using LegalDocSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LegalDocSystem.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Company> Companies { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<ProjectPermission> ProjectPermissions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company configuration
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            
            // Relationships
            entity.HasMany(e => e.Users)
                .WithOne(u => u.Company)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasMany(e => e.Projects)
                .WithOne(p => p.Company)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique();
            
            // Relationships
            entity.HasMany(e => e.ProjectPermissions)
                .WithOne(pp => pp.User)
                .HasForeignKey(pp => pp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasMany(e => e.Documents)
                .WithOne(d => d.UploadedBy)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasMany(e => e.AuditLogs)
                .WithOne(al => al.User)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Project configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.ClientName).HasMaxLength(200);
            entity.Property(e => e.CaseNumber).HasMaxLength(100);
            entity.HasIndex(e => new { e.CompanyId, e.CaseNumber });
            
            // Relationships
            entity.HasMany(e => e.Documents)
                .WithOne(d => d.Project)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasMany(e => e.ProjectPermissions)
                .WithOne(pp => pp.Project)
                .HasForeignKey(pp => pp.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileExtension).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BlobStoragePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.BlobContainerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContentHash).HasMaxLength(100);
            entity.HasIndex(e => new { e.ProjectId, e.IsLatestVersion });
            entity.HasIndex(e => e.ParentDocumentId);
            
            // Self-referencing relationship for versions
            entity.HasOne(e => e.ParentDocument)
                .WithMany(d => d.Versions)
                .HasForeignKey(e => e.ParentDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProjectPermission configuration
        modelBuilder.Entity<ProjectPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProjectId, e.UserId }).IsUnique();
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.HasIndex(e => new { e.CompanyId, e.CreatedAt });
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<Domain.Common.BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
