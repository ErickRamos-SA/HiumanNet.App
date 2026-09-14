using System.Globalization;
using HuimanNet.Domain.Exceptions;

namespace HuimanNet.Domain.Formulas;

/// <summary>
/// Convierte el texto de una fórmula en la secuencia de <see cref="Token"/>
/// que consume el <see cref="AnalizadorSintactico"/>.
/// </summary>
/// <remarks>
/// Escrito a mano y sin expresiones regulares: es determinista, no usa
/// reflexión y sobrevive al recorte de Native AOT. Los identificadores se
/// normalizan a mayúsculas, de modo que <c>uma_diaria</c> y <c>UMA_DIARIA</c>
/// se refieren al mismo parámetro.
/// </remarks>
public static class AnalizadorLexico
{
    /// <summary>
    /// Divide una fórmula en tokens.
    /// </summary>
    /// <param name="formula">Texto de la fórmula.</param>
    /// <returns>Los tokens en orden, terminados siempre por <see cref="TipoDeToken.Fin"/>.</returns>
    /// <exception cref="ErrorDeFormulaException">
    /// Se lanza ante un carácter inesperado, un número mal formado o una cadena sin cerrar.
    /// </exception>
    public static IReadOnlyList<Token> Tokenizar(string formula)
    {
        ArgumentNullException.ThrowIfNull(formula);

        var tokens = new List<Token>(formula.Length / 2 + 1);
        int i = 0;

        while (i < formula.Length)
        {
            char c = formula[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsDigit(c) || (c == '.' && i + 1 < formula.Length && char.IsDigit(formula[i + 1])))
            {
                int inicio = i;
                bool huboPunto = false;

                while (i < formula.Length && (char.IsDigit(formula[i]) || formula[i] == '.'))
                {
                    if (formula[i] == '.')
                    {
                        if (huboPunto)
                        {
                            throw new ErrorDeFormulaException("Número con más de un punto decimal", i);
                        }

                        huboPunto = true;
                    }

                    i++;
                }

                string texto = formula[inicio..i];

                if (!decimal.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _))
                {
                    throw new ErrorDeFormulaException($"Número no válido '{texto}'", inicio);
                }

                tokens.Add(new Token(TipoDeToken.Numero, texto, inicio));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                int inicio = i;

                while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '_'))
                {
                    i++;
                }

                tokens.Add(new Token(TipoDeToken.Identificador, formula[inicio..i].ToUpperInvariant(), inicio));
                continue;
            }

            if (c == '"')
            {
                int inicio = i;
                int cierre = formula.IndexOf('"', i + 1);

                if (cierre < 0)
                {
                    throw new ErrorDeFormulaException("Cadena de texto sin cerrar", inicio);
                }

                tokens.Add(new Token(TipoDeToken.Cadena, formula[(i + 1)..cierre].ToUpperInvariant(), inicio));
                i = cierre + 1;
                continue;
            }

            switch (c)
            {
                case '(':
                    tokens.Add(new Token(TipoDeToken.ParentesisAbre, "(", i));
                    i++;
                    continue;
                case ')':
                    tokens.Add(new Token(TipoDeToken.ParentesisCierra, ")", i));
                    i++;
                    continue;
                case ',':
                case ';':
                    tokens.Add(new Token(TipoDeToken.Coma, ",", i));
                    i++;
                    continue;
                case '+':
                case '-':
                case '*':
                case '/':
                case '^':
                case '=':
                    tokens.Add(new Token(TipoDeToken.Operador, c.ToString(), i));
                    i++;
                    continue;
                case '<':
                    if (i + 1 < formula.Length && formula[i + 1] == '=')
                    {
                        tokens.Add(new Token(TipoDeToken.Operador, "<=", i));
                        i += 2;
                    }
                    else if (i + 1 < formula.Length && formula[i + 1] == '>')
                    {
                        tokens.Add(new Token(TipoDeToken.Operador, "<>", i));
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TipoDeToken.Operador, "<", i));
                        i++;
                    }

                    continue;
                case '>':
                    if (i + 1 < formula.Length && formula[i + 1] == '=')
                    {
                        tokens.Add(new Token(TipoDeToken.Operador, ">=", i));
                        i += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TipoDeToken.Operador, ">", i));
                        i++;
                    }

                    continue;
                case '!':
                    if (i + 1 < formula.Length && formula[i + 1] == '=')
                    {
                        tokens.Add(new Token(TipoDeToken.Operador, "<>", i));
                        i += 2;
                        continue;
                    }

                    throw new ErrorDeFormulaException("Carácter inesperado '!'", i);
                default:
                    throw new ErrorDeFormulaException($"Carácter inesperado '{c}'", i);
            }
        }

        tokens.Add(new Token(TipoDeToken.Fin, string.Empty, formula.Length));
        return tokens;
    }
}
