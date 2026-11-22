using CallCenter.Domain.Enums;

namespace CallCenter.Application.DTOs;

public sealed record VoiceCoverageReply(CoverageStatus Status, byte[] Audio);
