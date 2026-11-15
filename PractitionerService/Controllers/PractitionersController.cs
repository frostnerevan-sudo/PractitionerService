using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using PractitionerService.Application.Dtos;
using PractitionerService.Application.Extension;
using PractitionerService.Application.Interfaces;
using PractitionerService.Domain.Exceptions;

namespace PractitionerService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PractitionerController : ControllerBase
{
    private readonly IPractitionerService _practitionerService;

    public PractitionerController(IPractitionerService practitionerService)
    {
        _practitionerService = practitionerService;
    }

    /// <summary>
    /// Получение врача по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var practitioner = await _practitionerService.GetByIdAsync(id, cancellationToken);
        if (practitioner is null)
            return NotFound(CreateOperationOutcome("Врач с указанным ID не найден.", OperationOutcome.IssueType.NotFound));
        var fhirResource = practitioner.ToFhirPractitioner();
        Response.Headers.ETag = $"W/\"{practitioner.Version}\"";
        if (practitioner.UpdatedAt.HasValue)
            Response.Headers.LastModified = practitioner.UpdatedAt.Value.ToString("R");
        var json = FhirJsonSerializer.Default.SerializeToString(fhirResource, pretty: true);
        return Content(json, "application/fhir+json");
    }

    /// <summary>
    /// Поиск и фильтрация списка врачей
    /// </summary>
    [HttpGet]
    [Produces("application/fhir+json")]
    public async Task<IActionResult> Search(
        [FromQuery] string? name,
        [FromQuery] string? speciality,
        [FromQuery] bool? active,
        CancellationToken cancellationToken)
    {
        var practitioners = await _practitionerService.SearchAsync(name, speciality, active, cancellationToken);
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Searchset,
            Total = practitioners.Count,
            Entry = new List<Bundle.EntryComponent>()
        };
        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/Practitioner";
        foreach (var item in practitioners)
        {
            bundle.Entry.Add(new Bundle.EntryComponent
            {
                FullUrl = $"{baseUrl}/{item.Id}",
                Resource = item.ToFhirPractitioner(),
                Search = new Bundle.SearchComponent { Mode = Bundle.SearchEntryMode.Match }
            });
        }
        var json = FhirJsonSerializer.Default.SerializeToString(bundle, pretty: true);
        return Content(json, "application/fhir+json");
    }

    /// <summary>
    /// Создание врача
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePractitionerDto dto,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var createdPractitioner = await _practitionerService.CreateAsync(dto, idempotencyKey, cancellationToken);
        var fhirResource = createdPractitioner.ToFhirPractitioner();
        var json = FhirJsonSerializer.Default.SerializeToString(fhirResource, pretty: true);
        var locationUrl = $"{Request.Scheme}://{Request.Host}/api/Practitioner/{createdPractitioner.Id}";
        Response.Headers.Location = locationUrl;
        Response.Headers.ETag = $"W/\"{createdPractitioner.Version}\"";
        return StatusCode(StatusCodes.Status201Created, Content(json, "application/fhir+json").Content);
    }

    /// <summary>
    /// Обновление данных врача
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePractitionerDto dto,
        [FromHeader(Name = "If-Match")] string? ifMatchHeader,
        CancellationToken cancellationToken)
    {
        int? expectedVersion = ParseIfMatchHeader(ifMatchHeader);
        if (!string.IsNullOrWhiteSpace(ifMatchHeader) && expectedVersion is null)
            return BadRequest(CreateOperationOutcome("Некорректный формат заголовка If-Match. Ожидается формат W/\"1\" или 1.", OperationOutcome.IssueType.Invalid));
        try
        {
            var updated = await _practitionerService.UpdateAsync(id, dto, expectedVersion, cancellationToken);
            if (updated is null)
                return NotFound(CreateOperationOutcome("Врач для обновления не найден.", OperationOutcome.IssueType.NotFound));
            var fhirResource = updated.ToFhirPractitioner();
            var json = FhirJsonSerializer.Default.SerializeToString(fhirResource, pretty: true);
            Response.Headers.ETag = $"W/\"{updated.Version}\"";
            return Content(json, "application/fhir+json");
        }
        catch (ConcurrencyConflictException ex)
        {
            var outcomeJson = CreateOperationOutcome(ex.Message, OperationOutcome.IssueType.Conflict);
            return StatusCode(StatusCodes.Status412PreconditionFailed, Content(outcomeJson, "application/fhir+json").Content);
        }
    }

    /// <summary>
    /// Удаление врача
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _practitionerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private string CreateOperationOutcome(string message, OperationOutcome.IssueType type)
    {
        var outcome = new OperationOutcome
        {
            Issue = new List<OperationOutcome.IssueComponent>
            {
                new OperationOutcome.IssueComponent
                {
                    Severity = OperationOutcome.IssueSeverity.Error,
                    Code = type,
                    Diagnostics = message
                }
            }
        };
        return FhirJsonSerializer.Default.SerializeToString(outcome, pretty: true);
    }

    private static int? ParseIfMatchHeader(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
            return null;
        var cleaned = headerValue.Trim();
        if (cleaned.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[2..];
        cleaned = cleaned.Trim('"');
        return int.TryParse(cleaned, out var version) ? version : null;
    }
}
