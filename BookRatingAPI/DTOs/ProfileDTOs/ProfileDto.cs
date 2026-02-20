using System;
using System.Collections.Generic;
using BookRatingAPI.DTOs.ProfileDTOs;

namespace BookRatingAPI.DTOs;

public class ProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ProfileStatsDto Stats { get; set; } = null!;
}