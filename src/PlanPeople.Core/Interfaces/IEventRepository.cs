using System.Collections.Generic;
using System.Threading.Tasks;
using PlanPeople.Core.Models;

namespace PlanPeople.Core.Interfaces;

public interface IEventRepository
{
    Task<IEnumerable<CalendarEvent>> GetAllAsync();
    Task<CalendarEvent?> GetByIdAsync(string id);
    Task AddAsync(CalendarEvent calendarEvent);
    Task UpdateAsync(CalendarEvent calendarEvent);
    Task DeleteAsync(string id); // 软删除
}