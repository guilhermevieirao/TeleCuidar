using Application.DTOs.MedicalReports;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/medical-reports")]
[Authorize]
public class MedicalReportsController : ControllerBase
{
    private readonly IMedicalReportService _reportService;
    private readonly ILogger<MedicalReportsController> _logger;

    public MedicalReportsController(
        IMedicalReportService reportService,
        ILogger<MedicalReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém um laudo por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<MedicalReportDto>> GetById(Guid id)
    {
        var report = await _reportService.GetByIdAsync(id);
        if (report == null)
            return NotFound(new { message = "Laudo não encontrado" });

        return Ok(report);
    }

    /// <summary>
    /// Lista todos os laudos de uma consulta
    /// </summary>
    [HttpGet("appointment/{appointmentId}")]
    public async Task<ActionResult<List<MedicalReportDto>>> GetByAppointmentId(Guid appointmentId)
    {
        var reports = await _reportService.GetByAppointmentIdAsync(appointmentId);
        return Ok(reports);
    }

    /// <summary>
    /// Lista todos os laudos de um paciente
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<List<MedicalReportDto>>> GetByPatientId(Guid patientId)
    {
        var reports = await _reportService.GetByPatientIdAsync(patientId);
        return Ok(reports);
    }

    /// <summary>
    /// Lista todos os laudos emitidos por um profissional
    /// </summary>
    [HttpGet("professional/{professionalId}")]
    public async Task<ActionResult<List<MedicalReportDto>>> GetByProfessionalId(Guid professionalId)
    {
        var reports = await _reportService.GetByProfessionalIdAsync(professionalId);
        return Ok(reports);
    }

    /// <summary>
    /// Cria um novo laudo
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MedicalReportDto>> Create([FromBody] CreateMedicalReportDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var report = await _reportService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar laudo");
            return StatusCode(500, new { message = "Erro interno ao criar laudo" });
        }
    }

    /// <summary>
    /// Atualiza um laudo existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<MedicalReportDto>> Update(Guid id, [FromBody] UpdateMedicalReportDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var report = await _reportService.UpdateAsync(id, dto, userId);
            
            if (report == null)
                return NotFound(new { message = "Laudo não encontrado ou você não tem permissão para editá-lo" });

            return Ok(report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar laudo");
            return StatusCode(500, new { message = "Erro interno ao atualizar laudo" });
        }
    }

    /// <summary>
    /// Remove um laudo
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var deleted = await _reportService.DeleteAsync(id, userId);
            
            if (!deleted)
                return NotFound(new { message = "Laudo não encontrado ou você não tem permissão para excluí-lo" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir laudo");
            return StatusCode(500, new { message = "Erro interno ao excluir laudo" });
        }
    }

    /// <summary>
    /// Gera o PDF de um laudo
    /// </summary>
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var pdfDto = await _reportService.GeneratePdfAsync(id);
            return File(pdfDto.PdfBytes, pdfDto.ContentType, pdfDto.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar PDF do laudo");
            return StatusCode(500, new { message = "Erro interno ao gerar PDF" });
        }
    }

    /// <summary>
    /// Valida um documento pelo hash
    /// </summary>
    [HttpGet("validate/{hash}")]
    [AllowAnonymous]
    public async Task<ActionResult<MedicalReportDto>> ValidateByHash(string hash)
    {
        var report = await _reportService.ValidateByHashAsync(hash);
        
        if (report == null)
            return NotFound(new { message = "Documento não encontrado", valid = false });

        return Ok(new
        {
            valid = true,
            isSigned = report.IsSigned,
            report = report
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Usuário não autenticado");
            
        return userId;
    }
}
