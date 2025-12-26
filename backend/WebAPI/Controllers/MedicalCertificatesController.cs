using Application.DTOs.MedicalCertificates;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/medical-certificates")]
[Authorize]
public class MedicalCertificatesController : ControllerBase
{
    private readonly IMedicalCertificateService _certificateService;
    private readonly IAuditLogService _auditLogService;

    public MedicalCertificatesController(
        IMedicalCertificateService certificateService,
        IAuditLogService auditLogService)
    {
        _certificateService = certificateService;
        _auditLogService = auditLogService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }

    /// <summary>
    /// Obtém um atestado por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MedicalCertificateDto>> GetById(Guid id)
    {
        var certificate = await _certificateService.GetByIdAsync(id);
        if (certificate == null)
            return NotFound();

        return Ok(certificate);
    }

    /// <summary>
    /// Obtém todos os atestados de uma consulta
    /// </summary>
    [HttpGet("appointment/{appointmentId}")]
    public async Task<ActionResult<List<MedicalCertificateDto>>> GetByAppointment(Guid appointmentId)
    {
        var certificates = await _certificateService.GetByAppointmentIdAsync(appointmentId);
        return Ok(certificates);
    }

    /// <summary>
    /// Obtém todos os atestados de um paciente
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<List<MedicalCertificateDto>>> GetByPatient(Guid patientId)
    {
        var certificates = await _certificateService.GetByPatientIdAsync(patientId);
        return Ok(certificates);
    }

    /// <summary>
    /// Obtém todos os atestados emitidos por um profissional
    /// </summary>
    [HttpGet("professional/{professionalId}")]
    public async Task<ActionResult<List<MedicalCertificateDto>>> GetByProfessional(Guid professionalId)
    {
        var certificates = await _certificateService.GetByProfessionalIdAsync(professionalId);
        return Ok(certificates);
    }

    /// <summary>
    /// Cria um novo atestado
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MedicalCertificateDto>> Create([FromBody] CreateMedicalCertificateDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var certificate = await _certificateService.CreateAsync(dto, userId.Value);

            await _auditLogService.CreateAuditLogAsync(
                userId,
                "create",
                "MedicalCertificate",
                certificate.Id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { certificate.AppointmentId, certificate.Tipo }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            return CreatedAtAction(nameof(GetById), new { id = certificate.Id }, certificate);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza um atestado
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<MedicalCertificateDto>> Update(Guid id, [FromBody] UpdateMedicalCertificateDto dto)
    {
        try
        {
            var certificate = await _certificateService.UpdateAsync(id, dto);
            if (certificate == null)
                return NotFound();

            var userId = GetCurrentUserId();
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "update",
                "MedicalCertificate",
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
    /// Exclui um atestado
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _certificateService.DeleteAsync(id);
            if (!result)
                return NotFound();

            var userId = GetCurrentUserId();
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "delete",
                "MedicalCertificate",
                id.ToString(),
                null,
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

    /// <summary>
    /// Gera PDF do atestado
    /// </summary>
    [HttpGet("{id}/pdf")]
    public async Task<ActionResult<MedicalCertificatePdfDto>> GeneratePdf(Guid id)
    {
        try
        {
            var pdf = await _certificateService.GeneratePdfAsync(id);
            return Ok(pdf);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Assina o atestado com certificado salvo na plataforma
    /// </summary>
    [HttpPost("{id}/sign")]
    public async Task<ActionResult<MedicalCertificateDto>> SignWithSavedCertificate(Guid id, [FromBody] SignMedicalCertificateDto dto)
    {
        try
        {
            var certificate = await _certificateService.SignWithSavedCertificateAsync(id, dto.SavedCertificateId, dto.Password);
            if (certificate == null)
                return NotFound();

            var userId = GetCurrentUserId();
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "sign",
                "MedicalCertificate",
                id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { dto.SavedCertificateId }),
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
    /// Assina o atestado com arquivo PFX enviado como Base64
    /// </summary>
    [HttpPost("{id}/sign-with-pfx")]
    public async Task<ActionResult<MedicalCertificateDto>> SignWithPfx(Guid id, [FromBody] SignMedicalCertificateWithPfxDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.PfxBase64))
                return BadRequest(new { message = "Arquivo PFX não enviado." });

            var pfxBytes = Convert.FromBase64String(dto.PfxBase64);

            var certificate = await _certificateService.SignWithPfxAsync(id, pfxBytes, dto.Password);
            if (certificate == null)
                return NotFound();

            var userId = GetCurrentUserId();
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "sign",
                "MedicalCertificate",
                id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { method = "pfx_upload" }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            return Ok(certificate);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Formato Base64 inválido." });
        }
    }

    /// <summary>
    /// Valida hash de documento
    /// </summary>
    [HttpGet("validate/{documentHash}")]
    [AllowAnonymous]
    public async Task<ActionResult> ValidateDocument(string documentHash)
    {
        var isValid = await _certificateService.ValidateDocumentHashAsync(documentHash);
        return Ok(new { valid = isValid, hash = documentHash });
    }
}
