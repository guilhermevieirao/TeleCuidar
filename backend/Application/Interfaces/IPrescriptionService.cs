using Application.DTOs.Receitas;

namespace Application.Interfaces;

public interface IPrescriptionService
{
    Task<PrescriptionDto?> GetPrescriptionByIdAsync(Guid id);
    Task<List<PrescriptionDto>> GetPrescriptionsByAppointmentIdAsync(Guid appointmentId);
    Task<List<PrescriptionDto>> GetPrescriptionsByPatientIdAsync(Guid patientId);
    Task<List<PrescriptionDto>> GetPrescriptionsByProfessionalIdAsync(Guid professionalId);
    Task<PrescriptionDto> CreatePrescriptionAsync(CreatePrescriptionDto dto, Guid professionalId);
    Task<PrescriptionDto?> UpdatePrescriptionAsync(Guid id, UpdatePrescriptionDto dto);
    Task<PrescriptionDto?> AddItemAsync(Guid prescriptionId, AddPrescriptionItemDto dto);
    Task<PrescriptionDto?> UpdateItemAsync(Guid prescriptionId, string itemId, UpdatePrescriptionItemDto dto);
    Task<PrescriptionDto?> RemoveItemAsync(Guid prescriptionId, string itemId);
    Task<PrescriptionPdfDto> GeneratePdfAsync(Guid prescriptionId);
    Task<PrescriptionPdfDto> GenerateSignedPdfAsync(Guid prescriptionId, byte[] pfxBytes, string pfxPassword);
    Task<PrescriptionDto?> SignPrescriptionAsync(Guid prescriptionId, SignPrescriptionDto dto);
    Task<bool> ValidateDocumentHashAsync(string documentHash);
    Task DeletePrescriptionAsync(Guid id);
}
