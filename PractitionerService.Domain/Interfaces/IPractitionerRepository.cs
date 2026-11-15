using PractitionerService.Domain.Entities;

namespace PractitionerService.Domain.Interfaces
{
    public interface IPractitionerRepository
    {
        Task AddAsync(Practitioner practitioner, CancellationToken cancellationToken);
        Task UpdateAsync(Practitioner practitioner, CancellationToken cancellationToken);
        Task<IReadOnlyList<Practitioner>> SearchAsync(string? name, string? speciality, bool? active, CancellationToken cancellationToken);
        Task<Practitioner?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<bool> ExistsBySourceEventIdAsync(string sourceEventId, CancellationToken cancellationToken);
    }
}
