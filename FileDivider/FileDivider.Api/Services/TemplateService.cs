using FileDivider.Api.Data;
using FileDivider.Api.Models;
using MongoDB.Driver;

namespace FileDivider.Api.Services
{
    public class TemplateService
    {
        private readonly MongoContext _context;

        public TemplateService(MongoContext context)
        {
            _context = context;
        }

        public async Task<List<PdfTemplate>> GetAll()
        {
            return await _context.PdfTemplates.Find(_ => true).ToListAsync();
        }

        public async Task<PdfTemplate?> GetById(Guid id)
        {
            return await _context.PdfTemplates.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(PdfTemplate item)
        {
            await _context.PdfTemplates.InsertOneAsync(item);
        }

        public async Task Update(Guid id, PdfTemplate updatedItem)
        {
            updatedItem.Id = id;
            var result = await _context.PdfTemplates.ReplaceOneAsync(x => x.Id == id, updatedItem);

            if (result.MatchedCount == 0)
                throw new ArgumentException($"Item com ID '{id}' não encontrado.");
        }

        public async Task Delete(Guid id)
        {
            var result = await _context.PdfTemplates.DeleteOneAsync(x => x.Id == id);

            if (result.DeletedCount == 0)
                throw new ArgumentException($"Item com ID '{id}' não encontrado.");
        }
    }
}
