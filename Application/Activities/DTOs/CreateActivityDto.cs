using System;

namespace Application.Activities.DTOs;

public class CreateActivityDto : BaseActivityDto
{
    public string Id { get; set; } = string.Empty;
}