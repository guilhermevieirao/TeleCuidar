using Application.DTOs.Cns;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Controller para integração com CADSUS (Cadastro Nacional de Usuários do SUS)
/// Permite consultar dados de cidadãos pelo CPF utilizando certificado digital A1
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CnsController : ControllerBase
{
    private readonly ICnsService _cnsService;
    private readonly ILogger<CnsController> _logger;

    public CnsController(ICnsService cnsService, ILogger<CnsController> logger)
    {
        _cnsService = cnsService;
        _logger = logger;
    }

    /// <summary>
    /// Consulta dados de um cidadão no CADSUS pelo CPF
    /// </summary>
    /// <param name="request">CPF do cidadão a ser consultado</param>
    /// <returns>Dados completos do cidadão cadastrado no CADSUS</returns>
    /// <response code="200">Dados do cidadão encontrados</response>
    /// <response code="400">CPF inválido ou não informado</response>
    /// <response code="503">Serviço CADSUS não configurado</response>
    /// <response code="500">Erro interno ao consultar CADSUS</response>
    [HttpPost("consultar-cpf")]
    [ProducesResponseType(typeof(CnsCidadaoDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(503)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<CnsCidadaoDto>> ConsultarCpf([FromBody] CnsConsultaRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Cpf))
            {
                return BadRequest(new { error = "CPF é obrigatório" });
            }

            // Verificar se o serviço está configurado
            if (!_cnsService.IsConfigured())
            {
                return StatusCode(503, new { 
                    error = "Serviço CNS não configurado", 
                    message = "Configure as variáveis de ambiente CNS_CERT_PATH e CNS_CERT_PASSWORD para habilitar consultas ao CADSUS."
                });
            }

            var resultado = await _cnsService.ConsultarCpfAsync(request.Cpf);
            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Consulta CNS - CPF inválido: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Erro ao consultar CNS");
            return StatusCode(500, new { error = "Erro ao consultar CADSUS", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno ao consultar CNS");
            return StatusCode(500, new { error = "Erro interno ao consultar CADSUS", message = ex.Message });
        }
    }

    /// <summary>
    /// Obtém o status do token de autenticação do CADSUS
    /// </summary>
    /// <returns>Status do token incluindo validade e tempo restante</returns>
    [HttpGet("token/status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CnsTokenStatusDto), 200)]
    public ActionResult<CnsTokenStatusDto> GetTokenStatus()
    {
        try
        {
            if (!_cnsService.IsConfigured())
            {
                return Ok(new CnsTokenStatusDto
                {
                    HasToken = false,
                    Message = "Serviço CNS não configurado"
                });
            }

            var status = _cnsService.GetTokenStatus();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar status do token CNS");
            return StatusCode(500, new { error = "Erro ao verificar status do token", message = ex.Message });
        }
    }

    /// <summary>
    /// Força a renovação do token de autenticação do CADSUS
    /// </summary>
    /// <returns>Novo status do token após renovação</returns>
    [HttpPost("token/renew")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CnsTokenRenewResponseDto), 200)]
    public async Task<ActionResult<CnsTokenRenewResponseDto>> RenewToken()
    {
        try
        {
            if (!_cnsService.IsConfigured())
            {
                return Ok(new CnsTokenRenewResponseDto
                {
                    Success = false,
                    Message = "Serviço CNS não configurado"
                });
            }

            var status = await _cnsService.ForceTokenRenewalAsync();
            return Ok(new CnsTokenRenewResponseDto
            {
                Success = true,
                Message = "Token renovado com sucesso",
                HasToken = status.HasToken,
                IsValid = status.IsValid,
                ExpiresAt = status.ExpiresAt,
                ExpiresIn = status.ExpiresIn
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao renovar token CNS");
            return Ok(new CnsTokenRenewResponseDto
            {
                Success = false,
                Message = $"Erro ao renovar token: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Verifica se o serviço CNS está configurado e pronto para uso
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult GetHealth()
    {
        var isConfigured = _cnsService.IsConfigured();
        return Ok(new
        {
            status = isConfigured ? "configured" : "not_configured",
            message = isConfigured 
                ? "Serviço CNS configurado e pronto para uso" 
                : "Serviço CNS não configurado. Configure CNS_CERT_PATH e CNS_CERT_PASSWORD."
        });
    }
}
