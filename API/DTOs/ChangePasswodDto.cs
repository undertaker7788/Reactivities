using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class ChangePasswodDto
{
    [Required]
    public string CurrentPassword { get; set; } = "";

    [Required]
    public string NewPassword { get; set; } = "";
}