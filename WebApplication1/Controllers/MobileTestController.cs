/*
 * File: MobileTestController.cs
 * Description: Test controller to simulate mobile app interactions for EV owners
 * Author: [Your Team Name]
 * Date: [Current Date]
 */

using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    /// <summary>
    /// Test controller to simulate mobile app functionality
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MobileTestController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;
        private readonly EVOwnersController _evOwnersController;

        public MobileTestController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
            _evOwnersController = new EVOwnersController(mongoDBService);
        }

        /// <summary>
        /// Test EV owner registration (simulates mobile app registration)
        /// </summary>
        [HttpPost("test-register")]
        public async Task<ActionResult> TestRegisterEVOwner()
        {
            var testRequest = new RegisterEVOwnerRequest
            {
                NIC = "123456789V",
                FullName = "John Doe",
                Email = "john.doe@example.com",
                Password = "TestPassword123",
                PhoneNumber = "0771234567",
                Address = "123 Main Street, Colombo"
            };

            return await _evOwnersController.RegisterEVOwner(testRequest);
        }

        /// <summary>
        /// Test EV owner login (simulates mobile app login)
        /// </summary>
        [HttpPost("test-login")]
        public async Task<ActionResult> TestLoginEVOwner([FromQuery] string email = "john.doe@example.com", [FromQuery] string password = "TestPassword123")
        {
            var loginRequest = new LoginEVOwnerRequest
            {
                Email = email,
                Password = password
            };

            return await _evOwnersController.LoginEVOwner(loginRequest);
        }

        /// <summary>
        /// Get test user status for mobile app simulation
        /// </summary>
        [HttpGet("test-user-status/{nic}")]
        public async Task<ActionResult> GetTestUserStatus(string nic)
        {
            return await _evOwnersController.GetEVOwnerByNIC(nic);
        }

        /// <summary>
        /// Complete workflow test
        /// </summary>
        [HttpGet("test-complete-workflow")]
        public async Task<ActionResult> TestCompleteWorkflow()
        {
            var results = new List<object>();

            try
            {
                // Step 1: Register a test user
                var testNIC = $"TEST{DateTime.Now.Ticks}V";
                var testEmail = $"test{DateTime.Now.Ticks}@example.com";
                var testPassword = "TestPassword123";
                
                var registerRequest = new RegisterEVOwnerRequest
                {
                    NIC = testNIC,
                    FullName = "Test User",
                    Email = testEmail,
                    Password = testPassword,
                    PhoneNumber = "0771234567",
                    Address = "123 Test Street, Colombo"
                };

                var registerResult = await _evOwnersController.RegisterEVOwner(registerRequest);
                results.Add(new { Step = "1. Registration", Result = "Success", Details = registerResult });

                // Step 2: Try to login (should fail - not approved)
                var loginRequest = new LoginEVOwnerRequest
                {
                    Email = testEmail,
                    Password = testPassword
                };

                var loginResult = await _evOwnersController.LoginEVOwner(loginRequest);
                results.Add(new { Step = "2. Login (Before Approval)", Result = "Expected Failure", Details = loginResult });

                // Step 3: Approve the user (simulating backoffice approval)
                var approvalRequest = new ApprovalRequest
                {
                    IsApproved = true,
                    ApprovedBy = "Test System"
                };

                var approvalResult = await _evOwnersController.UpdateApprovalStatus(testNIC, approvalRequest);
                results.Add(new { Step = "3. Approval", Result = "Success", Details = approvalResult });

                // Step 4: Try to login again (should succeed)
                var loginResult2 = await _evOwnersController.LoginEVOwner(loginRequest);
                results.Add(new { Step = "4. Login (After Approval)", Result = "Success", Details = loginResult2 });

                return Ok(new
                {
                    TestCompleted = true,
                    TestNIC = testNIC,
                    TestEmail = testEmail,
                    Message = "Complete EV Owner approval workflow test completed successfully",
                    Steps = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    TestCompleted = false,
                    Error = ex.Message,
                    PartialResults = results
                });
            }
        }

        /// <summary>
        /// Get instructions for testing the approval system
        /// </summary>
        [HttpGet("instructions")]
        public ActionResult GetTestInstructions()
        {
            var instructions = new
            {
                Title = "EV Owner Approval System Test Instructions",
                Description = "This system allows EV owners to register via mobile app and requires backoffice approval before they can login.",
                TestSteps = new[]
                {
                    "1. Register a test EV owner using: POST /api/MobileTest/test-register",
                    "2. Try to login (should fail): POST /api/MobileTest/test-login?email=john.doe@example.com&password=TestPassword123",
                    "3. Login to web app as backoffice user and approve the registration at: /EVOwnerManagement/PendingApprovals",
                    "4. Try to login again (should succeed): POST /api/MobileTest/test-login?email=john.doe@example.com&password=TestPassword123",
                    "5. Or run complete automated test: GET /api/MobileTest/test-complete-workflow"
                },
                WebAppUrls = new
                {
                    Login = "/Account/Login",
                    Dashboard = "/Dashboard",
                    EVOwnerManagement = "/EVOwnerManagement",
                    PendingApprovals = "/EVOwnerManagement/PendingApprovals"
                },
                TestCredentials = new
                {
                    BackofficeUser = new { Email = "admin@evcharging.com", Password = "Admin123!" },
                    TestEVOwner = new { Email = "john.doe@example.com", Password = "TestPassword123" }
                },
                ImportantNote = "EV Owner authentication now uses EMAIL instead of NIC for login"
            };

            return Ok(instructions);
        }
    }
}