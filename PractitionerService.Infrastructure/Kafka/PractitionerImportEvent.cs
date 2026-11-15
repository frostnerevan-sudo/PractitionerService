namespace PractitionerService.Infrastructure.Kafka
{
    public record PractitionerImportEvent(
    string SourceEventId,
    string FirstName,
    string LastName,
    string? Phone,
    string? Speciality,
    string? PlaceOfWork,
    string? Email,
    bool IsActive = true
);
}
