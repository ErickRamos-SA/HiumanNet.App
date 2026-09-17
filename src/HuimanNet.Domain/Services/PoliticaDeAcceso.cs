using HuimanNet.Domain.Entities;
using HuimanNet.Domain.Enums;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Services;

/// <summary>
/// Decide qué puede hacer cada rol sobre cada tipo de documento, sobre los
/// datos de cada empresa y sobre cada acción del sistema.
/// </summary>
/// <remarks>
/// Servicio de dominio puro y determinista. Es el punto único donde vive la
/// matriz de permisos: la API y Blazor lo invocan, no la reimplementan.
/// <para>
/// Dos niveles de control:
/// <list type="bullet">
///   <item><description><b>Acciones</b>: cada rol concede un conjunto por defecto (<see cref="PermisosPorRol"/>) que el administrador ajusta por usuario.</description></item>
///   <item><description><b>Ámbito</b>: un usuario de empresa cliente sólo opera sobre sus empresas (principal y adicionales); el operador y el administrador sobre todas.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class PoliticaDeAcceso
{
    /// <summary>Tipos de documento que pueden cargarse en el portal.</summary>
    private static readonly TipoDocumento[] TiposCargables =
        [TipoDocumento.Incidencia, TipoDocumento.DatosEmpleado, TipoDocumento.Resultado, TipoDocumento.Ajuste];

    /// <summary>
    /// Indica si un rol puede cargar documentos de un tipo determinado.
    /// </summary>
    /// <param name="rol">Rol del usuario solicitante.</param>
    /// <param name="tipo">Tipo de documento que se pretende cargar.</param>
    /// <returns><c>true</c> si la combinación de rol y tipo está permitida.</returns>
    /// <remarks>
    /// Es sólo la matriz por rol. El caso de uso exige además la acción de carga
    /// del tipo; consulte <see cref="GarantizarPuedeCargar(RolUsuario, IEnumerable{PermisoDeUsuario}, TipoDocumento)"/>.
    /// </remarks>
    public bool PuedeCargar(RolUsuario rol, TipoDocumento tipo) => PuedeCargarSegunRol(rol, tipo);

    /// <summary>
    /// Obtiene la acción del sistema que exige cargar un tipo de documento.
    /// </summary>
    /// <param name="tipo">Tipo de documento.</param>
    /// <returns>
    /// <see cref="AccionDelSistema.CargarDocumentos"/> para lo que aporta la
    /// empresa cliente; <see cref="AccionDelSistema.PublicarResultados"/> para
    /// resultados y ajustes.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si el tipo no está especificado.</exception>
    public static AccionDelSistema AccionParaCargar(TipoDocumento tipo) => tipo switch
    {
        TipoDocumento.Incidencia or TipoDocumento.DatosEmpleado => AccionDelSistema.CargarDocumentos,
        TipoDocumento.Resultado or TipoDocumento.Ajuste => AccionDelSistema.PublicarResultados,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de documento no especificado."),
    };

    /// <summary>
    /// Lista los tipos de documento que un usuario puede cargar.
    /// </summary>
    /// <param name="rol">Rol del usuario.</param>
    /// <param name="acciones">Acciones efectivas del usuario.</param>
    /// <returns>
    /// Los tipos que admite la matriz de su rol y para los que tiene habilitada
    /// la acción de carga, en el orden en que conviene ofrecerlos.
    /// </returns>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="acciones"/> es <c>null</c>.</exception>
    /// <remarks>
    /// Combina las dos barreras que aplica el caso de uso de carga, para que la
    /// web y la app ofrezcan exactamente lo que el servidor aceptará.
    /// </remarks>
    public static IReadOnlyList<TipoDocumento> TiposQuePuedeCargar(RolUsuario rol, IReadOnlyCollection<AccionDelSistema> acciones)
    {
        ArgumentNullException.ThrowIfNull(acciones);
        return [.. TiposCargables.Where(t => PuedeCargarSegunRol(rol, t) && acciones.Contains(AccionParaCargar(t)))];
    }

    /// <summary>
    /// Indica si un rol puede descargar un documento de un tipo determinado.
    /// </summary>
    /// <param name="rol">Rol del usuario solicitante.</param>
    /// <param name="tipo">Tipo del documento solicitado.</param>
    /// <returns><c>true</c> si la combinación de rol y tipo está permitida.</returns>
    public bool PuedeDescargar(RolUsuario rol, TipoDocumento tipo)
        => rol switch
        {
            RolUsuario.OperadorNomina => true,
            RolUsuario.Administrador => true,
            RolUsuario.ClienteEmpresa => tipo is TipoDocumento.Resultado
                or TipoDocumento.Ajuste
                or TipoDocumento.Incidencia
                or TipoDocumento.DatosEmpleado,
            _ => false,
        };

    /// <summary>
    /// Indica si un rol opera transversalmente sobre todas las empresas.
    /// </summary>
    /// <param name="rol">Rol del usuario solicitante.</param>
    /// <returns><c>true</c> para el operador de nómina y el administrador.</returns>
    public bool OperaSobreTodasLasEmpresas(RolUsuario rol) => rol.EsTransversal();

    /// <summary>
    /// Calcula las acciones efectivas de un usuario.
    /// </summary>
    /// <param name="rol">Rol del usuario.</param>
    /// <param name="personalizados">Permisos personalizados, o <c>null</c>.</param>
    /// <returns>Conjunto de acciones habilitadas.</returns>
    public IReadOnlySet<AccionDelSistema> AccionesEfectivas(
        RolUsuario rol, IEnumerable<PermisoDeUsuario>? personalizados)
        => PermisosPorRol.Efectivas(rol, personalizados);

    /// <summary>
    /// Indica si un usuario puede ejecutar una acción.
    /// </summary>
    /// <param name="rol">Rol del usuario.</param>
    /// <param name="personalizados">Permisos personalizados, o <c>null</c>.</param>
    /// <param name="accion">Acción consultada.</param>
    /// <returns><c>true</c> si la acción está habilitada.</returns>
    public bool PuedeEjecutar(RolUsuario rol, IEnumerable<PermisoDeUsuario>? personalizados, AccionDelSistema accion)
        => PermisosPorRol.Efectivas(rol, personalizados).Contains(accion);

    /// <summary>
    /// Comprueba que un usuario puede ejecutar una acción.
    /// </summary>
    /// <param name="rol">Rol del usuario.</param>
    /// <param name="personalizados">Permisos personalizados, o <c>null</c>.</param>
    /// <param name="accion">Acción que se pretende ejecutar.</param>
    /// <exception cref="AccesoNoAutorizadoException">Se lanza si la acción no está habilitada.</exception>
    public void GarantizarPuedeEjecutar(
        RolUsuario rol, IEnumerable<PermisoDeUsuario>? personalizados, AccionDelSistema accion)
    {
        if (!PuedeEjecutar(rol, personalizados, accion))
        {
            throw new AccesoNoAutorizadoException(
                $"El usuario con rol '{rol}' no tiene habilitada la acción '{accion}'.");
        }
    }

    /// <summary>
    /// Comprueba que un rol puede cargar un tipo de documento.
    /// </summary>
    /// <param name="rol">Rol del usuario solicitante.</param>
    /// <param name="tipo">Tipo de documento que se pretende cargar.</param>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el rol no puede cargar ese tipo de documento.
    /// </exception>
    public void GarantizarPuedeCargar(RolUsuario rol, TipoDocumento tipo)
    {
        if (!PuedeCargar(rol, tipo))
        {
            throw new AccesoNoAutorizadoException(
                $"El rol '{rol}' no puede cargar documentos de tipo '{tipo}'.");
        }
    }

    /// <summary>
    /// Comprueba que un usuario puede cargar un tipo de documento: que su rol lo
    /// admite y que tiene habilitada la acción de carga de ese tipo.
    /// </summary>
    /// <param name="rol">Rol del usuario solicitante.</param>
    /// <param name="personalizados">Permisos personalizados del usuario, o <c>null</c>.</param>
    /// <param name="tipo">Tipo de documento que se pretende cargar.</param>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el rol no puede cargar ese tipo o si el administrador le
    /// retiró la acción correspondiente.
    /// </exception>
    public void GarantizarPuedeCargar(RolUsuario rol, IEnumerable<PermisoDeUsuario>? personalizados, TipoDocumento tipo)
    {
        GarantizarPuedeCargar(rol, tipo);
        GarantizarPuedeEjecutar(rol, personalizados, AccionParaCargar(tipo));
    }

    /// <summary>
    /// Comprueba que un usuario con una sola empresa puede descargar un documento concreto.
    /// </summary>
    /// <param name="rol">Rol del usuario solicitante.</param>
    /// <param name="empresaDelUsuario">
    /// Empresa del solicitante, o <c>null</c> si su rol es transversal.
    /// </param>
    /// <param name="documento">Documento solicitado.</param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="documento"/> es <c>null</c>.
    /// </exception>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el rol no puede descargar ese tipo de documento o si el
    /// documento pertenece a otra empresa.
    /// </exception>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el documento aún no superó el escaneo de malware.
    /// </exception>
    public void GarantizarPuedeDescargar(RolUsuario rol, Guid? empresaDelUsuario, Documento documento)
        => GarantizarPuedeDescargar(rol, empresaDelUsuario is { } empresa ? [empresa] : [], documento);

    /// <summary>
    /// Comprueba que un usuario puede descargar un documento concreto.
    /// </summary>
    /// <param name="rol">Rol del usuario solicitante.</param>
    /// <param name="empresasDelUsuario">
    /// Empresas en las que opera el solicitante (principal y adicionales); vacía
    /// si su rol es transversal.
    /// </param>
    /// <param name="documento">Documento solicitado.</param>
    /// <exception cref="ArgumentNullException">
    /// Se lanza si <paramref name="empresasDelUsuario"/> o <paramref name="documento"/> es <c>null</c>.
    /// </exception>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el rol no puede descargar ese tipo de documento o si el
    /// documento pertenece a una empresa que no es del usuario.
    /// </exception>
    /// <exception cref="DocumentoInvalidoException">
    /// Se lanza si el documento aún no superó el escaneo de malware.
    /// </exception>
    public void GarantizarPuedeDescargar(RolUsuario rol, IReadOnlyCollection<Guid> empresasDelUsuario, Documento documento)
    {
        ArgumentNullException.ThrowIfNull(empresasDelUsuario);
        ArgumentNullException.ThrowIfNull(documento);

        if (!PuedeDescargar(rol, documento.Tipo))
        {
            throw new AccesoNoAutorizadoException(
                $"El rol '{rol}' no puede descargar documentos de tipo '{documento.Tipo}'.");
        }

        if (!OperaSobreTodasLasEmpresas(rol))
        {
            if (empresasDelUsuario.Count == 0)
            {
                throw new AccesoNoAutorizadoException(
                    "El usuario no está vinculado a ninguna empresa.");
            }

            if (!empresasDelUsuario.Contains(documento.EmpresaId))
            {
                throw new AccesoNoAutorizadoException(
                    $"El documento '{documento.Id}' pertenece a la empresa '{documento.EmpresaId}', a la que el usuario no tiene acceso.");
            }
        }

        documento.GarantizarQueEsDescargable();
    }

    /// <summary>
    /// Resuelve la empresa sobre la que debe operar la petición de un usuario con una sola empresa.
    /// </summary>
    /// <param name="rol">Rol del usuario solicitante.</param>
    /// <param name="empresaDelUsuario">Empresa del usuario, si tiene una.</param>
    /// <param name="empresaSolicitada">Empresa indicada en la petición, si la hay.</param>
    /// <returns>La empresa sobre la que se debe ejecutar la operación.</returns>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si un usuario de empresa cliente intenta operar sobre otra empresa,
    /// o si un rol transversal no indica la empresa objetivo.
    /// </exception>
    public Guid ResolverEmpresaObjetivo(RolUsuario rol, Guid? empresaDelUsuario, Guid? empresaSolicitada)
        => ResolverEmpresaObjetivo(rol, empresaDelUsuario, empresaDelUsuario is { } empresa ? [empresa] : [], empresaSolicitada);

    /// <summary>
    /// Resuelve la empresa sobre la que debe operar una petición.
    /// </summary>
    /// <param name="rol">Rol del usuario solicitante.</param>
    /// <param name="empresaPrincipal">Empresa principal del usuario, la que se usa si no pide otra.</param>
    /// <param name="empresasDelUsuario">Todas las empresas en las que opera el usuario, principal incluida.</param>
    /// <param name="empresaSolicitada">Empresa indicada en la petición, si la hay.</param>
    /// <returns>La empresa sobre la que se debe ejecutar la operación.</returns>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="empresasDelUsuario"/> es <c>null</c>.</exception>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si un usuario de empresa cliente intenta operar sobre una empresa
    /// que no es suya, o si un rol transversal no indica la empresa objetivo.
    /// </exception>
    /// <remarks>
    /// Es el punto donde se corta el riesgo número uno del portal: un cliente
    /// sólo elige entre sus propias empresas; cualquier otra se rechaza.
    /// </remarks>
    public Guid ResolverEmpresaObjetivo(
        RolUsuario rol, Guid? empresaPrincipal, IReadOnlyCollection<Guid> empresasDelUsuario, Guid? empresaSolicitada)
    {
        ArgumentNullException.ThrowIfNull(empresasDelUsuario);

        if (OperaSobreTodasLasEmpresas(rol))
        {
            return empresaSolicitada
                ?? throw new AccesoNoAutorizadoException(
                    $"El rol '{rol}' debe indicar la empresa sobre la que opera.");
        }

        if (empresaPrincipal is null)
        {
            throw new AccesoNoAutorizadoException("El usuario no está vinculado a ninguna empresa.");
        }

        if (empresaSolicitada is null || empresaSolicitada == empresaPrincipal)
        {
            return empresaPrincipal.Value;
        }

        if (empresasDelUsuario.Contains(empresaSolicitada.Value))
        {
            return empresaSolicitada.Value;
        }

        throw new AccesoNoAutorizadoException(
            $"El usuario de la empresa '{empresaPrincipal}' intentó operar sobre '{empresaSolicitada}', a la que no tiene acceso.");
    }

    /// <summary>
    /// Comprueba que un rol puede consultar la bitácora de auditoría del ámbito pedido.
    /// </summary>
    /// <param name="rol">Rol del usuario solicitante.</param>
    /// <param name="empresaConsultada">
    /// Empresa consultada, o <c>null</c> para una consulta transversal.
    /// </param>
    /// <exception cref="AccesoNoAutorizadoException">
    /// Se lanza si el rol no alcanza para el ámbito solicitado.
    /// </exception>
    public void GarantizarPuedeConsultarBitacora(RolUsuario rol, Guid? empresaConsultada)
    {
        if (rol == RolUsuario.Administrador)
        {
            return;
        }

        if (empresaConsultada is null)
        {
            throw new AccesoNoAutorizadoException(
                "Sólo el administrador puede consultar la bitácora de todas las empresas.");
        }

        if (rol is not (RolUsuario.OperadorNomina or RolUsuario.ClienteEmpresa))
        {
            throw new AccesoNoAutorizadoException(
                $"El rol '{rol}' no puede consultar la bitácora de auditoría.");
        }
    }

    /// <summary>
    /// Matriz de carga por rol: la empresa cliente aporta incidencias y datos de
    /// empleados; nómina y administración publican resultados y ajustes.
    /// </summary>
    /// <param name="rol">Rol del usuario.</param>
    /// <param name="tipo">Tipo de documento.</param>
    /// <returns><c>true</c> si la combinación está permitida.</returns>
    private static bool PuedeCargarSegunRol(RolUsuario rol, TipoDocumento tipo)
        => (rol, tipo) switch
        {
            (RolUsuario.ClienteEmpresa, TipoDocumento.Incidencia) => true,
            (RolUsuario.ClienteEmpresa, TipoDocumento.DatosEmpleado) => true,
            (RolUsuario.OperadorNomina, TipoDocumento.Resultado) => true,
            (RolUsuario.OperadorNomina, TipoDocumento.Ajuste) => true,
            (RolUsuario.Administrador, TipoDocumento.Resultado) => true,
            (RolUsuario.Administrador, TipoDocumento.Ajuste) => true,
            _ => false,
        };
}
