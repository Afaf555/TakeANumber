using System;

namespace API.DTOs;

public class CounterDto
{
    public int Id  { get; set; }
    public string Name { get; set; }="";
    public bool IsActive { get; set; }
    public List<int> ServiceIds {get; set; }=[];
}
