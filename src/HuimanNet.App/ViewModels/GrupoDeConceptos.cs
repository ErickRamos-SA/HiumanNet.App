using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HuimanNet.App.Services;
using HuimanNet.Contracts.Localizacion;
using HuimanNet.Contracts.Nomina;
using HuimanNet.Contracts.Periodos;
using HuimanNet.Domain.Enums;

namespace HuimanNet.App.ViewModels;

/// <summary>Conceptos de un tipo, para una lista agrupada.</summary>
public sealed class GrupoDeConceptos : List<ConceptoCalculadoDto>
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GrupoDeConceptos"/>.
    /// </summary>
    /// <param name="nombre">Nombre traducido del tipo.</param>
    /// <param name="conceptos">Conceptos del grupo.</param>
    public GrupoDeConceptos(string nombre, IEnumerable<ConceptoCalculadoDto> conceptos)
        : base(conceptos)
        => Nombre = nombre;

    /// <summary>Obtiene el nombre del grupo.</summary>
    /// <value>Por ejemplo «Percepciones».</value>
    public string Nombre { get; }
}
