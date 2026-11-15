using PractitionerService.Application.Dtos;
using PractitionerService.Domain.Entities;

namespace PractitionerService.Application.Interfaces
{
    public interface IPractitionerService
    {
        Task<Practitioner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Practitioner>> SearchAsync(string? name, string? speciality, bool? active, CancellationToken cancellationToken = default);
        Task<Practitioner> CreateAsync(CreatePractitionerDto dto, string? idempotencyKey, CancellationToken cancellationToken = default);
        Task<Practitioner?> UpdateAsync(Guid id, UpdatePractitionerDto dto, int? expectedVersion, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
