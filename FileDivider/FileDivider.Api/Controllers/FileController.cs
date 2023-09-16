using FileDivider.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace FileDivider.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : ControllerBase
    {
        public FileController()
        {
        }

        [HttpPost("/divide")]
        public async Task<IActionResult> DivideFile(IFormFile formFile, int numberLineToDivide)
        {
            if(numberLineToDivide < 2)
            {
                return BadRequest("O Numero de linhas precisa ser no minimo 100.");
            }

            var lines = await formFile.ReadAsList();
            lines = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            var linesChunck = lines
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / numberLineToDivide)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();

            List<byte[]> byteFiles = new();

            foreach(var lineChunck in linesChunck)
            {
                using var ms = new MemoryStream();
                using TextWriter tw = new StreamWriter(ms);

                var last = lineChunck.Last();
                foreach(var line in lineChunck)
                {
                    if(line.Equals(last))
                    {
                        tw.Write(line);
                    }
                    else
                    {
                        tw.WriteLine(line);
                    }

                }

                tw.Close();
                byteFiles.Add(ms.ToArray());

            }

            byte[] result = Array.Empty<byte>();
            int count = 1;

            using(MemoryStream zipArchiveMemoryStream = new())
            {
                using(ZipArchive zipArchive = new(zipArchiveMemoryStream, ZipArchiveMode.Create, true))
                {
                    foreach(var file in byteFiles)
                    {
                        var fileName = $"Arquivo_{count}.txt";

                        ZipArchiveEntry zipEntry = zipArchive.CreateEntry(fileName);
                        using Stream entryStream = zipEntry.Open();

                        using(MemoryStream tmpMemory = new(file))
                        {
                            tmpMemory.CopyTo(entryStream);
                        };

                        count++;
                    }
                }

                zipArchiveMemoryStream.Seek(0, SeekOrigin.Begin);
                result = zipArchiveMemoryStream.ToArray();
            }

            return File(result, "application/zip", $"FileDivider_{DateTime.Now:dd-MM-yyyy:HH:mm:ss}");
        }

        [HttpPost("/addRar")]
        public IActionResult AddRar(string dir)
        {
            string filePath = dir;
            string zipFileName = "teste.txt";

            byte[] result;

            using(MemoryStream zipArchiveMemoryStream = new())
            {
                using(ZipArchive zipArchive = new(zipArchiveMemoryStream, ZipArchiveMode.Create, true))
                {
                    ZipArchiveEntry zipEntry = zipArchive.CreateEntry(zipFileName);
                    using Stream entryStream = zipEntry.Open();

                    using(MemoryStream tmpMemory = new(System.IO.File.ReadAllBytes(filePath)))
                    {
                        tmpMemory.CopyTo(entryStream);
                    };
                }

                zipArchiveMemoryStream.Seek(0, SeekOrigin.Begin);
                result = zipArchiveMemoryStream.ToArray();
            }

            return File(result, "application/zip", "TESTE");
        }
    }
}