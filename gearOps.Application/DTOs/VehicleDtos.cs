namespace gearOps.Application.DTOs;

public class CreateVehicleDto
{
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string VehicleNumber { get; set; } = null!;
}

public class UpdateVehicleDto
{
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string VehicleNumber { get; set; } = null!;
}

public class VehicleResponseDto
{
    public int VehicleId { get; set; }
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string VehicleNumber { get; set; } = null!;
}
