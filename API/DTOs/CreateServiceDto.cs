using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class CreateServiceDto
{
    [Required]
    public string Name  { get; set; }="";

    [Required]
    [RegularExpression("^[A-Za-z]{1,3}$", ErrorMessage = "Code must be 1-3 letters")]
    public string Code { get; set; }="";
}
