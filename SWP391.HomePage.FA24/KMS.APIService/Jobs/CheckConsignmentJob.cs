using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;
using KMG.Repository.Models;
using KMG.Repository.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KMS.APIService.Jobs
{
    [DisallowConcurrentExecution] // Đảm bảo job không chạy song song
    public class CheckConsignmentJob : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CheckConsignmentJob> _logger;

        public CheckConsignmentJob(IServiceProvider serviceProvider, ILogger<CheckConsignmentJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Executing CheckConsignmentJob at {Time}", DateTime.Now);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SwpkoiFarmShopContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            // Lấy các consignments có trạng thái 'approved' và đã hết hạn
            var expiredConsignments = await dbContext.Consignments
                .Include(c => c.User)
                .Where(c => c.ConsignmentDateTo <= DateTime.Now && c.Status == "approved")
                .ToListAsync();

            foreach (var consignment in expiredConsignments)
            {
                // Chuyển trạng thái consignment thành 'completed'
                consignment.Status = "completed";

                // Gửi email thông báo cho user
                if (consignment.User != null)
                {
                    var emailSubject = "Consignment Completed Notification";
                    var emailBody = $@"
                        Dear {consignment.User.UserName},

                        Your consignment titled '{consignment.ConsignmentTitle}' has expired on {consignment.ConsignmentDateTo:yyyy-MM-dd}.
                        The consignment status has been updated to 'completed'.

                        Thank you for using our service.";

                    await emailService.SendEmailAsync(consignment.User.Email, emailSubject, emailBody);
                    _logger.LogInformation($"Email sent to user: {consignment.User.Email}");
                }
            }

            // Lưu thay đổi vào database
            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"{expiredConsignments.Count} consignments updated to 'completed'.");
        }
    }
}
