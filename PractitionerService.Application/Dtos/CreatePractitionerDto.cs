namespace PractitionerService.Application.Dtos
{
    public record CreatePractitionerDto(
        string FirstName,
        string LastName,
        string? Phone,
        string? Speciality,
        string? PlaceOfWork,
        string? Email,
        bool IsActive = true
    );
}
