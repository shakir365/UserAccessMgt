using UserAccessMgt.Application.DTOs.Attendance;
using UserAccessMgt.Application.DTOs.Common;

namespace UserAccessMgt.Application.Interfaces;

public interface IAttendanceService
{
    Task<ApiResponse<AttendanceDto>> CreateAsync(CreateAttendanceRequest request, int submittedByUserId);
    Task<ApiResponse<AttendanceDto>> GetByIdAsync(int id);
    Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByUserAsync(int userId);
    Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByDateAsync(DateTime date);
    Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByInstituteAsync(int instituteId);
    Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByInstituteAndDateRangeAsync(int instituteId, DateTime from, DateTime to);
    Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByUserAndDateRangeAsync(int userId, DateTime from, DateTime to);
    Task<ApiResponse<AttendanceSubmissionStatusDto>> GetSubmissionStatusAsync(DateTime? date);
    Task<ApiResponse<AttendanceAnalyticalReportDto>> GetAnalyticalReportAsync(AttendanceAnalyticalReportRequest request, int requesterUserId);
    Task<ApiResponse<AttendancePersonalAnalyticalReportDto>> GetPersonalAnalyticalReportAsync(AttendanceAnalyticalReportRequest request, int requesterUserId);
    Task<ApiResponse<AttendanceDto>> UpdateAsync(int id, UpdateAttendanceRequest request);
    Task<ApiResponse<string>> DeleteAsync(int id);
}
