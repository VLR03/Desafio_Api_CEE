using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Desafio_Api_CEE.Models
{
    public class Produto
    {
        [BsonId]

        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Nome")]
        public string? Nome { get; set; } = null;
    }
}