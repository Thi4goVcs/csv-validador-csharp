using CsvValidador;

namespace CsvValidador.Tests;

public class LeitorCsvTests
{
    private static string[][] Campos(ResultadoLeitura r) =>
        r.Registros.Select(x => x.Campos.ToArray()).ToArray();

    [Fact]
    public void LeLinhasESeparaPorVirgula()
    {
        var r = LeitorCsv.Ler("a,b,c\n1,2,3\n");
        Assert.Empty(r.Erros);
        Assert.Equal(new[] { new[] { "a", "b", "c" }, new[] { "1", "2", "3" } }, Campos(r));
    }

    [Fact]
    public void AceitaCrlfELfMisturados()
    {
        var r = LeitorCsv.Ler("a,b\r\n1,2\n3,4\r\n");
        Assert.Empty(r.Erros);
        Assert.Equal(3, r.Registros.Count);
    }

    [Fact]
    public void CampoCitadoPodeTerDelimitador()
    {
        var r = LeitorCsv.Ler("nome,cidade\n\"Silva, Ana\",Recife\n");
        Assert.Empty(r.Erros);
        Assert.Equal("Silva, Ana", r.Registros[1].Campos[0]);
        Assert.Equal(2, r.Registros[1].Campos.Count);
    }

    [Fact]
    public void AspasDuplasDentroDeCampoCitadoViramUmaAspas()
    {
        var r = LeitorCsv.Ler("frase\n\"ele disse \"\"oi\"\"\"\n");
        Assert.Empty(r.Erros);
        Assert.Equal("ele disse \"oi\"", r.Registros[1].Campos[0]);
    }

    [Fact]
    public void CampoCitadoPodeTerQuebraDeLinha()
    {
        var r = LeitorCsv.Ler("a,b\n\"linha1\nlinha2\",x\n");
        Assert.Empty(r.Erros);
        Assert.Equal(2, r.Registros.Count);
        Assert.Equal("linha1\nlinha2", r.Registros[1].Campos[0]);
    }

    [Fact]
    public void NumeroDaLinhaContaQuebrasDentroDeCampoCitado()
    {
        // o registro "z" começa na linha 4 do arquivo, não na 3, por causa da quebra no campo citado
        var r = LeitorCsv.Ler("a\n\"x\ny\"\nz\n");
        Assert.Equal(new[] { 1, 2, 4 }, r.Registros.Select(x => x.Linha).ToArray());
    }

    [Fact]
    public void IgnoraLinhasEmBranco()
    {
        var r = LeitorCsv.Ler("a,b\n\n1,2\n\n");
        Assert.Empty(r.Erros);
        Assert.Equal(2, r.Registros.Count);
    }

    [Fact]
    public void UltimaLinhaSemQuebraFinalContaNormalmente()
    {
        var r = LeitorCsv.Ler("a,b\n1,2");
        Assert.Equal(2, r.Registros.Count);
        Assert.Equal("2", r.Registros[1].Campos[1]);
    }

    [Fact]
    public void CamposVaziosSaoPreservados()
    {
        var r = LeitorCsv.Ler("a,b,c\n1,,\n");
        Assert.Equal(new[] { "1", "", "" }, r.Registros[1].Campos.ToArray());
    }

    [Fact]
    public void CampoCitadoVazioNaoViraLinhaEmBranco()
    {
        var r = LeitorCsv.Ler("\"\"\n");
        Assert.Single(r.Registros);
        Assert.Equal("", r.Registros[0].Campos[0]);
    }

    [Fact]
    public void DelimitadorPersonalizado()
    {
        var r = LeitorCsv.Ler("a;b\n1,5;2\n", ';');
        Assert.Empty(r.Erros);
        Assert.Equal("1,5", r.Registros[1].Campos[0]);
    }

    [Fact]
    public void TabulacaoComoDelimitador()
    {
        var r = LeitorCsv.Ler("a\tb\n1\t2\n", '\t');
        Assert.Equal(2, r.Registros[0].Campos.Count);
    }

    [Fact]
    public void RemoveBomDoInicio()
    {
        var r = LeitorCsv.Ler("﻿id,nome\n1,x\n");
        Assert.Equal("id", r.Registros[0].Campos[0]);
    }

    [Fact]
    public void AspasAbertasSemFecharGeramErroNaLinhaOndeAbriram()
    {
        var r = LeitorCsv.Ler("a,b\n1,\"sem fim\n3,4\n");
        var erro = Assert.Single(r.Erros);
        Assert.Equal(2, erro.Linha);
        Assert.Contains("nunca fechadas", erro.Mensagem);
    }

    [Fact]
    public void AspasNoMeioDeCampoSemAspasGeramErro()
    {
        var r = LeitorCsv.Ler("a,b\nab\"c,2\n");
        var erro = Assert.Single(r.Erros);
        Assert.Equal(2, erro.Linha);
    }

    [Fact]
    public void TextoDepoisDeAspasFechadasGeraErro()
    {
        var r = LeitorCsv.Ler("a,b\n\"x\"y,2\n");
        var erro = Assert.Single(r.Erros);
        Assert.Equal(2, erro.Linha);
        Assert.Contains("depois das aspas", erro.Mensagem);
    }

    [Fact]
    public void TextoVazioNaoGeraRegistroNemErro()
    {
        var r = LeitorCsv.Ler("");
        Assert.Empty(r.Registros);
        Assert.Empty(r.Erros);
    }

    [Theory]
    [InlineData('"')]
    [InlineData('\n')]
    [InlineData('\r')]
    public void DelimitadorInvalidoLancaExcecao(char delimitador)
    {
        Assert.Throws<ArgumentException>(() => LeitorCsv.Ler("a,b", delimitador));
    }
}
