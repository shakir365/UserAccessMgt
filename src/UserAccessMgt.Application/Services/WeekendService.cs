using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Weekend;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class WeekendService : IWeekendService
{
    private readonly IUnitOfWork _unitOfWork;

    public WeekendService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<WeekendDto>> CreateAsync(CreateWeekendRequest request)
    {
        var existing = await _unitOfWork.Repository<Weekend>()
            .FirstOrDefaultAsync(w => w.DayOfWeek == request.DayOfWeek);
        if (existing is not null)
            return ApiResponse<WeekendDto>.Fail("Weekend already exists for this day", "WEEKEND_EXISTS");

        var weekend = new Weekend
        {
            DayOfWeek = request.DayOfWeek,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Weekend>().AddAsync(weekend);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<WeekendDto>.Ok(GetDto(weekend.Id), "Weekend created successfully");
    }

    public Task<ApiResponse<WeekendDto>> GetByIdAsync(int id)
    {
        var dto = _unitOfWork.Repository<Weekend>().Query()
            .Where(w => w.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefault();
        return Task.FromResult(dto is null
            ? ApiResponse<WeekendDto>.Fail("Weekend not found", "NOT_FOUND")
            : ApiResponse<WeekendDto>.Ok(dto));
    }

    public Task<ApiResponse<IEnumerable<WeekendDto>>> GetAllAsync()
        => Task.FromResult(ApiResponse<IEnumerable<WeekendDto>>.Ok(
            _unitOfWork.Repository<Weekend>().Query().Select(ProjectToDto).OrderBy(w => w.DayOfWeek).ToList()));

    public async Task<ApiResponse<WeekendDto>> UpdateAsync(int id, UpdateWeekendRequest request)
    {
        var weekend = await _unitOfWork.Repository<Weekend>().GetByIdAsync(id);
        if (weekend is null)
            return ApiResponse<WeekendDto>.Fail("Weekend not found", "NOT_FOUND");

        if (request.DayOfWeek.HasValue)
            weekend.DayOfWeek = request.DayOfWeek.Value;
        if (request.IsActive.HasValue)
            weekend.IsActive = request.IsActive.Value;
        weekend.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Weekend>().Update(weekend);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<WeekendDto>.Ok(GetDto(weekend.Id), "Weekend updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id)
    {
        var weekend = await _unitOfWork.Repository<Weekend>().GetByIdAsync(id);
        if (weekend is null)
            return ApiResponse<string>.Fail("Weekend not found", "NOT_FOUND");

        _unitOfWork.Repository<Weekend>().Remove(weekend);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Weekend deleted successfully");
    }

    private static readonly System.Linq.Expressions.Expression<Func<Weekend, WeekendDto>> ProjectToDto = weekend => new WeekendDto
    {
        Id = weekend.Id,
        DayOfWeek = weekend.DayOfWeek,
        DayName = weekend.DayOfWeek == 0 ? "Sunday"
            : weekend.DayOfWeek == 1 ? "Monday"
            : weekend.DayOfWeek == 2 ? "Tuesday"
            : weekend.DayOfWeek == 3 ? "Wednesday"
            : weekend.DayOfWeek == 4 ? "Thursday"
            : weekend.DayOfWeek == 5 ? "Friday"
            : "Saturday",
        IsActive = weekend.IsActive,
        CreatedAt = weekend.CreatedAt,
        UpdatedAt = weekend.UpdatedAt
    };

    private WeekendDto GetDto(int id)
        => _unitOfWork.Repository<Weekend>().Query().Where(w => w.Id == id).Select(ProjectToDto).First();
}
