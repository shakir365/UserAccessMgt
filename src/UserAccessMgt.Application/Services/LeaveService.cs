using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Leave;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class LeaveService : ILeaveService
{
    private readonly IUnitOfWork _unitOfWork;
    private static readonly string[] ValidApprovalStatuses = ["Approved", "Rejected"];

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

        return ApiResponse<LeaveRequestDto>.Ok(GetDto(leave.Id), "Leave applied successfully");
    }

    public async Task<ApiResponse<LeaveRequestDto>> ApproveAsync(int id, int approverId, ApproveLeaveRequest request, int? approverInstituteId, bool isSuperAdmin)
    {
        var leave = await _unitOfWork.Repository<LeaveRequest>().GetByIdAsync(id);
        if (leave is null)
            return ApiResponse<LeaveRequestDto>.Fail("Leave request not found", "NOT_FOUND");

        var leaveUser = await _unitOfWork.Repository<User>().GetByIdAsync(leave.UserId);
        if (leaveUser is null)
            return ApiResponse<LeaveRequestDto>.Fail("Leave request user not found", "USER_NOT_FOUND");

        if (!isSuperAdmin && leaveUser.InstituteId != approverInstituteId)
            return ApiResponse<LeaveRequestDto>.Fail("Leave request is outside your institute", "FORBIDDEN");

        if (leave.Status != "Pending")
            return ApiResponse<LeaveRequestDto>.Fail("Leave request is already " + leave.Status, "ALREADY_PROCESSED");

        if (!ValidApprovalStatuses.Contains(request.Status))
            return ApiResponse<LeaveRequestDto>.Fail("Status must be Approved or Rejected", "INVALID_STATUS");

        leave.Status = request.Status;
        leave.ApprovedById = approverId;
        leave.ApprovedAt = DateTime.UtcNow;
        leave.Comments = request.Comments;
        leave.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<LeaveRequest>().Update(leave);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<LeaveRequestDto>.Ok(GetDto(leave.Id), $"Leave {request.Status.ToLower()} successfully");
    }

    public Task<ApiResponse<LeaveRequestDto>> GetByIdAsync(int id)
    {
        var leave = _unitOfWork.Repository<LeaveRequest>()
            .Query()
            .Where(l => l.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefault();
        if (leave is null)
            return Task.FromResult(ApiResponse<LeaveRequestDto>.Fail("Leave request not found", "NOT_FOUND"));

        return Task.FromResult(ApiResponse<LeaveRequestDto>.Ok(leave));
    }

    public Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetByUserAsync(int userId)
    {
        var records = _unitOfWork.Repository<LeaveRequest>()
            .Query()
            .Where(l => l.UserId == userId)
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetPendingAsync()
    {
        var records = _unitOfWork.Repository<LeaveRequest>()
            .Query()
            .Where(l => l.Status == "Pending")
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetAllAsync()
    {
        var records = _unitOfWork.Repository<LeaveRequest>()
            .Query()
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(records));
    }

    private static readonly System.Linq.Expressions.Expression<Func<LeaveRequest, LeaveRequestDto>> ProjectToDto = leave => new LeaveRequestDto
    {
        Id = leave.Id,
        UserId = leave.UserId,
        UserName = leave.User == null ? string.Empty : leave.User.LoginID,
        LeaveType = leave.LeaveType,
        StartDate = leave.StartDate,
        EndDate = leave.EndDate,
        Reason = leave.Reason,
        Status = leave.Status,
        ApprovedById = leave.ApprovedById,
        ApprovedByName = leave.ApprovedBy == null ? null : leave.ApprovedBy.LoginID,
        ApprovedAt = leave.ApprovedAt,
        Comments = leave.Comments,
        CreatedAt = leave.CreatedAt
    };

    private LeaveRequestDto GetDto(int id)
        => _unitOfWork.Repository<LeaveRequest>()
            .Query()
            .Where(l => l.Id == id)
            .Select(ProjectToDto)
            .First();
}
