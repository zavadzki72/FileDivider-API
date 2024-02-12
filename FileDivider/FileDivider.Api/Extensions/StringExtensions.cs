using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using static UglyToad.PdfPig.Core.PdfSubpath;

namespace FileDivider.Api.Extensions
{
    public static class StringExtensions
    {
        public static string TryExtractFileName(this string source, string field, string nameRefer)
        {
            if(string.IsNullOrWhiteSpace(source))
                return nameRefer;

            var split = source.Split(field);
            string newPageName = split.Last();
            newPageName = Regex.Replace(newPageName, "^[^\\w]*", "", RegexOptions.Singleline);
            newPageName = Regex.Replace(newPageName, "\\w+:.*", "", RegexOptions.Singleline);

            string pageName = newPageName.Trim();
            pageName = pageName.Replace(" ", "_");
            pageName = pageName.RemoveDiacritics();

            if(IsName(pageName))
            {
                return pageName;
            }

            var splitByLine = source.Split("\n");
            var lineWithField = splitByLine.First(x => x.Contains(field));

            newPageName = Regex.Replace(lineWithField, "^[^\\w]*", "", RegexOptions.Singleline);
            newPageName = Regex.Replace(newPageName, "\\w+:.*", "", RegexOptions.Singleline);

            if (IsName(newPageName))
            {
                newPageName = newPageName.Replace(" ", "_");
                newPageName = newPageName.RemoveDiacritics();
                return newPageName;
            }

            splitByLine = source.Split("\n");
            var itemIndex = splitByLine.Select((v, i) => new { item = v, index = i }).First(x => x.item.Contains(field)).index;
            var newList = splitByLine.Select((v, i) => new { item = v, index = i }).Where(x => x.index > itemIndex);

            foreach(var item in newList.Select(x => x.item))
            {
                var regex = new Regex("[A-Z\\s]+");
                var names = regex.Matches(item);
                var name = names.FirstOrDefault();

                if (name != null)
                {
                    newPageName = name.Value.Trim();

                    if (IsName(newPageName))
                    {
                        newPageName = newPageName.Replace(" ", "_");
                        newPageName = newPageName.RemoveDiacritics();
                        return newPageName;
                    }
                }
            }

            return nameRefer;
        }

        public static string RemoveDiacritics(this string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }

        private static bool IsName(string value)
        {
            return (value.Length > 5 && value.Length < 50 && value.All(x => char.IsLetter(x) || x.Equals(' ')));
        }
    }
}
