using Application.DTOs.Receitas;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _prescriptionService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICertificateStorageService _certificateStorageService;

    public PrescriptionsController(
        IPrescriptionService prescriptionService, 
        IAuditLogService auditLogService,
        ICertificateStorageService certificateStorageService)
    {
        _prescriptionService = prescriptionService;
        _auditLogService = auditLogService;
        _certificateStorageService = certificateStorageService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PrescriptionDto>> GetPrescription(Guid id)
    {
        var prescription = await _prescriptionService.GetPrescriptionByIdAsync(id);
        if (prescription == null)
            return NotFound();

        return Ok(prescription);
    }

    [HttpGet("appointment/{appointmentId}")]
    public async Task<ActionResult<List<PrescriptionDto>>> GetPrescriptionsByAppointment(Guid appointmentId)
    {
        var prescriptions = await _prescriptionService.GetPrescriptionsByAppointmentIdAsync(appointmentId);
        return Ok(prescriptions);
    }

    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<List<PrescriptionDto>>> GetPrescriptionsByPatient(Guid patientId)
    {
        var prescriptions = await _prescriptionService.GetPrescriptionsByPatientIdAsync(patientId);
        return Ok(prescriptions);
    }

    [HttpGet("professional/{professionalId}")]
    public async Task<ActionResult<List<PrescriptionDto>>> GetPrescriptionsByProfessional(Guid professionalId)
    {
        var prescriptions = await _prescriptionService.GetPrescriptionsByProfessionalIdAsync(professionalId);
        return Ok(prescriptions);
    }

    [HttpPost]
    public async Task<ActionResult<PrescriptionDto>> CreatePrescription([FromBody] CreatePrescriptionDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var prescription = await _prescriptionService.CreatePrescriptionAsync(dto, userId.Value);

            // Audit log
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "create",
                "Prescription",
                prescription.Id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { prescription.AppointmentId, prescription.Items.Count }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            return CreatedAtAction(nameof(GetPrescription), new { id = prescription.Id }, prescription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<PrescriptionDto>> UpdatePrescription(Guid id, [FromBody] UpdatePrescriptionDto dto)
    {
        try
        {
            var prescription = await _prescriptionService.UpdatePrescriptionAsync(id, dto);
            if (prescription == null)
                return NotFound();

            // Audit log
            await _auditLogService.CreateAuditLogAsync(
                GetCurrentUserId(),
                "update",
                "Prescription",
                id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { ItemsCount = dto.Items.Count }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            return Ok(prescription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/items")]
    public async Task<ActionResult<PrescriptionDto>> AddItem(Guid id, [FromBody] AddPrescriptionItemDto dto)
    {
        try
        {
            var prescription = await _prescriptionService.AddItemAsync(id, dto);
            if (prescription == null)
                return NotFound();

            return Ok(prescription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}/items/{itemId}")]
    public async Task<ActionResult<PrescriptionDto>> RemoveItem(Guid id, string itemId)
    {
        try
        {
            var prescription = await _prescriptionService.RemoveItemAsync(id, itemId);
            if (prescription == null)
                return NotFound();

            return Ok(prescription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/items/{itemId}")]
    public async Task<ActionResult<PrescriptionDto>> UpdateItem(Guid id, string itemId, [FromBody] UpdatePrescriptionItemDto dto)
    {
        try
        {
            var prescription = await _prescriptionService.UpdateItemAsync(id, itemId, dto);
            if (prescription == null)
                return NotFound();

            return Ok(prescription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/pdf")]
    public async Task<ActionResult<PrescriptionPdfDto>> GeneratePdf(Guid id)
    {
        try
        {
            var pdf = await _prescriptionService.GeneratePdfAsync(id);
            return Ok(pdf);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/pdf/signed")]
    public async Task<ActionResult<PrescriptionPdfDto>> GenerateSignedPdf(Guid id, [FromBody] GenerateSignedPdfDto dto)
    {
        try
        {
            var pfxBytes = Convert.FromBase64String(dto.PfxBase64);
            var pdf = await _prescriptionService.GenerateSignedPdfAsync(id, pfxBytes, dto.PfxPassword);
            
            // Audit log
            await _auditLogService.CreateAuditLogAsync(
                GetCurrentUserId(),
                "sign_pdf",
                "Prescription",
                id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { SignedAt = DateTime.UtcNow }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );
            
            return Ok(pdf);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Erro ao assinar PDF: {ex.Message}" });
        }
    }

    [HttpPost("{id}/pdf/sign-saved")]
    public async Task<ActionResult<PrescriptionPdfDto>> SignWithSavedCert(Guid id, [FromBody] SignWithSavedCertDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            // Obter certificado para assinatura
            var (pfxBytes, password) = await _certificateStorageService.GetCertificateForSigningAsync(
                userId.Value,
                dto.CertificateId,
                dto.Password
            );

            // Usar o serviço existente com os bytes do certificado
            var pdf = await _prescriptionService.GenerateSignedPdfAsync(id, pfxBytes, password);
            
            // Audit log
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "sign_pdf_saved_cert",
                "Prescription",
                id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { CertificateId = dto.CertificateId, SignedAt = DateTime.UtcNow }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );
            
            return Ok(pdf);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Erro ao gerar PDF assinado: {ex.Message}" });
        }
    }

    [HttpPost("{id}/sign")]
    public async Task<ActionResult<PrescriptionDto>> SignPrescription(Guid id, [FromBody] SignPrescriptionDto dto)
    {
        try
        {
            var prescription = await _prescriptionService.SignPrescriptionAsync(id, dto);
            if (prescription == null)
                return NotFound();

            // Audit log
            await _auditLogService.CreateAuditLogAsync(
                GetCurrentUserId(),
                "sign",
                "Prescription",
                id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { dto.CertificateSubject, SignedAt = DateTime.UtcNow }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            return Ok(prescription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("validate/{documentHash}")]
    [AllowAnonymous]
    public async Task<ActionResult> ValidateDocument(string documentHash)
    {
        var isValid = await _prescriptionService.ValidateDocumentHashAsync(documentHash);
        return Ok(new { isValid, documentHash });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePrescription(Guid id)
    {
        try
        {
            var prescription = await _prescriptionService.GetPrescriptionByIdAsync(id);
            if (prescription == null)
                return NotFound();

            await _prescriptionService.DeletePrescriptionAsync(id);

            // Audit log
            await _auditLogService.CreateAuditLogAsync(
                GetCurrentUserId(),
                "delete",
                "Prescription",
                id.ToString(),
                HttpContextExtensions.SerializeToJson(new { prescription.AppointmentId }),
                null,
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
