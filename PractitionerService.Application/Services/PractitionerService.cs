using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using PractitionerService.Application.Dtos;
using PractitionerService.Domain.Interfaces;
using PractitionerService.Domain.Entities;
using PractitionerService.Application.Interfaces;
using PractitionerService.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
namespace PractitionerService.Application.Services;

public class PractitionerService : IPractitionerService
{
    private readonly IPractitionerRepository _repository;
    private readonly IDistributedCache _cache;

    public PractitionerService(IPractitionerRepository repository, IDistributedCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Practitioner?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"practitioner:{id}";
        var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            return JsonSerializer.Deserialize<Practitioner>(cachedJson);
        }

        var practitioner = await _repository.GetByIdAsync(id, cancellationToken);
        if (practitioner is null || practitioner.IsDeleted)
        {
            return null;
        }

        var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(practitioner), cacheOptions, cancellationToken);
        return practitioner;
    }

    public async Task<IReadOnlyList<Practitioner>> SearchAsync(string? name, string? speciality, bool? active, CancellationToken cancellationToken = default)
    {
        return await _repository.SearchAsync(name, speciality, active, cancellationToken);
    }

    public async Task<Practitioner> CreateAsync(CreatePractitionerDto dto, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var idemCacheKey = $"idem:{idempotencyKey}";
            var existingIdStr = await _cache.GetStringAsync(idemCacheKey, cancellationToken);
            if (Guid.TryParse(existingIdStr, out var existingId))
            {
                var existingPractitioner = await _repository.GetByIdAsync(existingId, cancellationToken);
                if (existingPractitioner is not null)
                {
                    return existingPractitioner;
                }
            }
        }

        var practitioner = new Practitioner
        {
            Id = Guid.NewGuid(),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            Speciality = dto.Speciality,
            PlaceOfWork = dto.PlaceOfWork,
            Email = dto.Email,
            IsActive = dto.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _repository.AddAsync(practitioner, cancellationToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };
            await _cache.SetStringAsync($"idem:{idempotencyKey}", practitioner.Id.ToString(), options, cancellationToken);
        }
        return practitioner;
    }

    public async Task<Practitioner?> UpdateAsync(Guid id, UpdatePractitionerDto dto, int? expectedVersion, CancellationToken cancellationToken = default)
    {
        var practitioner = await _repository.GetByIdAsync(id, cancellationToken);
        if (practitioner is null || practitioner.IsDeleted)
        {
            return null;
        }
        if (expectedVersion.HasValue && practitioner.Version != expectedVersion.Value)
        {
            throw new ConcurrencyConflictException(
                $"Конфликт версий: ожидалась версия {expectedVersion.Value}, но актуальная версия в БД — {practitioner.Version}.");
        }
        practitioner.FirstName = dto.FirstName;
        practitioner.LastName = dto.LastName;
        practitioner.Phone = dto.Phone;
        practitioner.Speciality = dto.Speciality;
        practitioner.PlaceOfWork = dto.PlaceOfWork;
        practitioner.Email = dto.Email;
        practitioner.IsActive = dto.IsActive;
        practitioner.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await _repository.UpdateAsync(practitioner, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Запись была изменена другим процессом во время сохранения.");
        }
        await _cache.RemoveAsync($"practitioner:{id}", cancellationToken);
        return practitioner;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var practitioner = await _repository.GetByIdAsync(id, cancellationToken);
        if (practitioner is null || practitioner.IsDeleted)
        {
            return;
        }
        practitioner.IsDeleted = true;
        practitioner.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(practitioner, cancellationToken);
        await _cache.RemoveAsync($"practitioner:{id}", cancellationToken);
    }
}
