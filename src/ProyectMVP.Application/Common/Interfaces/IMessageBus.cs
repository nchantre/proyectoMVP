namespace ProyectMVP.Application.Common.Interfaces;

public interface IMessageBus
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : class;
}
