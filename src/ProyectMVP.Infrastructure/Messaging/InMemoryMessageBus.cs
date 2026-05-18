using Microsoft.Extensions.Logging;
using ProyectMVP.Application.Common.Interfaces;

namespace ProyectMVP.Infrastructure.Messaging;

public sealed class InMemoryMessageBus : IMessageBus
{
    private readonly ILogger<InMemoryMessageBus> _logger;

    public InMemoryMessageBus(ILogger<InMemoryMessageBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        _logger.LogInformation("Evento publicado en {Topic}: {MessageType}", topic, typeof(T).Name);
        return Task.CompletedTask;
    }
}
