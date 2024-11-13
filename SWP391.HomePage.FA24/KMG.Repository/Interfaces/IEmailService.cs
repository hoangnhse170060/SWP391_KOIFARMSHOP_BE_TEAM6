using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Interfaces
{
    public interface IEmailService 
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}

