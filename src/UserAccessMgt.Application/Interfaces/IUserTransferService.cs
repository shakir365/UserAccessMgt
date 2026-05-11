using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Transfer;

namespace UserAccessMgt.Application.Interfaces;

public interface IUserTransferService
{
    Task<ApiResponse<UserTransferDto>> TransferAsync(CreateTransferRequest request, int transferredById);
    Task<ApiResponse<IEnumerable<UserTransferDto>>> GetByUserAsync(int userId);
    Task<ApiResponse<IEnumerable<UserTransferDto>>> GetByInstituteAsync(int instituteId);
    Task<ApiResponse<IEnumerable<UserTransferDto>>> GetAllAsync();
    Task<ApiResponse<UserTransferDto>> GetByIdAsync(int id);
}
