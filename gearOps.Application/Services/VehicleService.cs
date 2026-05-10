using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;

namespace gearOps.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;

    public VehicleService(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<VehicleResponseDto>> GetCustomerVehiclesAsync(int customerId)
    {
        var vehicles = await _repository.GetVehiclesByCustomerIdAsync(customerId);
        return vehicles.Select(v => new VehicleResponseDto
        {
            VehicleId = v.VehicleId,
            Brand = v.Brand,
            Model = v.Model,
            Year = v.Year,
            VehicleNumber = v.VehicleNumber
        });
    }

    public async Task<VehicleResponseDto> GetVehicleByIdAsync(int vehicleId, int customerId)
    {
        var vehicle = await _repository.GetVehicleByIdAsync(vehicleId, customerId);

        if (vehicle == null) throw new NotFoundException("Vehicle not found or you don't have access.");

        return new VehicleResponseDto
        {
            VehicleId = vehicle.VehicleId,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            VehicleNumber = vehicle.VehicleNumber
        };
    }

    public async Task<VehicleResponseDto> AddVehicleAsync(int customerId, CreateVehicleDto dto)
    {
        var vehicle = new Vehicle
        {
            CustomerId = customerId,
            Brand = dto.Brand,
            Model = dto.Model,
            Year = dto.Year,
            VehicleNumber = dto.VehicleNumber
        };

        var createdVehicle = await _repository.AddVehicleAsync(vehicle);

        return new VehicleResponseDto
        {
            VehicleId = createdVehicle.VehicleId,
            Brand = createdVehicle.Brand,
            Model = createdVehicle.Model,
            Year = createdVehicle.Year,
            VehicleNumber = createdVehicle.VehicleNumber
        };
    }

    public async Task<bool> UpdateVehicleAsync(int vehicleId, int customerId, UpdateVehicleDto dto)
    {
        var vehicle = await _repository.GetVehicleByIdAsync(vehicleId, customerId);

        if (vehicle == null) throw new NotFoundException("Vehicle not found or you don't have access.");

        vehicle.Brand = dto.Brand;
        vehicle.Model = dto.Model;
        vehicle.Year = dto.Year;
        vehicle.VehicleNumber = dto.VehicleNumber;

        await _repository.UpdateVehicleAsync(vehicle);
        return true;
    }

    public async Task<bool> DeleteVehicleAsync(int vehicleId, int customerId)
    {
        var vehicle = await _repository.GetVehicleByIdAsync(vehicleId, customerId);

        if (vehicle == null) throw new NotFoundException("Vehicle not found or you don't have access.");

        await _repository.DeleteVehicleAsync(vehicle);
        return true;
    }
}
