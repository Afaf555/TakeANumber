using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class CreateEmployeeDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(4)]
    public string Password { get; set; } = string.Empty;

  
    public int? CounterId { get; set; }
}
