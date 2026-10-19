# csv-validador-csharp

Lê um arquivo CSV, diz se ele está bem formado e mostra um resumo de cada coluna (tipo, quantos valores preenchidos, quantos vazios). Se tiver problema, aponta a linha.

Primeiro projeto em C# do portfólio. O parser eu escrevi na mão em vez de usar uma biblioteca, porque CSV parece simples até aparecer vírgula dentro de aspas, aspas dentro de aspas e quebra de linha dentro de um campo. Era justamente isso que eu queria entender.

## Rodando

Precisa do .NET 8 SDK.

```bash
dotnet run --project src/CsvValidador.Cli -- exemplos/valido.csv
dotnet run --project src/CsvValidador.Cli -- exemplos/com-erros.csv
dotnet run --project src/CsvValidador.Cli -- arquivo.csv -d ";"   # outro delimitador
```

Saída com o `exemplos/com-erros.csv`:

```
Arquivo: exemplos/com-erros.csv
Linhas de dados: 2
Colunas: 4

coluna  tipo      preench.  vazios
id      inteiro          1       0
nome    texto            1       0
cidade  texto            1       0
nota    decimal          1       0

2 problema(s):
  linha 3: esperava 4 colunas, encontrou 3
  linha 4: aspas abertas e nunca fechadas
```

O código de saída é 0 se o CSV está ok, 1 se achou problema e 2 se o uso estava errado ou o arquivo não abriu. Dá pra usar num script ou num CI.

## Testes

```bash
dotnet test
```

32 testes com xUnit. Cobrem o parser (campo entre aspas, `""` como aspas escapada, quebra de linha dentro de campo citado, CRLF e LF misturados, BOM, delimitador diferente, linha em branco, aspas abertas sem fechar) e o validador (número de colunas errado, cabeçalho vazio ou repetido, inferência de tipo, ordem dos erros).

Também rodei contra o `titanic.csv` do meu [analise-titanic](https://github.com/Thi4goVcs/analise-titanic): 891 linhas, 15 colunas, nenhum erro, e os vazios batem com o que eu já tinha contado lá (177 idades, 688 conveses, 2 portos).

## Decisões que valem comentar

**A linha torta fica fora das estatísticas.** Se uma linha tem colunas a mais ou a menos, ela vira erro e não entra na contagem de tipo e de vazios. Se entrasse, os valores ficariam deslocados e uma coluna de números virava "texto" por causa de uma linha ruim.

**Um erro de formatação por registro.** Uma aspa fora do lugar costuma bagunçar o resto da linha, então reportar tudo geraria uma lista de erros que são consequência do primeiro. Só o primeiro aparece.

**O número da linha é a linha física do arquivo.** Um campo com quebra de linha dentro das aspas ocupa mais de uma linha, e o programa conta isso. Tem teste só pra esse caso, porque é o jeito mais fácil de errar a contagem.

## Limites que conheço

- Só reconhece ponto como decimal (`1.5`). Vírgula decimal (`1,5`) conflita com o delimitador padrão e com separador de milhar, então cai como texto. Com `-d ";"` em arquivo brasileiro isso incomoda, e eu não resolvi.
- Se aparece uma aspa aberta que nunca fecha, ela engole o resto do arquivo, e esse trecho todo é descartado. O erro aponta a linha onde a aspa abriu, mas o resumo das colunas só considera o que veio antes.
- Lê o arquivo inteiro na memória. Pra um CSV de vários GB, precisaria de leitura em stream.
- Só infere inteiro, decimal e texto. Não reconhece data, booleano nem e-mail.
