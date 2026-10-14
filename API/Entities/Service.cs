using System;

namespace API.Entities;

public class Service
{
 public int Id { get; set; }
 public string Name { get; set; }="";
 public List<CounterService> CounterServices { get; set; }=[];
 public List<Ticket> Tickets { get; set; }=[];

}

