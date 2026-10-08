using System.ComponentModel.DataAnnotations;

namespace BlueStaq.Application.DTOs;

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(256)] string Password);
public sealed record CreateTeamRequest([Required, StringLength(100)] string Name);
public sealed record SaveNoteRequest(
    [Required, StringLength(200)] string Title,
    [Required, StringLength(50000)] string Content);
public sealed record UserResponse(Guid Id, string Email, string DisplayName);
public sealed record TeamResponse(Guid Id, string Name, DateTimeOffset CreatedAt);
public sealed record NoteResponse(Guid Id, Guid TeamId, string Title, string Content,
    Guid CreatedByUserId, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
