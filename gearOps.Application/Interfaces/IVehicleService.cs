using System.Collections.Generic;
using System.Threading.Tasks;
using gearOps.Application.DTOs;

namespace gearOps.Application.Interfaces;

public interface IVehicleService
{
    Task<IEnumerable<VehicleResponseDto>> GetCustomerVehiclesAsync(int customerId);
    Task<VehicleResponseDto> GetVehicleByIdAsync(int vehicleId, int customerId);
    Task<VehicleResponseDto> AddVehicleAsync(int customerId, CreateVehicleDto dto);
    Task<bool> UpdateVehicleAsync(int vehicleId, int customerId, UpdateVehicleDto dto);
    Task<bool> DeleteVehicleAsync(int vehicleId, int customerId);
}
