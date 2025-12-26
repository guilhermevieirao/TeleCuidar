using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Tipo de atestado médico
/// </summary>
public enum MedicalCertificateType
{
    Comparecimento,
    Afastamento,
    Aptidao,
    Acompanhante,
    Outro
}

/// <summary>
/// Representa um atestado médico emitido durante uma consulta
/// </summary>
public class MedicalCertificate : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid PatientId { get; set; }
    
    /// <summary>
    /// Tipo do atestado
    /// </summary>
    public MedicalCertificateType Tipo { get; set; }
    
    /// <summary>
    /// Data de emissão do atestado
    /// </summary>
    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data de início do afastamento (para atestados de afastamento)
    /// </summary>
    public DateTime? DataInicio { get; set; }
    
    /// <summary>
    /// Data de fim do afastamento (para atestados de afastamento)
    /// </summary>
    public DateTime? DataFim { get; set; }
    
    /// <summary>
    /// Número de dias de afastamento
    /// </summary>
    public int? DiasAfastamento { get; set; }
    
    /// <summary>
    /// Código CID (Classificação Internacional de Doenças)
    /// </summary>
    public string? Cid { get; set; }
    
    /// <summary>
    /// Conteúdo/texto do atestado
    /// </summary>
    public string Conteudo { get; set; } = string.Empty;
    
    /// <summary>
    /// Observações adicionais
    /// </summary>
    public string? Observacoes { get; set; }
    
    /// <summary>
    /// Assinatura digital em Base64 (ICP-Brasil)
    /// </summary>
    public string? DigitalSignature { get; set; }
    
    /// <summary>
    /// Thumbprint do certificado usado para assinatura
    /// </summary>
    public string? CertificateThumbprint { get; set; }
    
    /// <summary>
    /// Subject (CN) do certificado
    /// </summary>
    public string? CertificateSubject { get; set; }
    
    /// <summary>
    /// Data/hora da assinatura
    /// </summary>
    public DateTime? SignedAt { get; set; }
    
    /// <summary>
    /// Hash único do documento para validação
    /// </summary>
    public string? DocumentHash { get; set; }
    
    /// <summary>
    /// PDF assinado armazenado em Base64
    /// </summary>
    public string? SignedPdfBase64 { get; set; }
    
    // Navigation properties
    public Appointment Appointment { get; set; } = null!;
    public User Professional { get; set; } = null!;
    public User Patient { get; set; } = null!;
    
    /// <summary>
    /// Verifica se o atestado está assinado digitalmente
    /// </summary>
    public bool IsSigned => !string.IsNullOrEmpty(DigitalSignature) && SignedAt.HasValue;
}
