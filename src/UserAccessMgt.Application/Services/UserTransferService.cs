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

        var transferredBy = await _unitOfWork.Repository<User>().GetByIdAsync(transferredById);
        if (transferredBy is null)
            return ApiResponse<UserTransferDto>.Fail("Transferred by user not found", "TRANSFERRED_BY_NOT_FOUND");

        if (user.InstituteId == request.ToInstituteId)
            return ApiResponse<UserTransferDto>.Fail("User is already in this institute", "SAME_INSTITUTE");

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

        return ApiResponse<UserTransferDto>.Ok(GetDto(transfer.Id), "User transferred successfully");
    }

    public Task<ApiResponse<IEnumerable<UserTransferDto>>> GetByUserAsync(int userId)
    {
        var records = _unitOfWork.Repository<UserTransfer>()
            .Query()
            .Where(t => t.UserId == userId)
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<UserTransferDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<UserTransferDto>>> GetByInstituteAsync(int instituteId)
    {
        var records = _unitOfWork.Repository<UserTransfer>()
            .Query()
            .Where(t => t.FromInstituteId == instituteId || t.ToInstituteId == instituteId)
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<UserTransferDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<UserTransferDto>>> GetAllAsync()
    {
        var records = _unitOfWork.Repository<UserTransfer>()
            .Query()
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<UserTransferDto>>.Ok(records));
    }

    public Task<ApiResponse<UserTransferDto>> GetByIdAsync(int id)
    {
        var transfer = _unitOfWork.Repository<UserTransfer>()
            .Query()
            .Where(t => t.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefault();
        if (transfer is null)
            return Task.FromResult(ApiResponse<UserTransferDto>.Fail("Transfer record not found", "NOT_FOUND"));

        return Task.FromResult(ApiResponse<UserTransferDto>.Ok(transfer));
    }

    private static readonly System.Linq.Expressions.Expression<Func<UserTransfer, UserTransferDto>> ProjectToDto = transfer => new UserTransferDto
    {
        Id = transfer.Id,
        UserId = transfer.UserId,
        UserName = transfer.User == null ? string.Empty : transfer.User.Username,
        FromInstituteName = transfer.FromInstitute == null ? string.Empty : transfer.FromInstitute.Name,
        ToInstituteName = transfer.ToInstitute == null ? string.Empty : transfer.ToInstitute.Name,
        TransferredByName = transfer.TransferredBy == null ? string.Empty : transfer.TransferredBy.Username,
        TransferDate = transfer.TransferDate,
        Reason = transfer.Reason
    };

    private UserTransferDto GetDto(int id)
        => _unitOfWork.Repository<UserTransfer>()
            .Query()
            .Where(t => t.Id == id)
            .Select(ProjectToDto)
            .First();
}
