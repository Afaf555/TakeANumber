using System;

namespace API.Entities;

public class Counter
{
    public int Id { get; set; }
    public string Name { get; set; }="";
    public bool IsActive { get; set; }
    public List<CounterService> CounterServices { get; set; }=[];
    public List<Ticket> Tickets { get; set; }=[];
    public List<User> Employees { get; set; }=[];

}
