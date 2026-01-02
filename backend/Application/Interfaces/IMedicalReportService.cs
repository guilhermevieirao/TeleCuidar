using Application.DTOs.MedicalReports;

namespace Application.Interfaces;

/// <summary>
/// Interface para serviço de laudos médicos
/// </summary>
public interface IMedicalReportService
{
    /// <summary>
    /// Obtém um laudo por ID
    /// </summary>
    Task<MedicalReportDto?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Lista todos os laudos de uma consulta
    /// </summary>
    Task<List<MedicalReportDto>> GetByAppointmentIdAsync(Guid appointmentId);
    
    /// <summary>
    /// Lista todos os laudos de um paciente
    /// </summary>
    Task<List<MedicalReportDto>> GetByPatientIdAsync(Guid patientId);
    
    /// <summary>
    /// Lista todos os laudos emitidos por um profissional
    /// </summary>
    Task<List<MedicalReportDto>> GetByProfessionalIdAsync(Guid professionalId);
    
    /// <summary>
    /// Cria um novo laudo
    /// </summary>
    Task<MedicalReportDto> CreateAsync(CreateMedicalReportDto dto, Guid professionalId);
    
    /// <summary>
    /// Atualiza um laudo existente
    /// </summary>
    Task<MedicalReportDto?> UpdateAsync(Guid id, UpdateMedicalReportDto dto, Guid professionalId);
    
    /// <summary>
    /// Remove um laudo
    /// </summary>
    Task<bool> DeleteAsync(Guid id, Guid professionalId);
    
    /// <summary>
    /// Gera o PDF do laudo
    /// </summary>
    Task<MedicalReportPdfDto> GeneratePdfAsync(Guid id);
    
    /// <summary>
    /// Valida um documento pelo hash
    /// </summary>
    Task<MedicalReportDto?> ValidateByHashAsync(string hash);
}
