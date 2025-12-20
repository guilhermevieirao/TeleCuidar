using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using System.Text.Json;
using System.Collections.Concurrent;

namespace WebAPI.Controllers;

/// <summary>
/// Controller para gerenciar chat de anexos em tempo real durante teleconsultas
/// </summary>
[ApiController]
[Route("api/appointments/{appointmentId}/attachments-chat")]
public class AttachmentsChatController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    // Lock por appointment para evitar race conditions
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public AttachmentsChatController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtém todas as mensagens de anexos de uma consulta
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AttachmentMessageDto>>> GetMessages(Guid appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        
        if (appointment == null)
            return NotFound(new { message = "Consulta não encontrada" });

        if (string.IsNullOrEmpty(appointment.AttachmentsChatJson))
            return Ok(new List<AttachmentMessageDto>());

        var messages = JsonSerializer.Deserialize<List<AttachmentMessageDto>>(appointment.AttachmentsChatJson);
        return Ok(messages ?? new List<AttachmentMessageDto>());
    }

    /// <summary>
    /// Adiciona uma nova mensagem de anexo ao chat
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<ActionResult> AddMessage(Guid appointmentId, [FromBody] AttachmentMessageDto dto)
    {
        // Obter ou criar lock para este appointment
        var semaphore = _locks.GetOrAdd(appointmentId, _ => new SemaphoreSlim(1, 1));
        
        await semaphore.WaitAsync();
        try
        {
            // Recarregar o appointment dentro do lock para garantir dados atuais
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            
            if (appointment == null)
                return NotFound(new { message = "Consulta não encontrada" });

            // Load existing messages
            var messages = new List<AttachmentMessageDto>();
            if (!string.IsNullOrEmpty(appointment.AttachmentsChatJson))
            {
                messages = JsonSerializer.Deserialize<List<AttachmentMessageDto>>(appointment.AttachmentsChatJson) 
                           ?? new List<AttachmentMessageDto>();
            }

            // Generate ID if not provided
            if (string.IsNullOrEmpty(dto.Id))
                dto.Id = Guid.NewGuid().ToString();
            
            // Set timestamp if not provided
            if (string.IsNullOrEmpty(dto.Timestamp))
                dto.Timestamp = DateTime.UtcNow.ToString("o");

            messages.Add(dto);
            appointment.AttachmentsChatJson = JsonSerializer.Serialize(messages);
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Anexo adicionado com sucesso", data = dto });
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Obtém mensagens desde uma determinada data (para polling eficiente)
    /// </summary>
    [HttpGet("since")]
    public async Task<ActionResult<List<AttachmentMessageDto>>> GetMessagesSince(Guid appointmentId, [FromQuery] string? timestamp)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        
        if (appointment == null)
            return NotFound(new { message = "Consulta não encontrada" });

        if (string.IsNullOrEmpty(appointment.AttachmentsChatJson))
            return Ok(new List<AttachmentMessageDto>());

        var allMessages = JsonSerializer.Deserialize<List<AttachmentMessageDto>>(appointment.AttachmentsChatJson) 
                          ?? new List<AttachmentMessageDto>();

        if (string.IsNullOrEmpty(timestamp))
            return Ok(allMessages);

        // Filter messages after timestamp
        if (DateTime.TryParse(timestamp, out var sinceDate))
        {
            var newMessages = allMessages
                .Where(m => DateTime.TryParse(m.Timestamp, out var msgDate) && msgDate > sinceDate)
                .ToList();
            return Ok(newMessages);
        }

        return Ok(allMessages);
    }

    /// <summary>
    /// Verifica quantas mensagens existem (para polling eficiente)
    /// </summary>
    [HttpHead]
    public async Task<ActionResult> CheckMessages(Guid appointmentId, [FromQuery] int? count)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        
        if (appointment == null)
            return NotFound();

        if (string.IsNullOrEmpty(appointment.AttachmentsChatJson))
        {
            Response.Headers["X-Message-Count"] = "0";
            return NoContent();
        }

        var messages = JsonSerializer.Deserialize<List<AttachmentMessageDto>>(appointment.AttachmentsChatJson);
        var messageCount = messages?.Count ?? 0;
        
        Response.Headers["X-Message-Count"] = messageCount.ToString();
        
        if (count.HasValue && messageCount > count.Value)
            return Ok(); // Has new messages
        
        return NoContent(); // No new messages
    }
}

public class AttachmentMessageDto
{
    public string? Id { get; set; }
    public string SenderRole { get; set; } = "PATIENT"; // "PATIENT" or "PROFESSIONAL"
    public string SenderName { get; set; } = string.Empty;
    public string? Timestamp { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileUrl { get; set; } = string.Empty; // Base64 encoded
}
