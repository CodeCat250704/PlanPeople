using System.Collections.Generic;
using System.Threading.Tasks;
using PlanPeople.Core.Models;
using Thread = PlanPeople.Core.Models.Thread; // 解决 Thread 命名冲突

namespace PlanPeople.Core.Interfaces;

public interface IChatRepository
{
    Task<IEnumerable<Thread>> GetThreadsAsync();
    Task AddThreadAsync(Thread thread);
    Task<IEnumerable<Message>> GetMessagesAsync(string threadId);
    Task AddMessageAsync(Message msg);
}