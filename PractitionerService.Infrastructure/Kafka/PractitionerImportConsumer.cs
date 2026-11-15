using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PractitionerService.Domain.Entities;
using PractitionerService.Domain.Interfaces;

namespace PractitionerService.Infrastructure.Kafka;

public class PractitionerImportConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PractitionerImportConsumer> _logger;
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _groupId;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PractitionerImportConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<PractitionerImportConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topic = configuration["Kafka:Topic"] ?? "practitioner-imports";
        _groupId = configuration["Kafka:GroupId"] ?? "practitioner-service-group";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_topic);

        _logger.LogInformation("Kafka Consumer запущен. Ожидание сообщений из топика {Topic}...", _topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    if (consumeResult?.Message?.Value is null)
                        continue;
                    await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);
                    consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Ошибка при чтении из Kafka: {Reason}", ex.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Непредвиденная ошибка при обработке сообщения из Kafka.");
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka Consumer корректно остановлен.");
        }
    }

    private async Task ProcessMessageAsync(string rawMessage, CancellationToken cancellationToken)
    {
        PractitionerImportEvent? importEvent;
        try
        {
            importEvent = JsonSerializer.Deserialize<PractitionerImportEvent>(rawMessage, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Не удалось десериализовать сообщение из Kafka: {Message}", rawMessage);
            return;
        }
        if (importEvent is null || string.IsNullOrWhiteSpace(importEvent.SourceEventId))
        {
            _logger.LogWarning("Пропущено некорректное событие без SourceEventId.");
            return;
        }
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPractitionerRepository>();
        var alreadyProcessed = await repository.ExistsBySourceEventIdAsync(importEvent.SourceEventId, cancellationToken);
        if (alreadyProcessed)
        {
            _logger.LogInformation("Событие {SourceEventId} уже было обработано ранее. Пропуск дубликата.", importEvent.SourceEventId);
            return;
        }

        var practitioner = new Practitioner
        {
            Id = Guid.NewGuid(),
            FirstName = importEvent.FirstName,
            LastName = importEvent.LastName,
            Phone = importEvent.Phone,
            Speciality = importEvent.Speciality,
            PlaceOfWork = importEvent.PlaceOfWork,
            Email = importEvent.Email,
            IsActive = importEvent.IsActive,
            SourceEventId = importEvent.SourceEventId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await repository.AddAsync(practitioner, cancellationToken);
        _logger.LogInformation("Врач {LastName} {FirstName} успешно импортирован из Kafka (EventId: {EventId}).",
            practitioner.LastName, practitioner.FirstName, importEvent.SourceEventId);
    }
}
