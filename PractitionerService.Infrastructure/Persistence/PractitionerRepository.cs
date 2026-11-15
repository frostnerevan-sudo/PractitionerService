using Microsoft.EntityFrameworkCore;
using PractitionerService.Domain.Entities;
using PractitionerService.Domain.Interfaces;

namespace PractitionerService.Infrastructure.Persistence
{
    public class PractitionerRepository : IPractitionerRepository
    {
        private readonly ApplicationDbContext _context;

        public PractitionerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Practitioner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Practitioners
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Practitioner>> SearchAsync(
            string? name,
            string? speciality,
            bool? active,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Practitioners
                .AsNoTracking()
                .Where(p => !p.IsDeleted);

            if (active.HasValue)
            {
                query = query.Where(p => p.IsActive == active.Value);
            }

            if (!string.IsNullOrWhiteSpace(speciality))
            {
                query = query.Where(p => p.Speciality != null && EF.Functions.ILike(p.Speciality, $"%{speciality}%"));
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(p => p.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("russian", name)));
            }

            return await query
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Take(100)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Practitioner practitioner, CancellationToken cancellationToken = default)
        {
            practitioner.Version = 1;
            await _context.Practitioners.AddAsync(practitioner, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Practitioner practitioner, CancellationToken cancellationToken = default)
        {
            practitioner.Version += 1;
            _context.Practitioners.Update(practitioner);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsBySourceEventIdAsync(string sourceEventId, CancellationToken cancellationToken = default)
        {
            return await _context.Practitioners
                .AnyAsync(p => p.SourceEventId == sourceEventId, cancellationToken);
        }
    }
}
