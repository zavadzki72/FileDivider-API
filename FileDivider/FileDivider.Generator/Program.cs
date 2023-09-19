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
    sw.WriteLine($"LINHA {i} GERADA -> Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Vulputate mi sit amet mauris. Ullamcorper velit sed ullamcorper morbi. Lorem ipsum dolor sit amet consectetur adipiscing elit duis. In tellus integer feugiat scelerisque varius morbi enim nunc. Feugiat nisl pretium fusce id. Tincidunt dui ut ornare lectus. Integer quis auctor elit sed vulputate. Molestie at elementum eu facilisis sed odio morbi. Vulputate sapien nec sagittis aliquam malesuada bibendum arcu vitae elementum. Nunc lobortis mattis aliquam faucibus. Vulputate odio ut enim blandit volutpat maecenas volutpat.");
}

sw.Close();

Console.WriteLine($"Arquivo gerado!");

Console.WriteLine($"-- Execucao finalizada --");
Console.ReadLine();
