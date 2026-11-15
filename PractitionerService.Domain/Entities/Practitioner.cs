using NpgsqlTypes;

namespace PractitionerService.Domain.Entities
{
    public class Practitioner
    {
        public Guid Id { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsActive { get; set; }

        public required string LastName { get; set; }

        public required string FirstName { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Speciality { get; set; }

        public string? PlaceOfWork { get; set; }

        public long Version { get; set; }

        public string? SourceEventId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public NpgsqlTsVector SearchVector { get; set; } = null!;
    }
}
