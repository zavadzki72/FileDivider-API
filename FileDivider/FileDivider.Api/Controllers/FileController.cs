using FileDivider.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

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
        [DisableRequestSizeLimit]
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

        [HttpPost("/divide/pdf/by-page")]
        [AllowAnonymous]
        public async Task<IActionResult> DivideFileByPage(IFormFile formFile, string fileName)
        {
            using var ms = new MemoryStream();
            await formFile.CopyToAsync(ms);
            var bytes = ms.ToArray();
            ms.Close();

            var pdfReader = PdfDocument.Open(bytes);

            if (pdfReader.NumberOfPages < 1)
            {
                return BadRequest($"O PDF não pode ser vazio.");
            }

            string fieldToFileName = "DEFAULT";

            if (!string.IsNullOrWhiteSpace(fileName) && fileName.Contains('{') && fileName.Contains('}'))
            {
                int pFrom = fileName.IndexOf("{")+1;
                int pTo = fileName.IndexOf("}");

                fieldToFileName = fileName[pFrom..pTo];
            }

            Dictionary<string, byte[]> files = new();
            int count = 1;

            foreach (var page in pdfReader.GetPages())
            {
                string pageName = $"Arquivo_{count}";
                if (!fieldToFileName.Equals("DEFAULT"))
                {
                    var pageText = ContentOrderTextExtractor.GetText(page);
                    if (pageText.Contains(fieldToFileName))
                    {
                        string parameterName = pageText.TryExtractFileName(fieldToFileName, pageName);
                        pageName = fileName.Replace(fieldToFileName, parameterName);
                        pageName = pageName.Replace("{", "").Replace("}", "");
                    }
                }

                var builder = new PdfDocumentBuilder();
                PdfPageBuilder pdfPage = builder.AddPage(page.Width, page.Height);
                pdfPage.CopyFrom(page);
                
                var byteArray = builder.Build();

                if (!files.TryAdd($"{pageName}.pdf", byteArray))
                {
                    pageName = $"{pageName}({count}).pdf";
                    files.Add(pageName, byteArray);
                }

                count++;
            }

            byte[] result = Array.Empty<byte>();

            using (MemoryStream zipArchiveMemoryStream = new())
            {
                using (ZipArchive zipArchive = new(zipArchiveMemoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        var finalFileName = file.Key;

                        ZipArchiveEntry zipEntry = zipArchive.CreateEntry(finalFileName);
                        using Stream entryStream = zipEntry.Open();

                        using (MemoryStream tmpMemory = new(file.Value))
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