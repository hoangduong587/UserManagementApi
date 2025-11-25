using UserManagmentApi.Models;

namespace UserManagmentApi.Services
{
    public class UserService : IUserService
    {
        private static readonly List<User> _users = new()
        {
            new User { Id = 1, Name = "John Doe", Salary = 50000, Department = "IT" },
            new User { Id = 2, Name = "Jane Smith", Salary = 60000, Department = "HR" },
            new User { Id = 3, Name = "Mike Johnson", Salary = 55000, Department = "Finance" }
        };
        
        private static int _nextId = 4;

        public Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return Task.FromResult(_users.AsEnumerable());
        }

        public Task<User?> GetUserByIdAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult(user);
        }

        public Task<User> CreateUserAsync(User user)
        {
            user.Id = _nextId++;
            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task<User?> UpdateUserAsync(int id, User user)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == id);
            if (existingUser == null)
            {
                return Task.FromResult<User?>(null);
            }

            existingUser.Name = user.Name;
            existingUser.Salary = user.Salary;
            existingUser.Department = user.Department;

            return Task.FromResult<User?>(existingUser);
        }

        public Task<bool> DeleteUserAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return Task.FromResult(false);
            }

            _users.Remove(user);
            return Task.FromResult(true);
        }
    }
}