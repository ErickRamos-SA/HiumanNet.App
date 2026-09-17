using FluentAssertions;
using HuimanNet.Domain.Exceptions;
using HuimanNet.Domain.ValueObjects;
using Xunit;

namespace HuimanNet.Domain.Tests;

public sealed class NombreArchivoTests
{
    [Theory]
    [InlineData("incidencias.xlsx")]
    [InlineData("Nómina agosto (1).csv")]
    [InlineData("recibos_2026-08 #3.pdf")]
    public void Crear_ConNombreValido_ConservaElNombre(string nombre)
    {
        NombreArchivo archivo = NombreArchivo.Crear(nombre);

        archivo.Valor.Should().Be(nombre);
    }

    // Path.GetInvalidFileNameChars sólo incluye estos caracteres en Windows: el
    // resultado no debe depender del sistema en el que corren las pruebas.
    [Theory]
    [InlineData("a:b.xlsx")]
    [InlineData("a*b.xlsx")]
    [InlineData("a?b.xlsx")]
    [InlineData("a<b.xlsx")]
    [InlineData("a>b.xlsx")]
    [InlineData("a|b.xlsx")]
    [InlineData("a\"b.xlsx")]
    public void Crear_ConCaracterReservadoDeWindows_LanzaDocumentoInvalido(string nombre)
    {
        Action accion = () => NombreArchivo.Crear(nombre);

        accion.Should().Throw<DocumentoInvalidoException>().WithMessage("*caracteres*");
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x09)]
    [InlineData(0x1F)]
    public void Crear_ConCaracterDeControl_LanzaDocumentoInvalido(int codigo)
    {
        string nombre = $"a{(char)codigo}b.xlsx";

        Action accion = () => NombreArchivo.Crear(nombre);

        accion.Should().Throw<DocumentoInvalidoException>().WithMessage("*caracteres de control*");
    }

    [Theory]
    [InlineData("carpeta/archivo.xlsx")]
    [InlineData("carpeta\\archivo.xlsx")]
    [InlineData("..archivo.xlsx")]
    public void Crear_ConComponentesDeRuta_LanzaDocumentoInvalido(string nombre)
    {
        Action accion = () => NombreArchivo.Crear(nombre);

        accion.Should().Throw<DocumentoInvalidoException>().WithMessage("*rutas*");
    }
}
