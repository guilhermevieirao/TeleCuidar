using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Tipo de laudo médico
/// </summary>
public enum MedicalReportType
{
    /// <summary>
    /// Laudo de exame
    /// </summary>
    Exame,
    
    /// <summary>
    /// Laudo de procedimento
    /// </summary>
    Procedimento,
    
    /// <summary>
    /// Laudo pericial
    /// </summary>
    Pericial,
    
    /// <summary>
    /// Laudo de avaliação
    /// </summary>
    Avaliacao,
    
    /// <summary>
    /// Parecer técnico
    /// </summary>
    ParecerTecnico,
    
    /// <summary>
    /// Laudo complementar
    /// </summary>
    Complementar,
    
    /// <summary>
    /// Outro tipo
    /// </summary>
    Outro
}

/// <summary>
/// Representa um laudo médico emitido durante uma consulta
/// </summary>
public class MedicalReport : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid PatientId { get; set; }
    
    /// <summary>
    /// Tipo do laudo
    /// </summary>
    public MedicalReportType Tipo { get; set; } = MedicalReportType.Exame;
    
    /// <summary>
    /// Título do laudo
    /// </summary>
    public string Titulo { get; set; } = string.Empty;
    
    /// <summary>
    /// Data de emissão do laudo
    /// </summary>
    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Histórico clínico / Anamnese resumida
    /// </summary>
    public string? HistoricoClinico { get; set; }
    
    /// <summary>
    /// Exame físico / Achados do exame
    /// </summary>
    public string? ExameFisico { get; set; }
    
    /// <summary>
    /// Exames complementares realizados
    /// </summary>
    public string? ExamesComplementares { get; set; }
    
    /// <summary>
    /// Hipótese diagnóstica
    /// </summary>
    public string? HipoteseDiagnostica { get; set; }
    
    /// <summary>
    /// Código CID-10
    /// </summary>
    public string? Cid { get; set; }
    
    /// <summary>
    /// Conclusão do laudo
    /// </summary>
    public string Conclusao { get; set; } = string.Empty;
    
    /// <summary>
    /// Recomendações / Conduta
    /// </summary>
    public string? Recomendacoes { get; set; }
    
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
    /// Indica se o documento está assinado digitalmente
    /// </summary>
    public bool IsSigned => !string.IsNullOrEmpty(DigitalSignature);
}
