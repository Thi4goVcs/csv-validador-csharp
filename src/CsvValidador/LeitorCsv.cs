using System.Text;

namespace CsvValidador;

public sealed record Registro(int Linha, IReadOnlyList<string> Campos);

public sealed record ErroCsv(int Linha, string Mensagem);

public sealed record ResultadoLeitura(IReadOnlyList<Registro> Registros, IReadOnlyList<ErroCsv> Erros);

// Parser de CSV escrito na mão (sem biblioteca), seguindo o essencial da RFC 4180:
// campo entre aspas, "" como aspas escapada e quebra de linha dentro de campo citado.
public static class LeitorCsv
{
    public static ResultadoLeitura Ler(string texto, char delimitador = ',')
    {
        if (delimitador is '"' or '\n' or '\r')
            throw new ArgumentException("Delimitador não pode ser aspas nem quebra de linha.", nameof(delimitador));

        if (texto.Length > 0 && texto[0] == '﻿')
            texto = texto[1..];

        var registros = new List<Registro>();
        var erros = new List<ErroCsv>();
        var campos = new List<string>();
        var campo = new StringBuilder();

        int linha = 1;
        int linhaInicio = 1;
        int linhaAspasAbertas = 0;
        bool entreAspas = false;
        bool campoCitado = false;
        bool aspasFechadas = false;
        bool erroNoRegistro = false;

        void ReportarErro(string mensagem)
        {
            // um erro por registro basta: o resto da linha costuma ser consequência do primeiro
            if (erroNoRegistro) return;
            erroNoRegistro = true;
            erros.Add(new ErroCsv(linha, mensagem));
        }

        void FecharCampo()
        {
            campos.Add(campo.ToString());
            campo.Clear();
            campoCitado = false;
            aspasFechadas = false;
        }

        void FecharRegistro()
        {
            bool linhaEmBranco = campos.Count == 0 && campo.Length == 0 && !campoCitado;
            if (!linhaEmBranco)
            {
                FecharCampo();
                registros.Add(new Registro(linhaInicio, campos.ToArray()));
                campos.Clear();
            }
            erroNoRegistro = false;
        }

        for (int i = 0; i < texto.Length; i++)
        {
            char c = texto[i];

            if (entreAspas)
            {
                if (c == '"')
                {
                    if (i + 1 < texto.Length && texto[i + 1] == '"')
                    {
                        campo.Append('"');
                        i++;
                    }
                    else
                    {
                        entreAspas = false;
                        aspasFechadas = true;
                    }
                }
                else
                {
                    if (c == '\n') linha++;
                    campo.Append(c);
                }
                continue;
            }

            if (c == delimitador)
            {
                FecharCampo();
            }
            else if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < texto.Length && texto[i + 1] == '\n') i++;
                FecharRegistro();
                linha++;
                linhaInicio = linha;
            }
            else if (c == '"')
            {
                if (campo.Length == 0 && !campoCitado)
                {
                    entreAspas = true;
                    campoCitado = true;
                    linhaAspasAbertas = linha;
                }
                else
                {
                    ReportarErro("aspas no meio de um campo sem aspas");
                    campo.Append(c);
                }
            }
            else
            {
                if (aspasFechadas) ReportarErro("texto depois das aspas que fecharam o campo");
                campo.Append(c);
            }
        }

        if (entreAspas)
        {
            // o campo aberto engoliu o resto do arquivo: o registro inteiro é descartado
            erros.Add(new ErroCsv(linhaAspasAbertas, "aspas abertas e nunca fechadas"));
        }
        else
        {
            FecharRegistro();
        }

        return new ResultadoLeitura(registros, erros);
    }
}
