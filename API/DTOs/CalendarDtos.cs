namespace API.DTOs;

public class NonWorkingDayDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = "";
}

public class CreateNonWorkingDayDto
{
    public DateTime Date { get; set; }
    public string Name { get; set; } = "";
}

public class EmployeeWorkDto
{
    public int EmployeeId { get; set; }
    public string Username { get; set; } = "";
    public int Sessions { get; set; }
    public double Hours { get; set; }
    public int TicketsServed { get; set; }
}

public class NonWorkingDayWorkDto
{
    public DateTime Date { get; set; }
    public string Reason { get; set; } = "";   
    public bool IsHoliday { get; set; }
    public List<EmployeeWorkDto> Employees { get; set; } = [];
}