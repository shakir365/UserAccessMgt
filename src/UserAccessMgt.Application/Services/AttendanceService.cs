using UserAccessMgt.Application.DTOs.Attendance;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private const int AllDivisionLevelId = 1;
    private const int OwnDivisionLevelId = 2;
    private const int OwnDistrictLevelId = 3;
    private const int OwnThanaLevelId = 4;
    private const int OwnInstituteLevelId = 5;
    private const int OwnDataLevelId = 6;
    private const int OwnDepartmentsLevelId = 7;
    private const string GpsRequiredMessage = "Unable to read GPS latitude-longitude. Please allow Location permission and try again.";
    private static readonly string[] ValidStatuses = ["Present", "Absent", "Late", "OnLeave"];
    private static readonly TimeZoneInfo _bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        TryGetTimeZoneId("Bangladesh Standard Time", "Asia/Dhaka"));

    private static string TryGetTimeZoneId(string windowsId, string ianaId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId) is not null ? windowsId : ianaId;
        }
        catch
        {
            return ianaId;
        }
    }

    private static DateTime BangladeshNow =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _bdTimeZone);

    public AttendanceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AttendanceDto>> CreateAsync(CreateAttendanceRequest request, int submittedByUserId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId);
        if (user is null)
            return ApiResponse<AttendanceDto>.Fail("User not found", "NOT_FOUND");

        var institute = await _unitOfWork.Repository<Institute>().GetByIdAsync(request.InstituteId);
        if (institute is null)
            return ApiResponse<AttendanceDto>.Fail("Institute not found", "INSTITUTE_NOT_FOUND");

        if (user.InstituteId != request.InstituteId)
            return ApiResponse<AttendanceDto>.Fail("User does not belong to this institute", "INSTITUTE_MISMATCH");

        var status = string.IsNullOrWhiteSpace(request.Status) ? "Present" : request.Status.Trim();
        if (!ValidStatuses.Contains(status))
            return ApiResponse<AttendanceDto>.Fail("Invalid attendance status", "INVALID_STATUS");

        var attendanceDate = (request.Date ?? BangladeshNow).Date;
        var submissionStatus = GetSubmissionStatus(attendanceDate);
        if (!submissionStatus.IsAllowed)
            return ApiResponse<AttendanceDto>.Fail(submissionStatus.Message, submissionStatus.ReasonCode);

        var existing = await _unitOfWork.Repository<Attendance>()
            .FirstOrDefaultAsync(a => a.UserId == request.UserId && a.Date >= attendanceDate && a.Date < attendanceDate.AddDays(1));
        if (existing is not null)
        {
            if (string.IsNullOrWhiteSpace(request.CheckOutLatitudeLongitude))
                return ApiResponse<AttendanceDto>.Fail(GpsRequiredMessage, "GPS_REQUIRED");

            existing.CheckOutTime = request.CheckOutTime ?? BangladeshNow;
            existing.CheckOutLatitudeLongitude = request.CheckOutLatitudeLongitude?.Trim();
            _unitOfWork.Repository<Attendance>().Update(existing);
            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<AttendanceDto>.Ok(GetDto(existing.Id), "Check-out updated successfully");
        }

        if (string.IsNullOrWhiteSpace(request.CheckInLatitudeLongitude))
            return ApiResponse<AttendanceDto>.Fail(GpsRequiredMessage, "GPS_REQUIRED");

        var now = BangladeshNow;
        var checkInTime = request.CheckInTime ?? now;
        if (status == "Present")
        {
            var shift = _unitOfWork.Repository<Shift>()
                .Query()
                .Where(s => s.IsActive)
                .OrderBy(s => s.StartTime)
                .FirstOrDefault();
            if (shift is not null && checkInTime.TimeOfDay > shift.StartTime.Add(TimeSpan.FromMinutes(shift.LateAfterMinutes)))
                status = "Late";
        }

        var attendance = new Attendance
        {
            UserId = request.UserId,
            Date = attendanceDate,
            CheckInTime = checkInTime,
            CheckOutTime = request.CheckOutTime,
            CheckInLatitudeLongitude = request.CheckInLatitudeLongitude?.Trim(),
            CheckOutLatitudeLongitude = request.CheckOutLatitudeLongitude?.Trim(),
            Status = status,
            Notes = request.Notes,
            InstituteId = request.InstituteId,
            SubmittedByUserId = submittedByUserId,
            CreatedAt = now
        };

        await _unitOfWork.Repository<Attendance>().AddAsync(attendance);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<AttendanceDto>.Ok(GetDto(attendance.Id), "Attendance recorded successfully");
    }

    public Task<ApiResponse<AttendanceDto>> GetByIdAsync(int id)
    {
        var attendance = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefault();
        if (attendance is null)
            return Task.FromResult(ApiResponse<AttendanceDto>.Fail("Attendance not found", "NOT_FOUND"));

        return Task.FromResult(ApiResponse<AttendanceDto>.Ok(attendance));
    }

    public Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByUserAsync(int userId)
    {
        var records = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.UserId == userId)
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByDateAsync(DateTime date)
    {
        var attendanceDate = date.Date;
        var records = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.Date >= attendanceDate && a.Date < attendanceDate.AddDays(1))
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByInstituteAsync(int instituteId)
    {
        var records = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.InstituteId == instituteId)
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByInstituteAndDateRangeAsync(int instituteId, DateTime from, DateTime to)
    {
        if (from.Date > to.Date)
            return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Fail("From date cannot be after to date", "INVALID_DATES"));

        var fromDate = from.Date;
        var toDate = to.Date;
        var toDateExclusive = toDate.AddDays(1);
        var users = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.InstituteId == instituteId && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.LoginID,
                u.InstituteId,
                InstituteName = u.Institute == null ? string.Empty : u.Institute.InstituteNameEN
            })
            .ToList();
        if (users.Count == 0)
            return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Ok([]));

        var userIds = users.Select(u => u.Id).ToList();
        var attendanceByUserDate = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.InstituteId == instituteId
                && userIds.Contains(a.UserId)
                && a.Date >= fromDate
                && a.Date < toDateExclusive)
            .Select(ProjectToDto)
            .ToList()
            .GroupBy(a => (a.UserId, Date: a.Date.Date))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.Id).First());

        var leaveByUserDate = GetApprovedLeaveDays(userIds, fromDate, toDate);

        var holidayByDate = _unitOfWork.Repository<Holiday>()
            .Query()
            .Where(h => h.IsActive && h.HolidayDate >= fromDate && h.HolidayDate < toDateExclusive)
            .Select(h => new { Date = h.HolidayDate, h.HolidayName })
            .ToList()
            .GroupBy(h => h.Date.Date)
            .ToDictionary(g => g.Key, g => g.First().HolidayName);

        var weekendDays = _unitOfWork.Repository<Weekend>()
            .Query()
            .Where(w => w.IsActive)
            .Select(w => w.DayOfWeek)
            .ToHashSet();

        var records = new List<AttendanceDto>();
        foreach (var user in users.OrderBy(u => u.LoginID))
        {
            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                if (leaveByUserDate.TryGetValue((user.Id, date), out var leaveType))
                {
                    records.Add(CreateGeneratedReportRow(user.Id, user.LoginID, user.InstituteId, user.InstituteName, date, "Leave", leaveType));
                    continue;
                }

                if (holidayByDate.TryGetValue(date, out var holidayName))
                {
                    records.Add(CreateGeneratedReportRow(user.Id, user.LoginID, user.InstituteId, user.InstituteName, date, "Holiday", holidayName));
                    continue;
                }

                if (weekendDays.Contains((int)date.DayOfWeek))
                {
                    records.Add(CreateGeneratedReportRow(user.Id, user.LoginID, user.InstituteId, user.InstituteName, date, "Weekend", "Weekend"));
                    continue;
                }

                if (attendanceByUserDate.TryGetValue((user.Id, date), out var attendance))
                {
                    records.Add(attendance);
                    continue;
                }

                records.Add(CreateGeneratedReportRow(user.Id, user.LoginID, user.InstituteId, user.InstituteName, date, "Absent", "No attendance record found"));
            }
        }

        return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByUserAndDateRangeAsync(int userId, DateTime from, DateTime to)
    {
        if (from.Date > to.Date)
            return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Fail("From date cannot be after to date", "INVALID_DATES"));

        var fromDate = from.Date;
        var toDate = to.Date;
        var toDateExclusive = toDate.AddDays(1);
        var user = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.LoginID,
                u.InstituteId,
                InstituteName = u.Institute == null ? string.Empty : u.Institute.InstituteNameEN
            })
            .FirstOrDefault();
        if (user is null)
            return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Fail("User not found", "NOT_FOUND"));

        var attendanceByDate = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.UserId == userId && a.Date >= fromDate && a.Date < toDateExclusive)
            .Select(ProjectToDto)
            .ToList()
            .GroupBy(a => a.Date.Date)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.Id).First());

        var leaveByDate = GetApprovedLeaveDays(userId, fromDate, toDate);

        var holidayByDate = _unitOfWork.Repository<Holiday>()
            .Query()
            .Where(h => h.IsActive && h.HolidayDate >= fromDate && h.HolidayDate < toDateExclusive)
            .Select(h => new { Date = h.HolidayDate, h.HolidayName })
            .ToList()
            .GroupBy(h => h.Date.Date)
            .ToDictionary(g => g.Key, g => g.First().HolidayName);

        var weekendDays = _unitOfWork.Repository<Weekend>()
            .Query()
            .Where(w => w.IsActive)
            .Select(w => w.DayOfWeek)
            .ToHashSet();

        var records = new List<AttendanceDto>();
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (leaveByDate.TryGetValue(date, out var leaveType))
            {
                records.Add(CreateGeneratedReportRow(user.Id, user.LoginID, user.InstituteId, user.InstituteName, date, "Leave", leaveType));
                continue;
            }

            if (holidayByDate.TryGetValue(date, out var holidayName))
            {
                records.Add(CreateGeneratedReportRow(user.Id, user.LoginID, user.InstituteId, user.InstituteName, date, "Holiday", holidayName));
                continue;
            }

            if (weekendDays.Contains((int)date.DayOfWeek))
            {
                records.Add(CreateGeneratedReportRow(user.Id, user.LoginID, user.InstituteId, user.InstituteName, date, "Weekend", "Weekend"));
                continue;
            }

            if (attendanceByDate.TryGetValue(date, out var attendance))
            {
                records.Add(attendance);
                continue;
            }

            records.Add(CreateGeneratedReportRow(user.Id, user.LoginID, user.InstituteId, user.InstituteName, date, "Absent", "No attendance record found"));
        }

        return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    public Task<ApiResponse<AttendanceSubmissionStatusDto>> GetSubmissionStatusAsync(DateTime? date)
    {
        var attendanceDate = (date ?? BangladeshNow).Date;
        return Task.FromResult(ApiResponse<AttendanceSubmissionStatusDto>.Ok(GetSubmissionStatus(attendanceDate)));
    }

    public Task<ApiResponse<AttendanceAnalyticalReportDto>> GetAnalyticalReportAsync(AttendanceAnalyticalReportRequest request, int requesterUserId)
    {
        var fromDate = (request.From ?? BangladeshNow).Date;
        var toDate = (request.To ?? fromDate).Date;
        if (fromDate > toDate)
            return Task.FromResult(ApiResponse<AttendanceAnalyticalReportDto>.Fail("From date cannot be after to date", "INVALID_DATES"));

        var period = NormalizeAnalyticsPeriod(request.Period);
        var requester = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == requesterUserId)
            .Select(u => new
            {
                u.Id,
                u.InstituteId,
                RoleName = u.Role == null ? string.Empty : u.Role.Name,
                UserDataViewLevelId = u.Role == null ? null : u.Role.UserDataViewLevelID,
                ThanaId = u.Institute == null ? null : u.Institute.ThanaId,
                DistrictId = u.Institute == null || u.Institute.Thana == null ? null : (int?)u.Institute.Thana.DistrictId,
                DivisionId = u.Institute == null || u.Institute.Thana == null || u.Institute.Thana.District == null ? null : (int?)u.Institute.Thana.District.DivisionId
            })
            .FirstOrDefault();
        if (requester is null)
            return Task.FromResult(ApiResponse<AttendanceAnalyticalReportDto>.Fail("User not found", "NOT_FOUND"));

        var dataViewLevelId = GetDataViewLevelId(requester.UserDataViewLevelId, requester.RoleName);
        var allInstitutes = _unitOfWork.Repository<Institute>()
            .Query()
            .Where(i => i.IsActive)
            .Select(i => new AnalyticsInstituteInfo
            {
                InstituteId = i.Id,
                InstituteName = i.InstituteNameEN,
                ThanaId = i.ThanaId,
                ThanaName = i.Thana == null ? null : i.Thana.ThanaNameEN,
                DistrictId = i.Thana == null ? null : (int?)i.Thana.DistrictId,
                DistrictName = i.Thana == null || i.Thana.District == null ? null : i.Thana.District.DistrictNameEN,
                DivisionId = i.Thana == null || i.Thana.District == null ? null : (int?)i.Thana.District.DivisionId,
                DivisionName = i.Thana == null || i.Thana.District == null || i.Thana.District.Division == null ? null : i.Thana.District.Division.DivisionNameEN
            })
            .ToList();

        var accessibleInstitutes = ApplyAnalyticsAccessScope(
            allInstitutes,
            dataViewLevelId,
            requester.InstituteId,
            requester.ThanaId,
            requester.DistrictId,
            requester.DivisionId).ToList();

        var filterOptions = BuildAnalyticsFilterOptions(accessibleInstitutes);
        var filteredInstitutes = accessibleInstitutes
            .Where(i => !request.DivisionId.HasValue || i.DivisionId == request.DivisionId)
            .Where(i => !request.DistrictId.HasValue || i.DistrictId == request.DistrictId)
            .Where(i => !request.ThanaId.HasValue || i.ThanaId == request.ThanaId)
            .Where(i => !request.InstituteId.HasValue || i.InstituteId == request.InstituteId)
            .OrderBy(i => i.InstituteName)
            .ToList();

        if (filteredInstitutes.Count == 0)
        {
            return Task.FromResult(ApiResponse<AttendanceAnalyticalReportDto>.Ok(new AttendanceAnalyticalReportDto
            {
                From = fromDate,
                To = toDate,
                Period = period,
                FilterOptions = filterOptions,
                Rows = []
            }));
        }

        var instituteIds = filteredInstitutes.Select(i => i.InstituteId).ToList();
        var users = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.IsActive && instituteIds.Contains(u.InstituteId))
            .Where(u => dataViewLevelId != OwnDataLevelId || u.Id == requesterUserId)
            .Select(u => new AnalyticsUserInfo
            {
                UserId = u.Id,
                InstituteId = u.InstituteId
            })
            .ToList();

        var userIds = users.Select(u => u.UserId).ToList();
        var toDateExclusive = toDate.AddDays(1);
        var attendanceByUserDate = userIds.Count == 0
            ? new Dictionary<(int UserId, DateTime Date), AttendanceDto>()
            : _unitOfWork.Repository<Attendance>()
                .Query()
                .Where(a => userIds.Contains(a.UserId)
                    && a.Date >= fromDate
                    && a.Date < toDateExclusive)
                .Select(ProjectToDto)
                .ToList()
                .GroupBy(a => (a.UserId, Date: a.Date.Date))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.Id).First());

        var leaveByUserDate = userIds.Count == 0
            ? new Dictionary<(int UserId, DateTime Date), string>()
            : GetApprovedLeaveDays(userIds, fromDate, toDate);

        var holidayDates = _unitOfWork.Repository<Holiday>()
            .Query()
            .Where(h => h.IsActive && h.HolidayDate >= fromDate && h.HolidayDate < toDateExclusive)
            .Select(h => h.HolidayDate)
            .ToList()
            .Select(h => h.Date)
            .ToHashSet();

        var weekendDays = _unitOfWork.Repository<Weekend>()
            .Query()
            .Where(w => w.IsActive)
            .Select(w => w.DayOfWeek)
            .ToHashSet();

        var buckets = GetAnalyticsPeriodBuckets(fromDate, toDate, period);
        var rows = new List<AttendanceAnalyticalReportRowDto>();
        foreach (var institute in filteredInstitutes)
        {
            var instituteUsers = users.Where(u => u.InstituteId == institute.InstituteId).ToList();
            foreach (var bucket in buckets)
            {
                var row = new AttendanceAnalyticalReportRowDto
                {
                    PeriodStart = bucket.Start,
                    PeriodEnd = bucket.End,
                    PeriodLabel = bucket.Label,
                    InstituteId = institute.InstituteId,
                    InstituteName = institute.InstituteName,
                    ThanaId = institute.ThanaId,
                    ThanaName = institute.ThanaName,
                    DistrictId = institute.DistrictId,
                    DistrictName = institute.DistrictName,
                    DivisionId = institute.DivisionId,
                    DivisionName = institute.DivisionName,
                    InstituteCount = 1,
                    TeacherCount = instituteUsers.Count
                };

                FillAnalyticsCounts(row, instituteUsers, bucket.Start, bucket.End, weekendDays, holidayDates, attendanceByUserDate, leaveByUserDate);
                rows.Add(row);
            }
        }

        var summary = new AttendanceAnalyticalSummaryDto
        {
            InstituteCount = filteredInstitutes.Count,
            TeacherCount = users.Count,
            ExpectedCount = rows.Sum(r => r.ExpectedCount),
            PresentCount = rows.Sum(r => r.PresentCount),
            LateCount = rows.Sum(r => r.LateCount),
            LeaveCount = rows.Sum(r => r.LeaveCount),
            AbsentCount = rows.Sum(r => r.AbsentCount)
        };
        SetAnalyticsRates(summary);

        return Task.FromResult(ApiResponse<AttendanceAnalyticalReportDto>.Ok(new AttendanceAnalyticalReportDto
        {
            From = fromDate,
            To = toDate,
            Period = period,
            Summary = summary,
            FilterOptions = filterOptions,
            Rows = rows
        }));
    }

    public Task<ApiResponse<AttendancePersonalAnalyticalReportDto>> GetPersonalAnalyticalReportAsync(AttendanceAnalyticalReportRequest request, int requesterUserId)
    {
        var fromDate = (request.From ?? BangladeshNow).Date;
        var toDate = (request.To ?? fromDate).Date;
        if (fromDate > toDate)
            return Task.FromResult(ApiResponse<AttendancePersonalAnalyticalReportDto>.Fail("From date cannot be after to date", "INVALID_DATES"));

        var period = NormalizeAnalyticsPeriod(request.Period);
        var requester = GetAnalyticsRequester(requesterUserId);
        if (requester is null)
            return Task.FromResult(ApiResponse<AttendancePersonalAnalyticalReportDto>.Fail("User not found", "NOT_FOUND"));

        var dataViewLevelId = GetDataViewLevelId(requester.UserDataViewLevelId, requester.RoleName);
        var allInstitutes = GetAnalyticsInstitutes();
        var accessibleInstitutes = ApplyAnalyticsAccessScope(
            allInstitutes,
            dataViewLevelId,
            requester.InstituteId,
            requester.ThanaId,
            requester.DistrictId,
            requester.DivisionId).ToList();

        var filteredInstitutes = accessibleInstitutes
            .Where(i => !request.DivisionId.HasValue || i.DivisionId == request.DivisionId)
            .Where(i => !request.DistrictId.HasValue || i.DistrictId == request.DistrictId)
            .Where(i => !request.ThanaId.HasValue || i.ThanaId == request.ThanaId)
            .Where(i => !request.InstituteId.HasValue || i.InstituteId == request.InstituteId)
            .OrderBy(i => i.InstituteName)
            .ToList();

        var instituteIds = filteredInstitutes.Select(i => i.InstituteId).ToList();
        var users = GetAnalyticsUsers(instituteIds, dataViewLevelId, requesterUserId)
            .Where(u => !request.UserId.HasValue || u.UserId == request.UserId.Value)
            .OrderBy(u => u.UserName)
            .ToList();

        var filterOptions = BuildAnalyticsFilterOptions(accessibleInstitutes, GetAnalyticsUsers(accessibleInstitutes.Select(i => i.InstituteId).ToList(), dataViewLevelId, requesterUserId));
        if (filteredInstitutes.Count == 0 || users.Count == 0)
        {
            return Task.FromResult(ApiResponse<AttendancePersonalAnalyticalReportDto>.Ok(new AttendancePersonalAnalyticalReportDto
            {
                From = fromDate,
                To = toDate,
                Period = period,
                FilterOptions = filterOptions,
                Rows = []
            }));
        }

        var userIds = users.Select(u => u.UserId).ToList();
        var toDateExclusive = toDate.AddDays(1);
        var attendanceByUserDate = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => userIds.Contains(a.UserId)
                && a.Date >= fromDate
                && a.Date < toDateExclusive)
            .Select(ProjectToDto)
            .ToList()
            .GroupBy(a => (a.UserId, Date: a.Date.Date))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.Id).First());

        var leaveByUserDate = GetApprovedLeaveDays(userIds, fromDate, toDate);
        var holidayDates = _unitOfWork.Repository<Holiday>()
            .Query()
            .Where(h => h.IsActive && h.HolidayDate >= fromDate && h.HolidayDate < toDateExclusive)
            .Select(h => h.HolidayDate)
            .ToList()
            .Select(h => h.Date)
            .ToHashSet();

        var weekendDays = _unitOfWork.Repository<Weekend>()
            .Query()
            .Where(w => w.IsActive)
            .Select(w => w.DayOfWeek)
            .ToHashSet();

        var instituteMap = filteredInstitutes.ToDictionary(i => i.InstituteId);
        var buckets = GetAnalyticsPeriodBuckets(fromDate, toDate, period);
        var rows = new List<AttendancePersonalAnalyticalReportRowDto>();
        foreach (var user in users)
        {
            if (!instituteMap.TryGetValue(user.InstituteId, out var institute))
                continue;

            foreach (var bucket in buckets)
            {
                var row = new AttendancePersonalAnalyticalReportRowDto
                {
                    PeriodStart = bucket.Start,
                    PeriodEnd = bucket.End,
                    PeriodLabel = bucket.Label,
                    UserId = user.UserId,
                    UserName = user.UserName,
                    LoginId = user.LoginId,
                    DesignationName = user.DesignationName,
                    InstituteId = institute.InstituteId,
                    InstituteName = institute.InstituteName,
                    ThanaId = institute.ThanaId,
                    ThanaName = institute.ThanaName,
                    DistrictId = institute.DistrictId,
                    DistrictName = institute.DistrictName,
                    DivisionId = institute.DivisionId,
                    DivisionName = institute.DivisionName,
                    InstituteCount = 1,
                    TeacherCount = 1
                };

                FillAnalyticsCounts(row, [user], bucket.Start, bucket.End, weekendDays, holidayDates, attendanceByUserDate, leaveByUserDate);
                if (period == "Day")
                    FillPersonalDayDetails(row, user.UserId, bucket.Start, weekendDays, holidayDates, attendanceByUserDate, leaveByUserDate);

                rows.Add(row);
            }
        }

        var summary = new AttendanceAnalyticalSummaryDto
        {
            InstituteCount = rows.Select(r => r.InstituteId).Distinct().Count(),
            TeacherCount = users.Count,
            ExpectedCount = rows.Sum(r => r.ExpectedCount),
            PresentCount = rows.Sum(r => r.PresentCount),
            LateCount = rows.Sum(r => r.LateCount),
            LeaveCount = rows.Sum(r => r.LeaveCount),
            AbsentCount = rows.Sum(r => r.AbsentCount)
        };
        SetAnalyticsRates(summary);

        return Task.FromResult(ApiResponse<AttendancePersonalAnalyticalReportDto>.Ok(new AttendancePersonalAnalyticalReportDto
        {
            From = fromDate,
            To = toDate,
            Period = period,
            Summary = summary,
            FilterOptions = filterOptions,
            Rows = rows
        }));
    }

    public async Task<ApiResponse<AttendanceDto>> UpdateAsync(int id, UpdateAttendanceRequest request)
    {
        var attendance = await _unitOfWork.Repository<Attendance>().GetByIdAsync(id);
        if (attendance is null)
            return ApiResponse<AttendanceDto>.Fail("Attendance not found", "NOT_FOUND");

        if (request.CheckInTime.HasValue) attendance.CheckInTime = request.CheckInTime;
        if (request.CheckOutTime.HasValue) attendance.CheckOutTime = request.CheckOutTime;
        else if (request.CheckOutLatitudeLongitude is not null) attendance.CheckOutTime = BangladeshNow;
        if (request.CheckInLatitudeLongitude is not null) attendance.CheckInLatitudeLongitude = request.CheckInLatitudeLongitude.Trim();
        if (request.CheckOutLatitudeLongitude is not null) attendance.CheckOutLatitudeLongitude = request.CheckOutLatitudeLongitude.Trim();
        if (request.Status is not null)
        {
            if (!ValidStatuses.Contains(request.Status))
                return ApiResponse<AttendanceDto>.Fail("Invalid attendance status", "INVALID_STATUS");

            attendance.Status = request.Status;
        }
        if (request.Notes is not null) attendance.Notes = request.Notes;

        _unitOfWork.Repository<Attendance>().Update(attendance);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<AttendanceDto>.Ok(GetDto(attendance.Id), "Attendance updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id)
    {
        var attendance = await _unitOfWork.Repository<Attendance>().GetByIdAsync(id);
        if (attendance is null)
            return ApiResponse<string>.Fail("Attendance not found", "NOT_FOUND");

        _unitOfWork.Repository<Attendance>().Remove(attendance);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Attendance deleted successfully");
    }

    private AttendanceSubmissionStatusDto GetSubmissionStatus(DateTime attendanceDate)
    {
        var holiday = _unitOfWork.Repository<Holiday>()
            .Query()
            .Where(h => h.IsActive
                && h.HolidayDate >= attendanceDate
                && h.HolidayDate < attendanceDate.AddDays(1))
            .Select(h => h.HolidayName)
            .FirstOrDefault();
        if (holiday is not null)
        {
            return new AttendanceSubmissionStatusDto
            {
                Date = attendanceDate,
                IsAllowed = false,
                ReasonCode = "HOLIDAY",
                Message = $"Attendance submission is not allowed today because it is holiday: {holiday}"
            };
        }

        var dayOfWeek = (int)attendanceDate.DayOfWeek;
        var isWeekend = _unitOfWork.Repository<Weekend>()
            .Query()
            .Any(w => w.IsActive
                && w.DayOfWeek == dayOfWeek);
        if (isWeekend)
        {
            return new AttendanceSubmissionStatusDto
            {
                Date = attendanceDate,
                IsAllowed = false,
                ReasonCode = "WEEKEND",
                Message = "Attendance submission is not allowed today because it is weekend"
            };
        }

        return new AttendanceSubmissionStatusDto
        {
            Date = attendanceDate,
            IsAllowed = true,
            Message = "Attendance submission is allowed today"
        };
    }

    private AnalyticsRequesterInfo? GetAnalyticsRequester(int requesterUserId)
        => _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == requesterUserId)
            .Select(u => new AnalyticsRequesterInfo
            {
                Id = u.Id,
                InstituteId = u.InstituteId,
                RoleName = u.Role == null ? string.Empty : u.Role.Name,
                UserDataViewLevelId = u.Role == null ? null : u.Role.UserDataViewLevelID,
                ThanaId = u.Institute == null ? null : u.Institute.ThanaId,
                DistrictId = u.Institute == null || u.Institute.Thana == null ? null : (int?)u.Institute.Thana.DistrictId,
                DivisionId = u.Institute == null || u.Institute.Thana == null || u.Institute.Thana.District == null ? null : (int?)u.Institute.Thana.District.DivisionId
            })
            .FirstOrDefault();

    private List<AnalyticsInstituteInfo> GetAnalyticsInstitutes()
        => _unitOfWork.Repository<Institute>()
            .Query()
            .Where(i => i.IsActive)
            .Select(i => new AnalyticsInstituteInfo
            {
                InstituteId = i.Id,
                InstituteName = i.InstituteNameEN,
                ThanaId = i.ThanaId,
                ThanaName = i.Thana == null ? null : i.Thana.ThanaNameEN,
                DistrictId = i.Thana == null ? null : (int?)i.Thana.DistrictId,
                DistrictName = i.Thana == null || i.Thana.District == null ? null : i.Thana.District.DistrictNameEN,
                DivisionId = i.Thana == null || i.Thana.District == null ? null : (int?)i.Thana.District.DivisionId,
                DivisionName = i.Thana == null || i.Thana.District == null || i.Thana.District.Division == null ? null : i.Thana.District.Division.DivisionNameEN
            })
            .ToList();

    private List<AnalyticsUserInfo> GetAnalyticsUsers(IReadOnlyCollection<int> instituteIds, int? dataViewLevelId, int requesterUserId)
        => _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.IsActive && instituteIds.Contains(u.InstituteId))
            .Where(u => dataViewLevelId != OwnDataLevelId || u.Id == requesterUserId)
            .Select(u => new AnalyticsUserInfo
            {
                UserId = u.Id,
                InstituteId = u.InstituteId,
                LoginId = u.LoginID,
                UserName = ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim() == string.Empty
                    ? u.LoginID
                    : ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim(),
                DesignationName = u.Designation == null ? null : u.Designation.DesignationNameEN
            })
            .ToList();

    private static IEnumerable<AnalyticsInstituteInfo> ApplyAnalyticsAccessScope(
        IEnumerable<AnalyticsInstituteInfo> institutes,
        int? dataViewLevelId,
        int requesterInstituteId,
        int? requesterThanaId,
        int? requesterDistrictId,
        int? requesterDivisionId)
        => dataViewLevelId switch
        {
            AllDivisionLevelId => institutes,
            OwnDivisionLevelId => institutes.Where(i => requesterDivisionId.HasValue && i.DivisionId == requesterDivisionId),
            OwnDistrictLevelId => institutes.Where(i => requesterDistrictId.HasValue && i.DistrictId == requesterDistrictId),
            OwnThanaLevelId => institutes.Where(i => requesterThanaId.HasValue && i.ThanaId == requesterThanaId),
            OwnInstituteLevelId => institutes.Where(i => i.InstituteId == requesterInstituteId),
            OwnDataLevelId => institutes.Where(i => i.InstituteId == requesterInstituteId),
            OwnDepartmentsLevelId => institutes.Where(i => i.InstituteId == requesterInstituteId),
            _ => institutes.Where(i => i.InstituteId == requesterInstituteId)
        };

    private static AttendanceAnalyticalFilterOptionsDto BuildAnalyticsFilterOptions(IEnumerable<AnalyticsInstituteInfo> institutes, IEnumerable<AnalyticsUserInfo>? users = null)
    {
        var instituteList = institutes.ToList();
        var userList = users?.ToList() ?? [];
        return new AttendanceAnalyticalFilterOptionsDto
        {
            Divisions = instituteList
                .Where(i => i.DivisionId.HasValue)
                .GroupBy(i => i.DivisionId!.Value)
                .Select(g => new AttendanceAnalyticalFilterOptionDto
                {
                    Id = g.Key,
                    Name = g.Select(i => i.DivisionName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"Division {g.Key}"
                })
                .OrderBy(o => o.Name)
                .ToList(),
            Districts = instituteList
                .Where(i => i.DistrictId.HasValue)
                .GroupBy(i => i.DistrictId!.Value)
                .Select(g => new AttendanceAnalyticalFilterOptionDto
                {
                    Id = g.Key,
                    Name = g.Select(i => i.DistrictName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"District {g.Key}",
                    ParentId = g.Select(i => i.DivisionId).FirstOrDefault(id => id.HasValue)
                })
                .OrderBy(o => o.Name)
                .ToList(),
            Thanas = instituteList
                .Where(i => i.ThanaId.HasValue)
                .GroupBy(i => i.ThanaId!.Value)
                .Select(g => new AttendanceAnalyticalFilterOptionDto
                {
                    Id = g.Key,
                    Name = g.Select(i => i.ThanaName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"Thana {g.Key}",
                    ParentId = g.Select(i => i.DistrictId).FirstOrDefault(id => id.HasValue)
                })
                .OrderBy(o => o.Name)
                .ToList(),
            Institutes = instituteList
                .GroupBy(i => i.InstituteId)
                .Select(g =>
                {
                    var institute = g.First();
                    return new AttendanceAnalyticalFilterOptionDto
                    {
                        Id = institute.InstituteId,
                        Name = institute.InstituteName,
                        ParentId = institute.ThanaId
                    };
                })
                .OrderBy(o => o.Name)
                .ToList(),
            Users = userList
                .GroupBy(u => u.UserId)
                .Select(g =>
                {
                    var user = g.First();
                    return new AttendanceAnalyticalFilterOptionDto
                    {
                        Id = user.UserId,
                        Name = string.IsNullOrWhiteSpace(user.DesignationName)
                            ? $"{user.UserName} ({user.LoginId})"
                            : $"{user.UserName} ({user.LoginId}) - {user.DesignationName}",
                        ParentId = user.InstituteId
                    };
                })
                .OrderBy(o => o.Name)
                .ToList()
        };
    }

    private static string NormalizeAnalyticsPeriod(string? period)
        => period?.Trim().ToLowerInvariant() switch
        {
            "month" or "monthly" => "Month",
            "year" or "yearly" => "Year",
            _ => "Day"
        };

    private static List<AnalyticsPeriodBucket> GetAnalyticsPeriodBuckets(DateTime fromDate, DateTime toDate, string period)
    {
        var buckets = new List<AnalyticsPeriodBucket>();
        if (period == "Month")
        {
            var cursor = new DateTime(fromDate.Year, fromDate.Month, 1);
            while (cursor <= toDate)
            {
                var start = cursor < fromDate ? fromDate : cursor;
                var monthEnd = cursor.AddMonths(1).AddDays(-1);
                var end = monthEnd > toDate ? toDate : monthEnd;
                buckets.Add(new AnalyticsPeriodBucket(start, end, cursor.ToString("MMM yyyy")));
                cursor = cursor.AddMonths(1);
            }

            return buckets;
        }

        if (period == "Year")
        {
            var cursor = new DateTime(fromDate.Year, 1, 1);
            while (cursor <= toDate)
            {
                var start = cursor < fromDate ? fromDate : cursor;
                var yearEnd = new DateTime(cursor.Year, 12, 31);
                var end = yearEnd > toDate ? toDate : yearEnd;
                buckets.Add(new AnalyticsPeriodBucket(start, end, cursor.ToString("yyyy")));
                cursor = cursor.AddYears(1);
            }

            return buckets;
        }

        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            buckets.Add(new AnalyticsPeriodBucket(date, date, date.ToString("dd MMM yyyy")));

        return buckets;
    }

    private static void FillAnalyticsCounts(
        AttendanceAnalyticalSummaryDto target,
        IReadOnlyCollection<AnalyticsUserInfo> users,
        DateTime fromDate,
        DateTime toDate,
        IReadOnlySet<int> weekendDays,
        IReadOnlySet<DateTime> holidayDates,
        IReadOnlyDictionary<(int UserId, DateTime Date), AttendanceDto> attendanceByUserDate,
        IReadOnlyDictionary<(int UserId, DateTime Date), string> leaveByUserDate)
    {
        foreach (var date in EachDate(fromDate, toDate))
        {
            if (weekendDays.Contains((int)date.DayOfWeek) || holidayDates.Contains(date))
                continue;

            target.ExpectedCount += users.Count;
            foreach (var user in users)
            {
                var key = (user.UserId, date);
                if (leaveByUserDate.ContainsKey(key))
                {
                    target.LeaveCount++;
                    continue;
                }

                if (attendanceByUserDate.TryGetValue(key, out var attendance))
                {
                    if (string.Equals(attendance.Status, "OnLeave", StringComparison.OrdinalIgnoreCase))
                    {
                        target.LeaveCount++;
                        continue;
                    }

                    target.PresentCount++;
                    if (string.Equals(attendance.Status, "Late", StringComparison.OrdinalIgnoreCase))
                        target.LateCount++;
                    continue;
                }

                target.AbsentCount++;
            }
        }

        SetAnalyticsRates(target);
    }

    private static void FillPersonalDayDetails(
        AttendancePersonalAnalyticalReportRowDto row,
        int userId,
        DateTime date,
        IReadOnlySet<int> weekendDays,
        IReadOnlySet<DateTime> holidayDates,
        IReadOnlyDictionary<(int UserId, DateTime Date), AttendanceDto> attendanceByUserDate,
        IReadOnlyDictionary<(int UserId, DateTime Date), string> leaveByUserDate)
    {
        var key = (userId, date.Date);
        if (leaveByUserDate.ContainsKey(key))
        {
            row.Status = "Leave";
            return;
        }

        if (holidayDates.Contains(date.Date))
        {
            row.Status = "Holiday";
            return;
        }

        if (weekendDays.Contains((int)date.DayOfWeek))
        {
            row.Status = "Weekend";
            return;
        }

        if (attendanceByUserDate.TryGetValue(key, out var attendance))
        {
            row.Status = string.IsNullOrWhiteSpace(attendance.Status) ? "Present" : attendance.Status;
            row.CheckInTime = attendance.CheckInTime;
            row.CheckOutTime = attendance.CheckOutTime;
            row.CheckInLatitudeLongitude = attendance.CheckInLatitudeLongitude;
            row.CheckOutLatitudeLongitude = attendance.CheckOutLatitudeLongitude;
            return;
        }

        row.Status = "Absent";
    }

    private static IEnumerable<DateTime> EachDate(DateTime fromDate, DateTime toDate)
    {
        for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            yield return date;
    }

    private static void SetAnalyticsRates(AttendanceAnalyticalSummaryDto target)
    {
        target.PresentRate = CalculateRate(target.PresentCount, target.ExpectedCount);
        target.LateRate = CalculateRate(target.LateCount, target.ExpectedCount);
        target.LeaveRate = CalculateRate(target.LeaveCount, target.ExpectedCount);
    }

    private static decimal CalculateRate(int count, int expectedCount)
        => expectedCount <= 0 ? 0 : Math.Round(count * 100m / expectedCount, 2);

    private static int? GetDataViewLevelId(int? userDataViewLevelId, string? roleName)
    {
        if (userDataViewLevelId.HasValue)
            return userDataViewLevelId.Value;

        return roleName?.Trim().Replace(" ", string.Empty).ToUpperInvariant() switch
        {
            "SUPERADMIN" => AllDivisionLevelId,
            "DIVISIONALADMIN" => OwnDivisionLevelId,
            "DISTRICTADMIN" => OwnDistrictLevelId,
            "DISRTICTADMIN" => OwnDistrictLevelId,
            "THANAADMIN" => OwnThanaLevelId,
            "INSTITUTEADMIN" => OwnInstituteLevelId,
            "USER" => OwnDataLevelId,
            "DEPARTMENTALADMIN" => OwnDepartmentsLevelId,
            _ => null
        };
    }

    private Dictionary<DateTime, string> GetApprovedLeaveDays(int userId, DateTime fromDate, DateTime toDate)
        => GetApprovedLeaveDays([userId], fromDate, toDate)
            .Where(l => l.Key.UserId == userId)
            .ToDictionary(l => l.Key.Date, l => l.Value);

    private Dictionary<(int UserId, DateTime Date), string> GetApprovedLeaveDays(IEnumerable<int> userIds, DateTime fromDate, DateTime toDate)
    {
        var userIdList = userIds.Distinct().ToList();
        var leaves = _unitOfWork.Repository<LeaveRequest>()
            .Query()
            .Where(l => userIdList.Contains(l.UserId)
                && l.Status == "Approved"
                && l.StartDate.Date <= toDate
                && l.EndDate.Date >= fromDate)
            .Select(l => new
            {
                l.UserId,
                l.StartDate,
                l.EndDate,
                l.LeaveType
            })
            .ToList();

        var leaveByDate = new Dictionary<(int UserId, DateTime Date), string>();
        foreach (var leave in leaves)
        {
            var start = leave.StartDate.Date < fromDate ? fromDate : leave.StartDate.Date;
            var end = leave.EndDate.Date > toDate ? toDate : leave.EndDate.Date;
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                leaveByDate.TryAdd((leave.UserId, date), leave.LeaveType);
            }
        }

        return leaveByDate;
    }

    private static AttendanceDto CreateGeneratedReportRow(
        int userId,
        string userName,
        int instituteId,
        string instituteName,
        DateTime date,
        string status,
        string? notes)
        => new()
        {
            Id = 0,
            UserId = userId,
            UserName = userName,
            Date = date,
            CheckInTime = null,
            CheckOutTime = null,
            CheckInLatitudeLongitude = null,
            CheckOutLatitudeLongitude = null,
            Status = status,
            Notes = notes,
            InstituteId = instituteId,
            InstituteName = instituteName,
            SubmittedByUserId = 0,
            SubmittedByUserName = string.Empty
        };

    private static readonly System.Linq.Expressions.Expression<Func<Attendance, AttendanceDto>> ProjectToDto = attendance => new AttendanceDto
    {
        Id = attendance.Id,
        UserId = attendance.UserId,
        UserName = attendance.User == null ? string.Empty : attendance.User.LoginID,
        Date = attendance.Date,
        CheckInTime = attendance.CheckInTime,
        CheckOutTime = attendance.CheckOutTime,
        CheckInLatitudeLongitude = attendance.CheckInLatitudeLongitude,
        CheckOutLatitudeLongitude = attendance.CheckOutLatitudeLongitude,
        Status = attendance.Status,
        Notes = attendance.Notes,
        InstituteId = attendance.InstituteId,
        InstituteName = attendance.Institute == null ? string.Empty : attendance.Institute.InstituteNameEN,
        SubmittedByUserId = attendance.SubmittedByUserId,
        SubmittedByUserName = attendance.SubmittedByUser == null ? string.Empty : attendance.SubmittedByUser.LoginID
    };

    private AttendanceDto GetDto(int id)
        => _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.Id == id)
            .Select(ProjectToDto)
            .First();

    private sealed record AnalyticsPeriodBucket(DateTime Start, DateTime End, string Label);

    private sealed class AnalyticsRequesterInfo
    {
        public int Id { get; set; }
        public int InstituteId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int? UserDataViewLevelId { get; set; }
        public int? ThanaId { get; set; }
        public int? DistrictId { get; set; }
        public int? DivisionId { get; set; }
    }

    private sealed class AnalyticsInstituteInfo
    {
        public int InstituteId { get; set; }
        public string InstituteName { get; set; } = string.Empty;
        public int? ThanaId { get; set; }
        public string? ThanaName { get; set; }
        public int? DistrictId { get; set; }
        public string? DistrictName { get; set; }
        public int? DivisionId { get; set; }
        public string? DivisionName { get; set; }
    }

    private sealed class AnalyticsUserInfo
    {
        public int UserId { get; set; }
        public int InstituteId { get; set; }
        public string LoginId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
    }
}
