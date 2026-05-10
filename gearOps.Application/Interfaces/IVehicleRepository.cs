using System.Collections.Generic;
using System.Threading.Tasks;
using gearOps.Domain.Entities;

namespace gearOps.Application.Interfaces;

public interface IVehicleRepository
{
    Task<IEnumerable<Vehicle>> GetVehiclesByCustomerIdAsync(int customerId);
    Task<Vehicle?> GetVehicleByIdAsync(int vehicleId, int customerId);
    Task<Vehicle> AddVehicleAsync(Vehicle vehicle);
    Task UpdateVehicleAsync(Vehicle vehicle);
    Task DeleteVehicleAsync(Vehicle vehicle);
}
