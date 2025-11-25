using Microsoft.AspNetCore.Mvc;
using UserManagmentApi.Models;
using UserManagmentApi.Services;

namespace UserManagmentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns>List of all users</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            return Ok(user);
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="user">User data</param>
        /// <returns>Created user</returns>
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                return BadRequest("Name is required.");
            }

            if (string.IsNullOrWhiteSpace(user.Department))
            {
                return BadRequest("Department is required.");
            }

            var createdUser = await _userService.CreateUserAsync(user);
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="user">Updated user data</param>
        /// <returns>Updated user</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                return BadRequest("Name is required.");
            }

            if (string.IsNullOrWhiteSpace(user.Department))
            {
                return BadRequest("Department is required.");
            }

            var updatedUser = await _userService.UpdateUserAsync(id, user);
            if (updatedUser == null)
            {
                return NotFound($"User with ID {id} not found.");
            }

            return Ok(updatedUser);
        }

        /// <summary>
        /// Delete user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var isDeleted = await _userService.DeleteUserAsync(id);
            if (!isDeleted)
            {
                return NotFound($"User with ID {id} not found.");
            }

            return Ok($"User with ID {id} has been deleted successfully.");
        }

        /// <summary>
        /// Test endpoint to simulate various error scenarios for testing error handling middleware
        /// </summary>
        /// <param name="errorType">Type of error to simulate: argument, notfound, unauthorized, conflict, server</param>
        /// <returns>Throws appropriate exception</returns>
        [HttpGet("test-error/{errorType}")]
        public ActionResult TestError(string errorType)
        {
            switch (errorType.ToLower())
            {
                case "argument":
                    throw new ArgumentException("This is a test argument exception");
                
                case "notfound":
                    throw new KeyNotFoundException("This is a test not found exception");
                
                case "unauthorized":
                    throw new UnauthorizedAccessException("This is a test unauthorized exception");
                
                case "conflict":
                    throw new InvalidOperationException("This is a test conflict exception");
                
                case "server":
                    throw new Exception("This is a test internal server error");
                
                default:
                    return BadRequest("Valid error types: argument, notfound, unauthorized, conflict, server");
            }
        }
    }
}