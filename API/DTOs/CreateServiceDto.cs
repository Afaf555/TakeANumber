using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class CreateServiceDto
{
    [Required]
    public string Name  { get; set; }="";
}
