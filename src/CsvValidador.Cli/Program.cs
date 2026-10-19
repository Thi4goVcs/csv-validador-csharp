using System.Text;
using CsvValidador;

Console.OutputEncoding = Encoding.UTF8;

const int OK = 0, CSV_INVALIDO = 1, USO_INCORRETO = 2;

string? caminho = null;
char delimitador = ',';

for (int i = 0; i < args.Length; i++)
{
    if (args[i] is "-d" or "--delimitador")
    {
        if (i + 1 >= args.Length) return Uso("faltou o valor depois de " + args[i]);
        string valor = args[++i];
        if (valor is "tab" or "\\t") delimitador = '\t';
        else if (valor.Length == 1) delimitador = valor[0];
        else return Uso($"delimitador inválido: \"{valor}\" (use um caractere só, ou \"tab\")");
    }
    else if (args[i] is "-h" or "--help")
    {
        return Uso(null, OK);
    }
    else if (caminho is null)
    {
        caminho = args[i];
    }
    else
    {
        return Uso($"argumento sobrando: {args[i]}");
    }
}

if (caminho is null) return Uso("passa o caminho do arquivo CSV");

string texto;
try
{
    texto = File.ReadAllText(caminho);
}
catch (Exception e) when (e is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Não consegui ler \"{caminho}\": {e.Message}");
    return USO_INCORRETO;
}

Relatorio relatorio;
try
{
    relatorio = Validador.Validar(texto, delimitador);
}
catch (ArgumentException e)
{
    return Uso(e.Message);
}

Console.WriteLine($"Arquivo: {caminho}");
Console.WriteLine($"Linhas de dados: {relatorio.LinhasDeDados}");
Console.WriteLine($"Colunas: {relatorio.Cabecalho.Count}");

if (relatorio.Colunas.Count > 0)
{
    Console.WriteLine();
    int largura = Math.Max(6, relatorio.Colunas.Max(c => c.Nome.Length));
    Console.WriteLine($"{"coluna".PadRight(largura)}  {"tipo",-8}  {"preench.",8}  {"vazios",6}");
    foreach (var c in relatorio.Colunas)
        Console.WriteLine($"{c.Nome.PadRight(largura)}  {c.Tipo.ToString().ToLowerInvariant(),-8}  {c.Preenchidos,8}  {c.Vazios,6}");
}

Console.WriteLine();
if (relatorio.Valido)
{
    Console.WriteLine("Nenhum problema encontrado.");
    return OK;
}

Console.WriteLine($"{relatorio.Erros.Count} problema(s):");
foreach (var erro in relatorio.Erros)
    Console.WriteLine($"  linha {erro.Linha}: {erro.Mensagem}");
return CSV_INVALIDO;

static int Uso(string? problema, int codigo = USO_INCORRETO)
{
    if (problema is not null) Console.Error.WriteLine($"Erro: {problema}\n");
    var saida = codigo == OK ? Console.Out : Console.Error;
    saida.WriteLine("Uso: csv-validador <arquivo.csv> [-d <delimitador>]");
    saida.WriteLine("  -d, --delimitador   caractere separador (padrão ','; use \"tab\" para tabulação)");
    saida.WriteLine("Código de saída: 0 = ok, 1 = CSV com problemas, 2 = uso incorreto/arquivo ilegível");
    return codigo;
}
