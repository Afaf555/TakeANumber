using System.ComponentModel.DataAnnotations;
using Microsoft.Identity.Client;

namespace API.DTOs;
public class RenameCounterDto
{
    [Required]
    public string Name {get; set; }="";
    
}