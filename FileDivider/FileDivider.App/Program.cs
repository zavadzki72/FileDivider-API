using System.Diagnostics;

Console.WriteLine("Informe o numero de linhas usado para quebrar em arquivos (Minimo de 100): ");
var numberLinesStr = Console.ReadLine();

if(!int.TryParse(numberLinesStr, out int numberLines) || numberLines < 100){
    Console.WriteLine("Numero de linhas invalido!");
    Console.ReadLine();

    return;
}

Console.WriteLine("Informe o diretorio do arquivo base (Incluindo o arquivo exemplo C:/Usuario/Textos/Arquivo.txt): ");
var baseDir = Console.ReadLine();

if(!File.Exists(baseDir?.Trim()))
{
    Console.WriteLine($"Arquivo nao encontrado no diretorio {baseDir}.");
    Console.ReadLine();

    return;
}


Console.WriteLine("Informe o diretorio em que os arquivos gerados serao salvos: ");
var destinationDir = Console.ReadLine()?.Trim();

if(string.IsNullOrWhiteSpace(destinationDir))
{
    Console.WriteLine($"O diretorio destino e invalido.");
    Console.ReadLine();

    return;
}

var lines = File.ReadAllLines(baseDir);

if(lines.Length <= numberLines)
{
    Console.WriteLine($"O Arquivo nao contem linhas o suficiente para quebrar.");
    Console.ReadLine();

    return;
}

lines = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

var linesChunck = lines
    .Select((x, i) => new { Index = i, Value = x })
    .GroupBy(x => x.Index / numberLines)
    .Select(x => x.Select(v => v.Value).ToList())
    .ToList();

var timer = new Stopwatch();
timer.Start();

Console.WriteLine($"-- Começando execucao --");
Console.WriteLine($"Serao gerados no total {linesChunck.Count} arquivos.");

int count = 1;

foreach(var lineChunck in linesChunck)
{
    var fileName = $"Arquivo_{count}.txt";
    var path = $@"{destinationDir}\{fileName}";

    var fileInfo = new FileInfo(path);
    fileInfo.Directory?.Create();

    using StreamWriter sw = File.CreateText(path);

    Console.WriteLine($"-> Gerando arquivo: {fileName}");

    foreach(var line in lineChunck)
    {
        sw.WriteLine(line);
    }

    sw.Close();

    count++;
    Console.WriteLine($"-> Arquivo {fileName} gerado!");
}

Console.WriteLine($"-- Execucao finalizada --");

timer.Stop();

var time = new DateTime(timer.ElapsedTicks).ToString("HH:mm:ss:fff");
Console.WriteLine($"Tempo de execucao: {time}");
Console.ReadLine();
