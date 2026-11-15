using Microsoft.EntityFrameworkCore;
using PractitionerService.Domain.Entities;

namespace PractitionerService.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();
        if (await context.Practitioners.AnyAsync())
        {
            return;
        }
        var initialPractitioners = new List<Practitioner>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FirstName = "Иван",
                LastName = "Иванов",
                Phone = "+375 29 511 15 41",
                Email = "ivanov@ex.com",
                Speciality = "Терапевт",
                PlaceOfWork = "Городская поликлиника №1",
                IsActive = true,
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FirstName = "Анна",
                LastName = "Смирнова",
                Phone = "+375 29 785 54 88",
                Email = "smirnova@ex.com",
                Speciality = "Кардиолог",
                PlaceOfWork = "Городская поликлиника №2",
                IsActive = true,
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                FirstName = "Петр",
                LastName = "Соколов",
                Phone = "+375 29 955 55 66",
                Email = "sokolov@ex.com",
                Speciality = "Хирург",
                PlaceOfWork = "Больница скорой помощи",
                IsActive = false,
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        await context.Practitioners.AddRangeAsync(initialPractitioners);
        await context.SaveChangesAsync();
    }
}
