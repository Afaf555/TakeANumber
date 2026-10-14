using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class CreateCounterDto
{
    [Required]
    public string Name { get; set; }="";

}
