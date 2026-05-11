using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Transfer;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class UserTransferService : IUserTransferService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserTransferService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<UserTransferDto>> TransferAsync(CreateTransferRequest request, int transferredById)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId);
        if (user is null)
            return ApiResponse<UserTransferDto>.Fail("User not found", "NOT_FOUND");

        var toInstitute = await _unitOfWork.Repository<Institute>().GetByIdAsync(request.ToInstituteId);
        if (toInstitute is null)
            return ApiResponse<UserTransferDto>.Fail("Target institute not found", "INSTITUTE_NOT_FOUND");

        if (user.InstituteId == request.ToInstituteId)
            return ApiResponse<UserTransferDto>.Fail("User is already in this institute", "SAME_INSTITUTE");

        var fromInstitute = await _unitOfWork.Repository<Institute>().GetByIdAsync(user.InstituteId);
        var transfer = new UserTransfer
        {
            UserId = user.Id,
            FromInstituteId = user.InstituteId,
            ToInstituteId = request.ToInstituteId,
            TransferredById = transferredById,
            TransferDate = DateTime.UtcNow,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow
        };

        user.InstituteId = request.ToInstituteId;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<UserTransfer>().AddAsync(transfer);
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<UserTransferDto>.Ok(MapToDto(transfer), "User transferred successfully");
    }

    public async Task<ApiResponse<IEnumerable<UserTransferDto>>> GetByUserAsync(int userId)
    {
        var records = await _unitOfWork.Repository<UserTransfer>()
            .FindAsync(t => t.UserId == userId);
        return ApiResponse<IEnumerable<UserTransferDto>>.Ok(records.Select(MapToDto));
    }

    public async Task<ApiResponse<IEnumerable<UserTransferDto>>> GetByInstituteAsync(int instituteId)
    {
        var records = await _unitOfWork.Repository<UserTransfer>()
            .FindAsync(t => t.FromInstituteId == instituteId || t.ToInstituteId == instituteId);
        return ApiResponse<IEnumerable<UserTransferDto>>.Ok(records.Select(MapToDto));
    }

    public async Task<ApiResponse<IEnumerable<UserTransferDto>>> GetAllAsync()
    {
        var records = await _unitOfWork.Repository<UserTransfer>().GetAllAsync();
        return ApiResponse<IEnumerable<UserTransferDto>>.Ok(records.Select(MapToDto));
    }

    public async Task<ApiResponse<UserTransferDto>> GetByIdAsync(int id)
    {
        var transfer = await _unitOfWork.Repository<UserTransfer>().GetByIdAsync(id);
        if (transfer is null)
            return ApiResponse<UserTransferDto>.Fail("Transfer record not found", "NOT_FOUND");

        return ApiResponse<UserTransferDto>.Ok(MapToDto(transfer));
    }

    private static UserTransferDto MapToDto(UserTransfer transfer) => new()
    {
        Id = transfer.Id,
        UserId = transfer.UserId,
        UserName = transfer.User?.Username ?? string.Empty,
        FromInstituteName = transfer.FromInstitute?.Name ?? string.Empty,
        ToInstituteName = transfer.ToInstitute?.Name ?? string.Empty,
        TransferredByName = transfer.TransferredBy?.Username ?? string.Empty,
        TransferDate = transfer.TransferDate,
        Reason = transfer.Reason
    };
}
