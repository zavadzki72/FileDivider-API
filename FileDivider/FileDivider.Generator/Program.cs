Console.WriteLine("Informe o numero de linhas a serem geradas no arquivo: ");
var numberLinesStr = Console.ReadLine();

if(!int.TryParse(numberLinesStr, out int numberLines))
{
    Console.WriteLine("Numero de linhas invalido!");
    Console.ReadLine();

    return;
}

Console.WriteLine("Informe o diretorio em que o arquivos gerado sera salvo: ");
var destinationDir = Console.ReadLine()?.Trim();

if(string.IsNullOrWhiteSpace(destinationDir))
{
    Console.WriteLine($"O diretorio e invalido.");
    Console.ReadLine();

    return;
}

Console.WriteLine($"-- Começando execucao --");
Console.WriteLine($"Sera gerado um arquivo com {numberLines} linhas.");

var path = $@"{destinationDir}\generated_{numberLines}L_.txt";

var fileInfo = new FileInfo(path);
fileInfo.Directory?.Create();

using StreamWriter sw = File.CreateText(path);

for(int i=1; i<=numberLines; i++)
{
    sw.WriteLine($"LINHA {i} GERADA");
}

sw.Close();

Console.WriteLine($"Arquivo gerado!");

Console.WriteLine($"-- Execucao finalizada --");
Console.ReadLine();
