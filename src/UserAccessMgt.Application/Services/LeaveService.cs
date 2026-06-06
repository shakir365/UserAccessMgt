using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Leave;
using UserAccessMgt.Application.DTOs.User;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class LeaveService : ILeaveService
{
    private const int AllDivisionLevelId = 1;
    private const int OwnDivisionLevelId = 2;
    private const int OwnDistrictLevelId = 3;
    private const int OwnThanaLevelId = 4;
    private const int OwnInstituteLevelId = 5;
    private const int OwnDataLevelId = 6;
    private const int OwnDepartmentsLevelId = 7;

    private static readonly string[] ValidApprovalStatuses = ["Approved", "Rejected"];
    private static readonly string[] ActiveLeaveStatuses = ["Pending", "Approved"];
    private static readonly TimeZoneInfo _bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        TryGetTimeZoneId("Bangladesh Standard Time", "Asia/Dhaka"));

    private readonly IUnitOfWork _unitOfWork;

    public LeaveService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<ApiResponse<IEnumerable<LeaveTypeDto>>> GetLeaveTypesAsync()
    {
        var leaveTypes = _unitOfWork.Repository<LeaveType>()
            .Query()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Id)
            .Select(t => new LeaveTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                IsActive = t.IsActive
            })
            .ToList()
            .AsEnumerable();

        return Task.FromResult(ApiResponse<IEnumerable<LeaveTypeDto>>.Ok(leaveTypes));
    }

    public Task<ApiResponse<UserDto>> GetSupervisorForUserAsync(int userId)
    {
        var user = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.InstituteId,
                RoleName = u.Role.Name,
                u.Role.UserDataViewLevelID,
                ThanaId = u.Institute.ThanaId,
                DistrictId = u.Institute.Thana == null ? null : (int?)u.Institute.Thana.DistrictId
            })
            .FirstOrDefault();

        if (user is null)
            return Task.FromResult(ApiResponse<UserDto>.Fail("User not found", "NOT_FOUND"));

        var supervisorResult = GetSupervisorUserId(
            user.Id,
            user.InstituteId,
            user.ThanaId,
            user.DistrictId,
            user.UserDataViewLevelID,
            user.RoleName);

        if (!supervisorResult.SupervisorUserId.HasValue)
            return Task.FromResult(ApiResponse<UserDto>.Fail(supervisorResult.Message, supervisorResult.ErrorCode));

        var supervisor = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == supervisorResult.SupervisorUserId.Value && u.IsActive)
            .Select(MapUserToDto)
            .FirstOrDefault();

        return supervisor is null
            ? Task.FromResult(ApiResponse<UserDto>.Fail("Supervisor not found", "SUPERVISOR_NOT_FOUND"))
            : Task.FromResult(ApiResponse<UserDto>.Ok(supervisor));
    }

    public async Task<ApiResponse<LeaveRequestDto>> ApplyAsync(CreateLeaveRequest request)
    {
        var user = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == request.UserId)
            .Select(u => new
            {
                u.Id,
                u.InstituteId,
                RoleName = u.Role.Name,
                u.Role.UserDataViewLevelID,
                ThanaId = u.Institute.ThanaId,
                DistrictId = u.Institute.Thana == null ? null : (int?)u.Institute.Thana.DistrictId
            })
            .FirstOrDefault();

        if (user is null)
            return ApiResponse<LeaveRequestDto>.Fail("User not found", "NOT_FOUND");

        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date;
        if (startDate > endDate)
            return ApiResponse<LeaveRequestDto>.Fail("Start date cannot be after end date", "INVALID_DATES");

        LeaveType? leaveType = null;
        var leaveTypeName = request.LeaveType.Trim();
        if (request.LeaveTypeId.HasValue)
        {
            leaveType = _unitOfWork.Repository<LeaveType>()
                .Query()
                .FirstOrDefault(t => t.Id == request.LeaveTypeId.Value && t.IsActive);

            if (leaveType is null)
                return ApiResponse<LeaveRequestDto>.Fail("Leave type not found", "LEAVE_TYPE_NOT_FOUND");

            leaveTypeName = leaveType.Name;
        }

        if (string.IsNullOrWhiteSpace(leaveTypeName))
            return ApiResponse<LeaveRequestDto>.Fail("Leave type is required", "LEAVE_TYPE_REQUIRED");

        if (string.IsNullOrWhiteSpace(request.Reason))
            return ApiResponse<LeaveRequestDto>.Fail("Reason is required", "REASON_REQUIRED");

        var hasOverlap = _unitOfWork.Repository<LeaveRequest>()
            .Query()
            .Any(l => l.UserId == request.UserId
                && ActiveLeaveStatuses.Contains(l.Status)
                && l.StartDate <= endDate
                && l.EndDate >= startDate);

        if (hasOverlap)
        {
            return ApiResponse<LeaveRequestDto>.Fail(
                "A pending or approved leave already exists within this date range",
                "LEAVE_DATE_OVERLAP");
        }

        var supervisorResult = GetSupervisorUserId(
            user.Id,
            user.InstituteId,
            user.ThanaId,
            user.DistrictId,
            user.UserDataViewLevelID,
            user.RoleName);

        if (!supervisorResult.SupervisorUserId.HasValue)
            return ApiResponse<LeaveRequestDto>.Fail(supervisorResult.Message, supervisorResult.ErrorCode);

        var now = GetBangladeshNow();
        var leave = new LeaveRequest
        {
            UserId = request.UserId,
            LeaveTypeId = leaveType?.Id,
            SupervisorUserId = supervisorResult.SupervisorUserId.Value,
            LeaveType = leaveTypeName,
            StartDate = startDate,
            EndDate = endDate,
            Reason = request.Reason.Trim(),
            Status = "Pending",
            CreatedAt = now
        };

        await _unitOfWork.Repository<LeaveRequest>().AddAsync(leave);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<LeaveRequestDto>.Ok(GetDto(leave.Id), "Leave applied successfully");
    }

    public async Task<ApiResponse<LeaveRequestDto>> ApproveAsync(int id, int approverId, ApproveLeaveRequest request, bool isSuperAdmin)
    {
        var leave = await _unitOfWork.Repository<LeaveRequest>().GetByIdAsync(id);
        if (leave is null)
            return ApiResponse<LeaveRequestDto>.Fail("Leave request not found", "NOT_FOUND");

        if (!isSuperAdmin && leave.SupervisorUserId != approverId)
            return ApiResponse<LeaveRequestDto>.Fail("Only the assigned supervisor can approve this leave request", "FORBIDDEN");

        if (leave.Status != "Pending")
            return ApiResponse<LeaveRequestDto>.Fail("Leave request is already " + leave.Status, "ALREADY_PROCESSED");

        if (!ValidApprovalStatuses.Contains(request.Status))
            return ApiResponse<LeaveRequestDto>.Fail("Status must be Approved or Rejected", "INVALID_STATUS");

        if (request.Status == "Rejected" && string.IsNullOrWhiteSpace(request.Comments))
            return ApiResponse<LeaveRequestDto>.Fail("Rejected reason is required", "REJECT_REASON_REQUIRED");

        var now = GetBangladeshNow();
        leave.Status = request.Status;
        leave.ApprovedById = approverId;
        leave.ApprovedAt = now;
        leave.Comments = request.Comments;
        leave.UpdatedAt = now;

        _unitOfWork.Repository<LeaveRequest>().Update(leave);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<LeaveRequestDto>.Ok(GetDto(leave.Id), $"Leave {request.Status.ToLower()} successfully");
    }

    public async Task<ApiResponse<LeaveRequestDto>> CancelAsync(int id, int userId, CancelLeaveRequest request)
    {
        var leave = await _unitOfWork.Repository<LeaveRequest>().GetByIdAsync(id);
        if (leave is null)
            return ApiResponse<LeaveRequestDto>.Fail("Leave request not found", "NOT_FOUND");

        if (leave.UserId != userId)
            return ApiResponse<LeaveRequestDto>.Fail("Only the applicant can cancel this leave request", "FORBIDDEN");

        if (leave.Status == "Cancelled")
            return ApiResponse<LeaveRequestDto>.Fail("Leave request is already cancelled", "ALREADY_CANCELLED");

        if (leave.Status == "Rejected")
            return ApiResponse<LeaveRequestDto>.Fail("Rejected leave cannot be cancelled", "INVALID_STATUS");

        var today = GetBangladeshNow().Date;
        if (leave.StartDate.Date < today)
            return ApiResponse<LeaveRequestDto>.Fail("Leave date has already crossed", "LEAVE_DATE_CROSSED");

        leave.Status = "Cancelled";
        leave.Comments = string.IsNullOrWhiteSpace(request.Comments)
            ? leave.Comments
            : request.Comments.Trim();
        leave.UpdatedAt = GetBangladeshNow();

        _unitOfWork.Repository<LeaveRequest>().Update(leave);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<LeaveRequestDto>.Ok(GetDto(leave.Id), "Leave cancelled successfully");
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
            .OrderByDescending(l => l.StartDate)
            .ThenByDescending(l => l.Id)
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetPendingForSupervisorAsync(int supervisorUserId, bool isSuperAdmin)
    {
        var query = _unitOfWork.Repository<LeaveRequest>()
            .Query()
            .Where(l => l.Status == "Pending");

        if (!isSuperAdmin)
            query = query.Where(l => l.SupervisorUserId == supervisorUserId);

        var records = query
            .OrderBy(l => l.StartDate)
            .ThenBy(l => l.Id)
            .Select(ProjectToDto)
            .ToList();

        return Task.FromResult(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<LeaveRequestDto>>> GetAllAsync()
    {
        var records = _unitOfWork.Repository<LeaveRequest>()
            .Query()
            .OrderByDescending(l => l.StartDate)
            .ThenByDescending(l => l.Id)
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<LeaveRequestDto>>.Ok(records));
    }

    private static readonly System.Linq.Expressions.Expression<Func<LeaveRequest, LeaveRequestDto>> ProjectToDto = leave => new LeaveRequestDto
    {
        Id = leave.Id,
        UserId = leave.UserId,
        UserName = leave.User == null ? string.Empty : leave.User.LoginID,
        LeaveTypeId = leave.LeaveTypeId,
        LeaveType = leave.LeaveType,
        StartDate = leave.StartDate,
        EndDate = leave.EndDate,
        Reason = leave.Reason,
        Status = leave.Status,
        SupervisorUserId = leave.SupervisorUserId,
        SupervisorName = leave.SupervisorUser == null
            ? null
            : ((leave.SupervisorUser.FirstName ?? string.Empty) + " " + (leave.SupervisorUser.LastName ?? string.Empty)).Trim(),
        SupervisorLoginID = leave.SupervisorUser == null ? null : leave.SupervisorUser.LoginID,
        SupervisorDesignationName = leave.SupervisorUser == null || leave.SupervisorUser.Designation == null ? null : leave.SupervisorUser.Designation.DesignationNameEN,
        ApprovedById = leave.ApprovedById,
        ApprovedByName = leave.ApprovedBy == null ? null : leave.ApprovedBy.LoginID,
        ApprovedAt = leave.ApprovedAt,
        Comments = leave.Comments,
        CreatedAt = leave.CreatedAt
    };

    private static readonly System.Linq.Expressions.Expression<Func<User, UserDto>> MapUserToDto = user => new UserDto
    {
        Id = user.Id,
        LoginID = user.LoginID,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        MobileNumber = user.MobileNumber,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        InstituteId = user.InstituteId,
        InstituteName = user.Institute == null ? string.Empty : user.Institute.InstituteNameEN,
        RoleId = user.RoleId,
        RoleName = user.Role == null ? string.Empty : user.Role.Name,
        UserDataViewLevelID = user.Role == null ? null : user.Role.UserDataViewLevelID,
        DataViewLevel = user.Role == null || user.Role.UserDataViewLevel == null ? null : user.Role.UserDataViewLevel.DataViewLevel,
        RelatedRoleInfo = user.Role == null || user.Role.UserDataViewLevel == null ? null : user.Role.UserDataViewLevel.RelatedRoleInfo,
        GradeId = user.GradeId,
        GradeName = user.Grade == null ? null : user.Grade.GradeNameEN,
        DesignationId = user.DesignationId,
        DesignationName = user.Designation == null ? null : user.Designation.DesignationNameEN
    };

    private LeaveRequestDto GetDto(int id)
        => _unitOfWork.Repository<LeaveRequest>()
            .Query()
            .Where(l => l.Id == id)
            .Select(ProjectToDto)
            .First();

    private (int? SupervisorUserId, string Message, string ErrorCode) GetSupervisorUserId(
        int userId,
        int instituteId,
        int? thanaId,
        int? districtId,
        int? userDataViewLevelId,
        string roleName)
    {
        var today = GetBangladeshNow().Date;
        var directSupervisorId = _unitOfWork.Repository<UserDirectSupervisor>()
            .Query()
            .Where(s => s.UserID == userId
                && s.ActiveDateFrom <= today
                && (!s.ExpireDate.HasValue || s.ExpireDate.Value >= today)
                && s.SupervisorUser != null
                && s.SupervisorUser.IsActive)
            .Select(s => (int?)s.Supervisor_UserID)
            .FirstOrDefault();

        if (directSupervisorId.HasValue)
            return (directSupervisorId.Value, string.Empty, string.Empty);

        var supervisorLevelId = GetSupervisorLevelId(GetDataViewLevelId(userDataViewLevelId, roleName));
        if (!supervisorLevelId.HasValue)
        {
            return (
                null,
                "Supervisor is not configured for this user data view level",
                "SUPERVISOR_LEVEL_NOT_CONFIGURED");
        }

        var supervisorRoleIds = _unitOfWork.Repository<Role>()
            .Query()
            .ToList()
            .Where(r => GetDataViewLevelId(r.UserDataViewLevelID, r.Name) == supervisorLevelId.Value)
            .Select(r => r.Id)
            .ToList();

        if (supervisorRoleIds.Count == 0)
            return (null, "Supervisor role is not configured", "SUPERVISOR_ROLE_NOT_CONFIGURED");

        var supervisorId = GetSupervisorQuery(
                userId,
                instituteId,
                thanaId,
                districtId,
                supervisorLevelId.Value,
                supervisorRoleIds)
            .OrderBy(u => u.Id)
            .Select(u => (int?)u.Id)
            .FirstOrDefault();

        return supervisorId.HasValue
            ? (supervisorId.Value, string.Empty, string.Empty)
            : (null, "Supervisor not found", "SUPERVISOR_NOT_FOUND");
    }

    private IQueryable<User> GetSupervisorQuery(
        int userId,
        int instituteId,
        int? thanaId,
        int? districtId,
        int supervisorLevelId,
        IReadOnlyCollection<int> supervisorRoleIds)
    {
        var users = _unitOfWork.Repository<User>().Query();
        var institutes = _unitOfWork.Repository<Institute>().Query();
        var roles = _unitOfWork.Repository<Role>().Query();
        var thanas = _unitOfWork.Repository<Thana>().Query();

        if (supervisorLevelId == OwnInstituteLevelId)
        {
            return users
                .Where(u => u.Id != userId
                    && u.IsActive
                    && supervisorRoleIds.Contains(u.RoleId)
                    && u.InstituteId == instituteId);
        }

        if (supervisorLevelId == OwnThanaLevelId)
        {
            return from u in users
                   join i in institutes on u.InstituteId equals i.Id
                   where u.Id != userId
                       && u.IsActive
                       && supervisorRoleIds.Contains(u.RoleId)
                       && i.ThanaId == thanaId
                   select u;
        }

        if (supervisorLevelId == OwnDistrictLevelId)
        {
            return from u in users
                   join i in institutes on u.InstituteId equals i.Id
                   join t in thanas on i.ThanaId equals t.ThanaId
                   where u.Id != userId
                       && u.IsActive
                       && supervisorRoleIds.Contains(u.RoleId)
                       && t.DistrictId == districtId
                   select u;
        }

        if (supervisorLevelId == OwnDivisionLevelId)
        {
            var divisionId = (from t in thanas
                              where t.DistrictId == districtId
                              select (int?)t.District.DivisionId)
                .FirstOrDefault();

            return from u in users
                   join i in institutes on u.InstituteId equals i.Id
                   join t in thanas on i.ThanaId equals t.ThanaId
                   where u.Id != userId
                       && u.IsActive
                       && supervisorRoleIds.Contains(u.RoleId)
                       && t.District.DivisionId == divisionId
                   select u;
        }

        if (supervisorLevelId == AllDivisionLevelId)
        {
            return from u in users
                   join r in roles on u.RoleId equals r.Id
                   where u.Id != userId
                       && u.IsActive
                       && supervisorRoleIds.Contains(u.RoleId)
                   select u;
        }

        return users.Where(u => false);
    }

    private static int? GetSupervisorLevelId(int? userDataViewLevelId)
        => userDataViewLevelId switch
        {
            OwnDivisionLevelId => AllDivisionLevelId,
            OwnDistrictLevelId => OwnDivisionLevelId,
            OwnThanaLevelId => OwnDistrictLevelId,
            OwnInstituteLevelId => OwnThanaLevelId,
            OwnDataLevelId => OwnInstituteLevelId,
            OwnDepartmentsLevelId => OwnInstituteLevelId,
            _ => null
        };

    private static int? GetDataViewLevelId(int? userDataViewLevelId, string? roleName)
    {
        if (userDataViewLevelId.HasValue)
            return userDataViewLevelId.Value;

        return roleName?.Trim().ToLowerInvariant() switch
        {
            "superadmin" => AllDivisionLevelId,
            "divisionaladmin" => OwnDivisionLevelId,
            "districtadmin" or "disrtictadmin" => OwnDistrictLevelId,
            "thanaadmin" => OwnThanaLevelId,
            "instituteadmin" => OwnInstituteLevelId,
            "departmentaladmin" => OwnDepartmentsLevelId,
            "user" => OwnDataLevelId,
            _ => null
        };
    }

    private static DateTime GetBangladeshNow()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _bdTimeZone);

    private static string TryGetTimeZoneId(string windowsId, string linuxId)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(windowsId);
            return windowsId;
        }
        catch (TimeZoneNotFoundException)
        {
            return linuxId;
        }
        catch (InvalidTimeZoneException)
        {
            return linuxId;
        }
    }
}
