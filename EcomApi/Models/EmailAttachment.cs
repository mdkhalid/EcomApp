namespace EcomApi.Models;

public sealed record EmailAttachment(string FileName, string ContentType, byte[] Data);
