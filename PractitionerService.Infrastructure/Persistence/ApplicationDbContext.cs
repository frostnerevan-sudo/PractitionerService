using Microsoft.EntityFrameworkCore;
using PractitionerService.Domain.Entities;

namespace PractitionerService.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Practitioner> Practitioners => Set<Practitioner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Practitioner>(entity =>
        {
            entity.ToTable("practitioners");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Speciality).HasMaxLength(150);
            entity.Property(e => e.PlaceOfWork).HasMaxLength(250);
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.HasGeneratedTsVectorColumn(
                    p => p.SearchVector,
                    "russian",
                    p => new { p.FirstName, p.LastName })
                .HasIndex(p => p.SearchVector)
                .HasMethod("GIN");
        });
    }
}
