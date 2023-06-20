using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Desafio_Api_CEE.Models
{
    public class Produto
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? NumeroCartao { get; set; }
        public string? Conta { get; set; }
        public string? Agencia { get; set; }
        public string? Cpf { get; set; }
        public DateTime? DataNasc { get; set; }
        public string? NomeCompleto { get; set; }
        public string? NomeCartao { get; set; }
        public string? Bandeira { get; set; }
        public string? Tipo { get; set; }
        public string? DataVenc { get; set; }
        public string? Cvv { get; set; }
        public string? Limite { get; set; }
        public string? Senha { get; set; }
        public string? SenhaConfirm { get; set; }
        public string? Status { get; set; } = "SOLICITADO";
    }
}
