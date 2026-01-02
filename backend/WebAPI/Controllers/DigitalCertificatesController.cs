using Application.DTOs.Certificates;
using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DigitalCertificatesController : ControllerBase
{
    private readonly IDigitalCertificateService _certificateService;
    private readonly IAuditLogService _auditLogService;
    private readonly ApplicationDbContext _context;

    public DigitalCertificatesController(
        IDigitalCertificateService certificateService,
        IAuditLogService auditLogService,
        ApplicationDbContext context)
    {
        _certificateService = certificateService;
        _auditLogService = auditLogService;
        _context = context;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }

    /// <summary>
    /// Lista todos os certificados do usuário atual
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DigitalCertificateDto>>> GetMyCertificates()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var certificates = await _certificateService.GetCertificatesByUserIdAsync(userId.Value);
        return Ok(certificates);
    }

    /// <summary>
    /// Obtém um certificado específico por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DigitalCertificateDto>> GetCertificate(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var certificate = await _certificateService.GetCertificateByIdAsync(id, userId.Value);
        if (certificate == null)
            return NotFound();

        return Ok(certificate);
    }

    /// <summary>
    /// Valida um certificado PFX sem salvar
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<CertificateValidationResult>> ValidateCertificate([FromBody] ValidateCertificateDto dto)
    {
        var result = await _certificateService.ValidateCertificateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Salva um novo certificado digital
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DigitalCertificateDto>> SaveCertificate([FromBody] SaveCertificateDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var certificate = await _certificateService.SaveCertificateAsync(dto, userId.Value);

            // Audit log
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "create",
                "DigitalCertificate",
                certificate.Id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { certificate.DisplayName, certificate.Thumbprint }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            return CreatedAtAction(nameof(GetCertificate), new { id = certificate.Id }, certificate);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza um certificado existente
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<ActionResult<DigitalCertificateDto>> UpdateCertificate(Guid id, [FromBody] UpdateCertificateDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var certificate = await _certificateService.UpdateCertificateAsync(id, dto, userId.Value);
            if (certificate == null)
                return NotFound();

            // Audit log
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "update",
                "DigitalCertificate",
                id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(dto),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            return Ok(certificate);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove um certificado
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCertificate(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var deleted = await _certificateService.DeleteCertificateAsync(id, userId.Value);
        if (!deleted)
            return NotFound();

        // Audit log
        await _auditLogService.CreateAuditLogAsync(
            userId,
            "delete",
            "DigitalCertificate",
            id.ToString(),
            null,
            null,
            HttpContext.GetIpAddress(),
            HttpContext.GetUserAgent()
        );

        return NoContent();
    }

    /// <summary>
    /// Assina um documento (receita ou atestado)
    /// </summary>
    [HttpPost("sign")]
    public async Task<ActionResult<SignDocumentResult>> SignDocument([FromBody] SignDocumentRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _certificateService.SignDocumentAsync(dto, userId.Value);

        if (result.Success)
        {
            // Audit log
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "sign",
                dto.DocumentType,
                dto.DocumentId.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { result.DocumentHash, result.CertificateSubject }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );
        }

        return Ok(result);
    }

    /// <summary>
    /// Salva um certificado e assina um documento em uma operação
    /// </summary>
    [HttpPost("save-and-sign")]
    public async Task<ActionResult<SaveAndSignResultDto>> SaveCertificateAndSign([FromBody] SaveCertificateAndSignDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var (certificate, signResult) = await _certificateService.SaveCertificateAndSignAsync(dto, userId.Value);

            // Audit logs
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "create",
                "DigitalCertificate",
                certificate.Id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { certificate.DisplayName, certificate.Thumbprint }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            if (signResult.Success)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId,
                    "sign",
                    dto.DocumentType,
                    dto.DocumentId.ToString(),
                    null,
                    HttpContextExtensions.SerializeToJson(new { signResult.DocumentHash, signResult.CertificateSubject }),
                    HttpContext.GetIpAddress(),
                    HttpContext.GetUserAgent()
                );
            }

            return Ok(new SaveAndSignResultDto
            {
                Certificate = certificate,
                SignResult = signResult
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Download do PDF assinado de uma receita
    /// </summary>
    [HttpGet("prescription/{id}/signed-pdf")]
    public async Task<IActionResult> DownloadSignedPrescriptionPdf(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var prescription = await _context.Prescriptions
            .Include(p => p.Patient)
            .Where(p => p.Id == id && (p.ProfessionalId == userId || p.PatientId == userId))
            .Select(p => new { p.SignedPdfBase64, p.CreatedAt, PatientName = p.Patient!.Name + " " + p.Patient!.LastName })
            .FirstOrDefaultAsync();

        if (prescription == null)
            return NotFound(new { message = "Receita não encontrada" });

        if (string.IsNullOrEmpty(prescription.SignedPdfBase64))
            return BadRequest(new { message = "Esta receita ainda não foi assinada digitalmente" });

        var pdfBytes = Convert.FromBase64String(prescription.SignedPdfBase64);
        var fileName = PdfDocumentHelper.GenerateFileName("Receita", prescription.PatientName, prescription.CreatedAt, "Assinada");
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Download do PDF assinado de um atestado
    /// </summary>
    [HttpGet("certificate/{id}/signed-pdf")]
    public async Task<IActionResult> DownloadSignedMedicalCertificatePdf(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var medCert = await _context.MedicalCertificates
            .Include(c => c.Patient)
            .Where(c => c.Id == id && (c.ProfessionalId == userId || c.PatientId == userId))
            .Select(c => new { c.SignedPdfBase64, c.CreatedAt, PatientName = c.Patient!.Name + " " + c.Patient!.LastName })
            .FirstOrDefaultAsync();

        if (medCert == null)
            return NotFound(new { message = "Atestado não encontrado" });

        if (string.IsNullOrEmpty(medCert.SignedPdfBase64))
            return BadRequest(new { message = "Este atestado ainda não foi assinado digitalmente" });

        var pdfBytes = Convert.FromBase64String(medCert.SignedPdfBase64);
        var fileName = PdfDocumentHelper.GenerateFileName("Atestado", medCert.PatientName, medCert.CreatedAt, "Assinado");
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Download do PDF assinado de uma solicitação de exame
    /// </summary>
    [HttpGet("exam/{id}/signed-pdf")]
    public async Task<IActionResult> DownloadSignedExamRequestPdf(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var exam = await _context.ExamRequests
            .Include(e => e.Patient)
            .Where(e => e.Id == id && (e.ProfessionalId == userId || e.PatientId == userId))
            .Select(e => new { e.SignedPdfBase64, e.CreatedAt, PatientName = e.Patient!.Name + " " + e.Patient!.LastName })
            .FirstOrDefaultAsync();

        if (exam == null)
            return NotFound(new { message = "Solicitação de exame não encontrada" });

        if (string.IsNullOrEmpty(exam.SignedPdfBase64))
            return BadRequest(new { message = "Esta solicitação de exame ainda não foi assinada digitalmente" });

        var pdfBytes = Convert.FromBase64String(exam.SignedPdfBase64);
        var fileName = PdfDocumentHelper.GenerateFileName("Exame", exam.PatientName, exam.CreatedAt, "Assinado");
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Download do PDF assinado de um laudo
    /// </summary>
    [HttpGet("report/{id}/signed-pdf")]
    public async Task<IActionResult> DownloadSignedMedicalReportPdf(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var report = await _context.MedicalReports
            .Include(r => r.Patient)
            .Where(r => r.Id == id && (r.ProfessionalId == userId || r.PatientId == userId))
            .Select(r => new { r.SignedPdfBase64, r.CreatedAt, PatientName = r.Patient!.Name + " " + r.Patient!.LastName })
            .FirstOrDefaultAsync();

        if (report == null)
            return NotFound(new { message = "Laudo não encontrado" });

        if (string.IsNullOrEmpty(report.SignedPdfBase64))
            return BadRequest(new { message = "Este laudo ainda não foi assinado digitalmente" });

        var pdfBytes = Convert.FromBase64String(report.SignedPdfBase64);
        var fileName = PdfDocumentHelper.GenerateFileName("Laudo", report.PatientName, report.CreatedAt, "Assinado");
        return File(pdfBytes, "application/pdf", fileName);
    }
}

/// <summary>
/// DTO para resultado de salvar e assinar
/// </summary>
public class SaveAndSignResultDto
{
    public DigitalCertificateDto Certificate { get; set; } = null!;
    public SignDocumentResult SignResult { get; set; } = null!;
}
