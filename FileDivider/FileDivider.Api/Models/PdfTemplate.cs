using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace FileDivider.Api.Models
{
    public class PdfTemplate
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("extraction_helper")]
        public Dictionary<string, string> ExtractionHelper { get; set; } = [];

        public (bool, string) IsValid()
        {
            if(Name.Length < 3)
                return (false, "O nome do template precisa ter no minimo 3 caracteres");

            if(ExtractionHelper.Count < 2)
                return (false, "O template precisa ter pelo menos dois helpers de extração");

            if(!ExtractionHelper.ContainsKey(ExtractionHelperMandatoryValues.StartRegex))
                return (false, "O template precisa ter um helper de extração chamado 'Inicio'");

            return (true, string.Empty);
        }
    }

    public static class ExtractionHelperMandatoryValues
    {
        public static string StartRegex = "Inicio";
    }
}
