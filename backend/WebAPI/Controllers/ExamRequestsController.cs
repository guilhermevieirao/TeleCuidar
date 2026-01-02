using Application.DTOs.ExamRequests;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/exam-requests")]
[Authorize]
public class ExamRequestsController : ControllerBase
{
    private readonly IExamRequestService _examService;
    private readonly IAuditLogService _auditLogService;

    public ExamRequestsController(
        IExamRequestService examService,
        IAuditLogService auditLogService)
    {
        _examService = examService;
        _auditLogService = auditLogService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? Guid.Parse(userIdClaim) : null;
    }

    /// <summary>
    /// Obtém uma solicitação de exame por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ExamRequestDto>> GetById(Guid id)
    {
        var exam = await _examService.GetByIdAsync(id);
        if (exam == null)
            return NotFound();

        return Ok(exam);
    }

    /// <summary>
    /// Obtém todas as solicitações de exame de uma consulta
    /// </summary>
    [HttpGet("appointment/{appointmentId}")]
    public async Task<ActionResult<List<ExamRequestDto>>> GetByAppointment(Guid appointmentId)
    {
        var exams = await _examService.GetByAppointmentIdAsync(appointmentId);
        return Ok(exams);
    }

    /// <summary>
    /// Obtém todas as solicitações de exame de um paciente
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<ActionResult<List<ExamRequestDto>>> GetByPatient(Guid patientId)
    {
        var exams = await _examService.GetByPatientIdAsync(patientId);
        return Ok(exams);
    }

    /// <summary>
    /// Obtém todas as solicitações de exame emitidas por um profissional
    /// </summary>
    [HttpGet("professional/{professionalId}")]
    public async Task<ActionResult<List<ExamRequestDto>>> GetByProfessional(Guid professionalId)
    {
        var exams = await _examService.GetByProfessionalIdAsync(professionalId);
        return Ok(exams);
    }

    /// <summary>
    /// Cria uma nova solicitação de exame
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ExamRequestDto>> Create([FromBody] CreateExamRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var exam = await _examService.CreateAsync(dto, userId.Value);

            await _auditLogService.CreateAuditLogAsync(
                userId,
                "create",
                "ExamRequest",
                exam.Id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(new { exam.AppointmentId, exam.NomeExame, exam.Categoria }),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            return CreatedAtAction(nameof(GetById), new { id = exam.Id }, exam);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza uma solicitação de exame
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ExamRequestDto>> Update(Guid id, [FromBody] UpdateExamRequestDto dto)
    {
        try
        {
            var exam = await _examService.UpdateAsync(id, dto);
            if (exam == null)
                return NotFound();

            var userId = GetCurrentUserId();
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "update",
                "ExamRequest",
                id.ToString(),
                null,
                HttpContextExtensions.SerializeToJson(dto),
                HttpContext.GetIpAddress(),
                HttpContext.GetUserAgent()
            );

            return Ok(exam);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Exclui uma solicitação de exame
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _examService.DeleteAsync(id);
            if (!result)
                return NotFound();

            var userId = GetCurrentUserId();
            await _auditLogService.CreateAuditLogAsync(
                userId,
                "delete",
                "ExamRequest",
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
    /// Gera PDF da solicitação de exame
    /// </summary>
    [HttpGet("{id}/pdf")]
    public async Task<ActionResult<ExamRequestPdfDto>> GeneratePdf(Guid id)
    {
        try
        {
            var pdf = await _examService.GeneratePdfAsync(id);
            return Ok(pdf);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Valida hash de documento
    /// </summary>
    [HttpGet("validate/{documentHash}")]
    [AllowAnonymous]
    public async Task<ActionResult> ValidateDocument(string documentHash)
    {
        var isValid = await _examService.ValidateDocumentHashAsync(documentHash);
        return Ok(new { valid = isValid, hash = documentHash });
    }
}
