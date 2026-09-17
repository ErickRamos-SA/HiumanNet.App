using HuimanNet.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace HuimanNet.Infrastructure.Tests;

/// <summary>Ejecuta casos de uso en un ámbito nuevo, como hace cada petición.</summary>
internal sealed class Casos(IServiceProvider servicios)
{
    public async Task<TR> Ejecutar<TC, TR>(TC comando)
    {
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IManejadorDeComando<TC, TR>>().EjecutarAsync(comando, EntornoSqlDePrueba.Ct);
    }

    public async Task Ejecutar<TC>(TC comando)
    {
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        await ambito.ServiceProvider.GetRequiredService<IManejadorDeComando<TC>>().EjecutarAsync(comando, EntornoSqlDePrueba.Ct);
    }

    public async Task<TR> Consultar<TQ, TR>(TQ consulta)
    {
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        return await ambito.ServiceProvider.GetRequiredService<IManejadorDeConsulta<TQ, TR>>().EjecutarAsync(consulta, EntornoSqlDePrueba.Ct);
    }

    public async Task<TR> Usar<TS, TR>(Func<TS, Task<TR>> operacion)
        where TS : notnull
    {
        await using AsyncServiceScope ambito = servicios.CreateAsyncScope();
        return await operacion(ambito.ServiceProvider.GetRequiredService<TS>());
    }
}
