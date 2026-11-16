using MediatR;
using SIFTC.Domain.Common;

namespace SIFTC.Application.UseCases.Diccionario.Commands.CargarDiccionario;

/// <summary>
/// POST /api/v1/diccionario/cargar (HU-DD-08). Lee diccionario.csv
/// (separador ';', UTF-8), valida las 8 columnas y reemplaza la tabla.
/// </summary>
public sealed record CargarDiccionarioCommand(Stream Archivo) : IRequest<Result<CargaDiccionarioResponseDto>>;
