using System.Collections.Generic;
using System.Threading.Tasks;
using PlanPeople.Core.Models;

namespace PlanPeople.Core.Interfaces;

public interface INoteRepository
{
    Task<IEnumerable<Note>> GetAllAsync();
    Task<Note?> GetByIdAsync(string id);
    Task AddAsync(Note note);
    Task UpdateAsync(Note note);
    Task DeleteAsync(string id);
}