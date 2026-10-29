using System;
using API.Entities.Enums;

namespace API.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }="";
    public byte[] PasswordHash { get; set; }=Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; }=Array.Empty<byte>();
    public Role Role { get; set; }
    public int? CounterId { get; set; }
    public Counter Counter { get; set; }=null!;
    public List<Ticket> Tickets { get; set; }=[];
    public bool IsOnBreak { get; set; }

}
