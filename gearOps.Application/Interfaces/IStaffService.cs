using System.Collections.Generic;
using System.Threading.Tasks;
using gearOps.Application.DTOs;

namespace gearOps.Application.Interfaces;

public interface IStaffService
{
    Task<IEnumerable<StaffResponseDto>> GetAllStaffAsync();
    Task<StaffResponseDto> GetStaffByIdAsync(int staffId);
    Task<StaffResponseDto> CreateStaffAsync(CreateStaffDto dto);
    Task<bool> UpdateStaffAsync(int staffId, UpdateStaffDto dto);
    Task<bool> DeleteStaffAsync(int staffId);
}
