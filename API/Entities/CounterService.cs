using System;

namespace API.Entities;

public class CounterService
{
public int CounterId { get; set; }
public Counter Counter { get; set; }=null!;

public int ServiceId { get; set; }  
public Service Service { get; set; }=null!;
}
