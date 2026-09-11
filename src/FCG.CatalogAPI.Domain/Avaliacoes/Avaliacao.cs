using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FCG.CatalogAPI.Domain.Avaliacoes;

/// <summary>
/// Documento MongoDB que representa a avaliação de um jogo por um usuário.
/// </summary>
public class Avaliacao
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("JogoId")]
    public Guid JogoId { get; set; }

    [BsonElement("UsuarioId")]
    public Guid UsuarioId { get; set; }

    [BsonElement("NomeUsuario")]
    public string NomeUsuario { get; set; } = string.Empty;

    [BsonElement("Nota")]
    public int Nota { get; set; }

    [BsonElement("Comentario")]
    public string Comentario { get; set; } = string.Empty;

    [BsonElement("CriadoEm")]
    public DateTime CriadoEm { get; set; }

    [BsonElement("AtualizadoEm")]
    [BsonIgnoreIfNull]
    public DateTime? AtualizadoEm { get; set; }

    public static Avaliacao Criar(Guid jogoId, Guid usuarioId, string nomeUsuario, int nota, string comentario)
    {
        if (nota < 1 || nota > 5)
            throw new ArgumentException("A nota deve ser entre 1 e 5.");
        if (string.IsNullOrWhiteSpace(comentario))
            throw new ArgumentException("O comentário é obrigatório.");

        return new Avaliacao
        {
            JogoId = jogoId,
            UsuarioId = usuarioId,
            NomeUsuario = nomeUsuario,
            Nota = nota,
            Comentario = comentario.Trim(),
            CriadoEm = DateTime.UtcNow
        };
    }
}
