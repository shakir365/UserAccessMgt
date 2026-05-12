using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Leave;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class LeaveService : ILeaveService
{
    private readonly IUnitOfWork _unitOfWork;

    public LeaveService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<LeaveRequestDto>> ApplyAsync(CreateLeaveRequest request)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId);
        if (user is null)
            return ApiResponse<LeaveRequestDto>.Fail("User not found", "NOT_FOUND");

        if (request.StartDate > request.EndDate)
            return ApiResponse<LeaveRequestDto>.Fail("Start date cannot be after end date", "INVALID_DATES");

        var leave = new LeaveRequest
        {
            UserId = request.UserId,
            LeaveType = request.LeaveType,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            Reason = request.Reason,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<LeaveRequest>().AddAsync(leave);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<LeaveRequestDto>.Ok(MapToDto(leave), "Leave applied successfully");
    }

    public async Task<ApiResponse<LeaveRequestDto>> ApproveAsync(int id, int approverId, ApproveLeaveRequest request)
    {
        var leave = await _unitOfWork.Repository<LeaveRequest>().GetByIdAsync(id);
        if (leave is null)
            return ApiResponse<LeaveRequestDto>.Fail("Leave request not found", "NOT_FOUND");

        if (leave.Status != "Pending")
            return ApiResponse<LeaveRequestDto>.Fail("Leave request is already " + leave.Status, "ALREADY_PROCESSED");

        var validStatuses = new[] { "Approved", "Rejected" };
        if (!validStatuses.Contains(request.Status))
            return ApiResponse<LeaveRequestDto>.Fail("Status must be Approved or Rejected", "INVALID_STATUS");

        leave.Status = request.Status;
        leave.ApprovedById = approverId;
        leave.ApprovedAt = DateTime.UtcNow;
        leave.Comments = request.Comments;
        leave.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<LeaveRequest>().Update(leave);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<LeaveRequestDto>.Ok(MapToDto(leave), $"Leave {request.Status.ToLower()} successfully");
    }

    public async Task<ApiResponse<LeaveRequestDto>> GetByIdAsync(int id)
    {
        var leave = await _unitOfWork.Repository<LeaveRequest>().GetByIdAsync(id);
        if (leave is null)
            return ApiResponse<LeaveRequestDto>.Fail("Leave request not found", "NOT_FOUND");

        return ApiResponse<LeaveRequestDto>.Ok(MapToDto(leave));
    }

    public async Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetByUserAsync(int userId)
    {
        var records = await _unitOfWork.Repository<LeaveRequest>()
            .FindAsync(l => l.UserId == userId);
        return ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(records.Select(MapToDto));
    }

    public async Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetPendingAsync()
    {
        var records = await _unitOfWork.Repository<LeaveRequest>()
            .FindAsync(l => l.Status == "Pending");
        return ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(records.Select(MapToDto));
    }

    public async Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetAllAsync()
    {
        var records = await _unitOfWork.Repository<LeaveRequest>().GetAllAsync();
        return ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(records.Select(MapToDto));
    }

    private static LeaveRequestDto MapToDto(LeaveRequest leave) => new()
    {
        Id = leave.Id,
        UserId = leave.UserId,
        UserName = leave.User?.Username ?? string.Empty,
        LeaveType = leave.LeaveType,
        StartDate = leave.StartDate,
        EndDate = leave.EndDate,
        Reason = leave.Reason,
        Status = leave.Status,
        ApprovedById = leave.ApprovedById,
        ApprovedByName = leave.ApprovedBy?.Username,
        ApprovedAt = leave.ApprovedAt,
        Comments = leave.Comments,
        CreatedAt = leave.CreatedAt
    };
}
