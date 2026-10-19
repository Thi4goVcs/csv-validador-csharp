using System.Globalization;

namespace CsvValidador;

public enum TipoColuna
{
    Vazia,
    Inteiro,
    Decimal,
    Texto,
}

public sealed record RelatorioColuna(string Nome, int Preenchidos, int Vazios, TipoColuna Tipo);

public sealed record Relatorio(
    IReadOnlyList<string> Cabecalho,
    int LinhasDeDados,
    IReadOnlyList<RelatorioColuna> Colunas,
    IReadOnlyList<ErroCsv> Erros)
{
    public bool Valido => Erros.Count == 0;
}

public static class Validador
{
    public static Relatorio Validar(string texto, char delimitador = ',')
    {
        var leitura = LeitorCsv.Ler(texto, delimitador);
        var erros = new List<ErroCsv>(leitura.Erros);

        if (leitura.Registros.Count == 0)
        {
            erros.Add(new ErroCsv(1, "arquivo sem nenhuma linha (nem cabeçalho)"));
            return new Relatorio(Array.Empty<string>(), 0, Array.Empty<RelatorioColuna>(), Ordenar(erros));
        }

        var cabecalho = leitura.Registros[0].Campos;
        var linhaCabecalho = leitura.Registros[0].Linha;

        for (int i = 0; i < cabecalho.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(cabecalho[i]))
                erros.Add(new ErroCsv(linhaCabecalho, $"coluna {i + 1} do cabeçalho está sem nome"));
        }

        var repetidos = cabecalho
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .GroupBy(n => n.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);
        foreach (var grupo in repetidos)
            erros.Add(new ErroCsv(linhaCabecalho, $"nome de coluna repetido no cabeçalho: \"{grupo.Key}\""));

        // linha com número de colunas diferente do cabeçalho é erro e fica fora das estatísticas,
        // senão os valores deslocados contaminariam o tipo e a contagem de cada coluna
        var linhasBoas = new List<Registro>();
        foreach (var registro in leitura.Registros.Skip(1))
        {
            if (registro.Campos.Count == cabecalho.Count)
                linhasBoas.Add(registro);
            else
                erros.Add(new ErroCsv(registro.Linha,
                    $"esperava {cabecalho.Count} colunas, encontrou {registro.Campos.Count}"));
        }

        var colunas = new List<RelatorioColuna>();
        for (int i = 0; i < cabecalho.Count; i++)
        {
            var valores = linhasBoas.Select(r => r.Campos[i]).ToList();
            int vazios = valores.Count(v => string.IsNullOrWhiteSpace(v));
            var preenchidos = valores.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            colunas.Add(new RelatorioColuna(cabecalho[i], preenchidos.Count, vazios, InferirTipo(preenchidos)));
        }

        return new Relatorio(cabecalho, leitura.Registros.Count - 1, colunas, Ordenar(erros));
    }

    // Só reconhece ponto como separador decimal ("1.5"). Vírgula decimal ("1,5") conflita com
    // o delimitador padrão e com separador de milhar, então esses valores caem em Texto.
    private static TipoColuna InferirTipo(List<string> preenchidos)
    {
        if (preenchidos.Count == 0) return TipoColuna.Vazia;

        var cultura = CultureInfo.InvariantCulture;
        if (preenchidos.All(v => long.TryParse(v.Trim(), NumberStyles.AllowLeadingSign, cultura, out _)))
            return TipoColuna.Inteiro;
        if (preenchidos.All(v => decimal.TryParse(v.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, cultura, out _)))
            return TipoColuna.Decimal;
        return TipoColuna.Texto;
    }

    private static List<ErroCsv> Ordenar(List<ErroCsv> erros) =>
        erros.OrderBy(e => e.Linha).ToList();
}
