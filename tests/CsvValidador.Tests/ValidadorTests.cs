using CsvValidador;

namespace CsvValidador.Tests;

public class ValidadorTests
{
    [Fact]
    public void CsvBemFormadoEhValido()
    {
        var r = Validador.Validar("id,nome\n1,Ana\n2,Bia\n");
        Assert.True(r.Valido);
        Assert.Equal(2, r.LinhasDeDados);
        Assert.Equal(2, r.Colunas.Count);
    }

    [Fact]
    public void ArquivoVazioEhInvalido()
    {
        var r = Validador.Validar("");
        Assert.False(r.Valido);
        Assert.Contains("sem nenhuma linha", r.Erros[0].Mensagem);
    }

    [Fact]
    public void ApenasCabecalhoEhValidoComZeroLinhasDeDados()
    {
        var r = Validador.Validar("id,nome\n");
        Assert.True(r.Valido);
        Assert.Equal(0, r.LinhasDeDados);
        Assert.All(r.Colunas, c => Assert.Equal(TipoColuna.Vazia, c.Tipo));
    }

    [Fact]
    public void LinhaComColunaASmaisEhReportadaComNumeroDaLinha()
    {
        var r = Validador.Validar("a,b,c\n1,2,3\n4,5\n6,7,8\n");
        var erro = Assert.Single(r.Erros);
        Assert.Equal(3, erro.Linha);
        Assert.Equal("esperava 3 colunas, encontrou 2", erro.Mensagem);
    }

    [Fact]
    public void LinhaComColunaDeMenosOuDeMaisSaiDasEstatisticas()
    {
        // se a linha torta entrasse, "x" contaminaria a coluna de números e viraria Texto
        var r = Validador.Validar("n,m\n1,2\nx,y,z\n3,4\n");
        Assert.Equal(TipoColuna.Inteiro, r.Colunas[0].Tipo);
        Assert.Equal(2, r.Colunas[0].Preenchidos);
    }

    [Fact]
    public void CabecalhoComNomeVazioEhErro()
    {
        var r = Validador.Validar("a,,c\n1,2,3\n");
        var erro = Assert.Single(r.Erros);
        Assert.Contains("coluna 2", erro.Mensagem);
    }

    [Fact]
    public void CabecalhoComNomeRepetidoEhErroSemDiferenciarMaiusculas()
    {
        var r = Validador.Validar("Id,nome,ID\n1,a,2\n");
        var erro = Assert.Single(r.Erros);
        Assert.Contains("repetido", erro.Mensagem);
    }

    [Fact]
    public void InfereInteiroDecimalETexto()
    {
        var r = Validador.Validar("i,d,t\n1,1.5,a\n-2,3,b\n+7,0.25,3\n");
        Assert.Equal(TipoColuna.Inteiro, r.Colunas[0].Tipo);
        Assert.Equal(TipoColuna.Decimal, r.Colunas[1].Tipo);
        Assert.Equal(TipoColuna.Texto, r.Colunas[2].Tipo);
    }

    [Fact]
    public void VirgulaDecimalNaoContaComoNumero()
    {
        var r = Validador.Validar("preco;x\n1,5;a\n", ';');
        Assert.Equal(TipoColuna.Texto, r.Colunas[0].Tipo);
    }

    [Fact]
    public void ValoresVaziosNaoQuebramATipagemEViramContagem()
    {
        var r = Validador.Validar("n\n1\n\"\"\n3\n");
        Assert.Equal(TipoColuna.Inteiro, r.Colunas[0].Tipo);
        Assert.Equal(2, r.Colunas[0].Preenchidos);
        Assert.Equal(1, r.Colunas[0].Vazios);
    }

    [Fact]
    public void ErrosVemOrdenadosPorLinha()
    {
        // erro de aspas (leitor) e erro de colunas (validador) chegam por caminhos diferentes
        var r = Validador.Validar("a,b\n1\nx\"y,2\n3,4,5\n");
        Assert.Equal(new[] { 2, 3, 4 }, r.Erros.Select(e => e.Linha).ToArray());
    }

    [Fact]
    public void ErroDeAspasAbertasNaoImpedeRelatorioDoQueFoiLidoAntes()
    {
        var r = Validador.Validar("a,b\n1,2\n3,\"sem fim\n");
        Assert.Equal(1, r.LinhasDeDados);
        Assert.False(r.Valido);
    }
}
