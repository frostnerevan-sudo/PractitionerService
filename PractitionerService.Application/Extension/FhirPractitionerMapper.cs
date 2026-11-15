using Hl7.Fhir.Model;
using DomainPractitioner = PractitionerService.Domain.Entities.Practitioner;

namespace PractitionerService.Application.Extension;

public static class FhirPractitionerMapper
{
    public static Practitioner ToFhirPractitioner(this DomainPractitioner entity)
    {
        var fhirPractitioner = new Practitioner
        {
            Id = entity.Id.ToString(),
            Active = entity.IsActive,
            Meta = new Meta
            {
                VersionId = entity.Version.ToString(),
                LastUpdated = entity.UpdatedAt ?? entity.CreatedAt
            },
            Name = new List<HumanName>
            {
                new HumanName
                {
                    Use = HumanName.NameUse.Official,
                    Family = entity.LastName,
                    Given = new[] { entity.FirstName }
                }
            },
            Telecom = new List<ContactPoint>()
        };

        if (!string.IsNullOrWhiteSpace(entity.Phone))
        {
            fhirPractitioner.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Value = entity.Phone,
                Use = ContactPoint.ContactPointUse.Work
            });
        }

        if (!string.IsNullOrWhiteSpace(entity.Email))
        {
            fhirPractitioner.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Email,
                Value = entity.Email,
                Use = ContactPoint.ContactPointUse.Work
            });
        }

        if (!string.IsNullOrWhiteSpace(entity.Speciality))
        {
            fhirPractitioner.Qualification = new List<Practitioner.QualificationComponent>
            {
                new Practitioner.QualificationComponent
                {
                    Code = new CodeableConcept
                    {
                        Text = entity.Speciality
                    }
                }
            };
        }

        if (!string.IsNullOrWhiteSpace(entity.PlaceOfWork))
        {
            fhirPractitioner.Extension.Add(new Hl7.Fhir.Model.Extension
            {
                Url = "http://example.org",
                Value = new FhirString(entity.PlaceOfWork)
            });
        }

        return fhirPractitioner;
    }
}
