using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using System.Text.Json;

namespace WebAPI.Controllers;

/// <summary>
/// Controller para gerenciar dados biométricos em tempo real durante teleconsultas
/// </summary>
[ApiController]
[Route("api/appointments/{appointmentId}/[controller]")]
public class BiometricsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BiometricsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtém os dados biométricos atuais de uma consulta
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<BiometricsDto>> GetBiometrics(Guid appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        
        if (appointment == null)
            return NotFound(new { message = "Consulta não encontrada" });

        if (string.IsNullOrEmpty(appointment.BiometricsJson))
            return Ok(new BiometricsDto());

        var biometrics = JsonSerializer.Deserialize<BiometricsDto>(appointment.BiometricsJson);
        return Ok(biometrics);
    }

    /// <summary>
    /// Atualiza os dados biométricos de uma consulta (usado pelo paciente)
    /// </summary>
    [HttpPut]
    public async Task<ActionResult> UpdateBiometrics(Guid appointmentId, [FromBody] BiometricsDto dto)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        
        if (appointment == null)
            return NotFound(new { message = "Consulta não encontrada" });

        dto.LastUpdated = DateTime.UtcNow.ToString("o");
        appointment.BiometricsJson = JsonSerializer.Serialize(dto);
        
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Biométricos atualizados com sucesso", data = dto });
    }

    /// <summary>
    /// Verifica se houve atualização desde uma determinada data (para polling eficiente)
    /// </summary>
    [HttpHead]
    public async Task<ActionResult> CheckUpdate(Guid appointmentId, [FromQuery] string? since)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        
        if (appointment == null)
            return NotFound();

        if (string.IsNullOrEmpty(appointment.BiometricsJson))
            return NoContent(); // 204 - no data yet

        var biometrics = JsonSerializer.Deserialize<BiometricsDto>(appointment.BiometricsJson);
        
        if (string.IsNullOrEmpty(since) || string.IsNullOrEmpty(biometrics?.LastUpdated))
            return Ok(); // Has data

        // Compare timestamps
        if (DateTime.TryParse(since, out var sinceDate) && 
            DateTime.TryParse(biometrics.LastUpdated, out var lastUpdated))
        {
            if (lastUpdated > sinceDate)
                return Ok(); // Has updates
            else
                return NoContent(); // No updates since
        }

        return Ok();
    }
}

public class BiometricsDto
{
    public int? HeartRate { get; set; }
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? OxygenSaturation { get; set; }
    public decimal? Temperature { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? Glucose { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public string? LastUpdated { get; set; }
}
