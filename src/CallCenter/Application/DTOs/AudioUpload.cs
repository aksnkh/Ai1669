namespace CallCenter.Application.DTOs;

public sealed record AudioUpload(Stream Content, string FileName, string MediaType);
