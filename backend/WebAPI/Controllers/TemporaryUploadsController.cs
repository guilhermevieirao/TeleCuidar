using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using WebAPI.Hubs;

namespace WebAPI.Controllers;

/// <summary>
/// Controller para gerenciar uploads temporários entre dispositivos (ex: QR Code mobile upload)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TemporaryUploadsController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly IHubContext<TeleconsultationHub> _hubContext;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

    public TemporaryUploadsController(IMemoryCache cache, IHubContext<TeleconsultationHub> hubContext)
    {
        _cache = cache;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Armazena um upload temporário associado a um token (suporta múltiplos uploads por token)
    /// </summary>
    [HttpPost("{token}")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<ActionResult> StoreUpload(string token, [FromBody] TemporaryUploadDto dto)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token é obrigatório" });

        if (dto == null || string.IsNullOrWhiteSpace(dto.FileUrl))
            return BadRequest(new { message = "Dados do arquivo são obrigatórios" });

        var cacheKey = $"temp_upload_queue_{token}";
        
        // Get or create queue for this token
        var queue = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiration;
            return new ConcurrentQueue<TemporaryUploadDto>();
        })!;

        // Add to queue
        queue.Enqueue(dto);
        
        // Refresh expiration
        _cache.Set(cacheKey, queue, CacheExpiration);

        // Notificar via SignalR quem está escutando este token
        await _hubContext.Clients.Group($"mobile_upload_{token}").SendAsync("MobileUploadReceived", new
        {
            Title = dto.Title,
            Type = dto.Type,
            FileUrl = dto.FileUrl,
            Timestamp = dto.Timestamp
        });

        return Ok(new { message = "Upload armazenado com sucesso" });
    }

    /// <summary>
    /// Recupera o próximo upload temporário pelo token (FIFO - first in, first out)
    /// </summary>
    [HttpGet("{token}")]
    public ActionResult<TemporaryUploadDto> GetUpload(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token é obrigatório" });

        var cacheKey = $"temp_upload_queue_{token}";
        
        if (_cache.TryGetValue<ConcurrentQueue<TemporaryUploadDto>>(cacheKey, out var queue) && queue != null)
        {
            if (queue.TryDequeue(out var dto))
            {
                return Ok(dto);
            }
        }

        return NotFound(new { message = "Upload não encontrado ou expirado" });
    }

    /// <summary>
    /// Verifica se existe um upload pendente para o token (sem consumir)
    /// </summary>
    [HttpHead("{token}")]
    public ActionResult CheckUpload(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest();

        var cacheKey = $"temp_upload_queue_{token}";
        
        if (_cache.TryGetValue<ConcurrentQueue<TemporaryUploadDto>>(cacheKey, out var queue) && queue != null && !queue.IsEmpty)
        {
            return Ok();
        }

        return NotFound();
    }
}

public class TemporaryUploadDto
{
    public string Title { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty; // Base64 encoded
    public string Type { get; set; } = "document"; // "image" or "document"
    public long Timestamp { get; set; }
}
