using System.Threading.Tasks;

public class EmailService
{
    // This simulates sending an email
    public Task SendEmailAsync(string toEmail, string subject, string body)
    {
        // Log email to console (for testing purposes)
        Console.WriteLine("----- EMAIL SENT -----");
        Console.WriteLine($"To: {toEmail}");
        Console.WriteLine($"Subject: {subject}");
        Console.WriteLine($"Body: {body}");
        Console.WriteLine("----------------------");

        return Task.CompletedTask;
    }
}
