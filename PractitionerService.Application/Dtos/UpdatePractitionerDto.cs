namespace PractitionerService.Application.Dtos
{
    public record UpdatePractitionerDto(
        string FirstName,
        string LastName,
        string? Phone,
        string? Speciality,
        string? PlaceOfWork,
        string? Email,
        bool IsActive
    );
}
