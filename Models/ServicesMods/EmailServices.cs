using Microsoft.Extensions.Configuration;
using SkyTech.Models.JobPostServices;
using SkyTech.Services;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using QRCoder;

namespace SkyTech.Models
{
    public class EmailServices: ILeaseBillingNotifier
    {
        private readonly string fromEmail;
        private readonly string fromPassword;

        // IConfiguration replaces ConfigurationManager in Core
        // Register as scoped: builder.Services.AddScoped<Services>();
        public EmailServices(IConfiguration configuration)
        {
            fromEmail = configuration["EmailSettings:FromEmail"];
            fromPassword = configuration["EmailSettings:FromPassword"];
        }

        // ── Shared helper — avoids repeating the SmtpClient block ──
        private void Send(string toEmail, string subject, string body)
        {
            var fromAddress = new MailAddress(fromEmail, "Spaza Connect");
            var toAddress = new MailAddress(toEmail);

            using var smtp = new SmtpClient
            {
                Host = "smtp.office365.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(fromEmail, fromPassword)
            };
            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            };

            try
            {
                smtp.Send(message);
                Console.WriteLine($"DEBUG: Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: SMTP SEND FAILED: {ex.GetType().Name} - {ex.Message}");
                throw; // re-throw so callers' catch blocks still work as before
            }
        }


        // ── Same as Send, but with an inline embedded image — used for the
        // loan settlement QR code. Kept separate from Send/SendWithAttachment
        // so every existing call site is untouched. ──
        private void SendWithInlineImage(string toEmail, string subject, string htmlBody, byte[] imageBytes, string contentId)
        {
            var fromAddress = new MailAddress(fromEmail, "Spaza Connect");
            var toAddress = new MailAddress(toEmail);

            using var smtp = new SmtpClient
            {
                Host = "smtp.office365.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(fromEmail, fromPassword)
            };
            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                IsBodyHtml = true
            };

            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
            var qrImage = new LinkedResource(new MemoryStream(imageBytes), "image/png")
            {
                ContentId = contentId
            };
            htmlView.LinkedResources.Add(qrImage);
            message.AlternateViews.Add(htmlView);

            try
            {
                smtp.Send(message);
                Console.WriteLine($"DEBUG: Email with inline image sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: SMTP SEND FAILED: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }


        // ── Same as Send, but with a file attached — used for the lease
        // payment confirmation PDF. Kept separate from Send rather than
        // adding an optional param there, so every existing call site
        // (there are a lot of them above) is untouched. ──
        private void SendWithAttachment(string toEmail, string subject, string body, byte[] attachmentBytes, string attachmentFileName)
        {
            var fromAddress = new MailAddress(fromEmail, "Spaza Connect");
            var toAddress = new MailAddress(toEmail);

            using var smtp = new SmtpClient
            {
                Host = "smtp.office365.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(fromEmail, fromPassword)
            };
            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            };

            using var stream = new MemoryStream(attachmentBytes);
            message.Attachments.Add(new Attachment(stream, attachmentFileName, "application/pdf"));

            try
            {
                smtp.Send(message);
                Console.WriteLine($"DEBUG: Email with attachment sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: SMTP SEND FAILED: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        // ── Customer ─────────────────────────────────────────
        public void CustomerRegConfirmation(string toEmail, string firstName) =>
            Send(toEmail,
                "Welcome to SkyTech - Registration Successful",
                $"Dear {firstName},\n\n" +
                "Thank you for signing up with SkyTech. " +
                "Your account is now active and you can order your everyday essentials from the comfort of your home.\n" +
                "We're here to make shopping easier, faster and closer to your home.\n\n" +
                "Looking forward to serving you,\nSkyTech Team");

        // ── Driver ───────────────────────────────────────────
        public void DriverRegConfirmation(string toEmail, string firstName) =>
            Send(toEmail,
                "Welcome to SkyTech - Registration Successful",
                $"Dear {firstName},\n\n" +
                "Thank you for signing up withSkyTech to be part of our delivery driving community. " +
                "We will be reviewing your application and you will get feedback within 7 days.\n" +
                "Warm Regards,\nSkyTech Team");

        public void DriveAppliApproved(string toEmail, string firstName) =>
            Send(toEmail,
                "Welcome toSkyTech - Application Successful",
                $"Dear {firstName},\n\n" +
                "Thank you for applying to SkyTech. " +
                "You have been accepted as one of our delivery drivers.\n" +
                "Please log in to our system for more info on your delivery duties.\n" +
                "Warm Regards,\nSkyTech Team");

        public void DriveAppliReject(string toEmail, string firstName) =>
            Send(toEmail,
                "SkyTech - Application Outcome",
                $"Dear {firstName},\n\n" +
                "Thank you for applying to SkyTech. " +
                "Unfortunately your application was not successful.\n" +
                "Kind Regards,\nSkyTech Team");

        // ── Staff ────────────────────────────────────────────
        public void StaffRegConfirmation(string toEmail, string firstName) =>
            Send(toEmail,
                "Registration Succeeded",
                $"Dear {firstName},\n\n" +
                "You have been successfully registered as a staff member. " +
                "Welcome to the team!\n\nWarm Regards");

        // ── OTP / Password reset ─────────────────────────────
        public void SendOtpEmail(string toEmail, string otp) =>
            Send(toEmail,
                "Your OTP Code",
                $"Your OTP code is: {otp}\nThis code will expire in 5 minutes.");

        // ── Shop Owner ───────────────────────────────────────
        public void SOwnerAppConfirmation(string toEmail, string firstName) =>
            Send(toEmail,
                "Welcome to SkyTech - Application Received",
                $"Dear {firstName},\n\n" +
                "Thank you for applying toSkyTech. " +
                "Your account is now active. Please check your application status in our application.\n" +
                "You will also receive an email on whether your application is approved or not.\n\n" +
                "SkyTech Team");

        public void SOwnerApplApproved(string toEmail, string firstName) =>
            Send(toEmail,
                "SkyTech - Application Approved",
                $"Dear {firstName},\n\n" +
                "Your application to sell on SkyTech has been approved.\n" +
                "SkyTech Team");

        public void SOwnerApplRejected(string toEmail, string firstName) =>
            Send(toEmail,
                "SkyTech - Application Outcome",
                $"Dear {firstName},\n\n" +
                "Thank you for applying to SkyTech. " +
                "Unfortunately your application has been rejected.\n" +
                "Please check your documents and ensure they are valid before trying again.\n\n" +
                "SkyTech Team");

        //======================MANAGE JOB APPLICATIONS=====================
        public void RejectJobApplication(string toEmail, string firstName) =>
            Send(toEmail,
                "SkyTech - Application Outcome",
                $"Dear {firstName},\n\n" +
                "Thank you for applying to SkyTech. " +
                "After carefully reviewing your application,unfortunately your application has been rejected.\n" +
                ".\n\n" +
                "SkyTech Team");
        public void OnlineInterview(string toEmail, string firstName) =>
     Send(toEmail,
         "SkyTech - Online Interview",
         $"Dear {firstName},\n\n" +
         "Thank you for applying to SkyTech.\n\n" +
         "We are pleased to inform you that you have been selected for an online interview for the position you applied for.\n\n" +
         "Please log in to your SkyTech account to view the interview details, including the date and time.\n\n" +
         "We look forward to speaking with you.\n\n" +
         "Warm Regards,\n" +
         "SkyTech Team");
        public void PhysicalInterview(string toEmail, string firstName) =>
            Send(toEmail,
                "SkyTech - Application Outcome",
                $"Dear {firstName},\n\n" +
                "Thank you for applying to SkyTech. " +
                "After carefully reviewing your application, your application has been accepted.\n" +
                "Please chech your account for more information on your upcoming interview for this position" +
                ".\n\n" +
                "SkyTech Team");



        //===Job applications
        public void JobApplicationAccepted(string toEmail, string firstName) =>
    Send(toEmail,
        "SkyTech - Application Received",
        $"Dear {firstName},\n\n" +
        "Thank you for applying to SkyTech.\n\n" +
        "Your application has been successfully submitted and has met the initial skill requirements for the position.\n\n" +
        "Your application will now proceed to the next stage of the recruitment process.\n\n" +
        "Please check your SkyTech account regularly for updates regarding your application.\n\n" +
        "Warm Regards,\n" +
        "SkyTech Team");


        public void JobApplicationShortlisted(string toEmail, string firstName) =>
    Send(toEmail,
        "SkyTech - Application Shortlisted",
        $"Dear {firstName},\n\n" +
        "Thank you for applying to SkyTech.\n\n" +
        "We are pleased to inform you that your application has been shortlisted for the next stage of the recruitment process.\n\n" +
        "Please log in to your SkyTech account regularly for further information.\n\n" +
        "Warm Regards,\n" +
        "SkyTech Team");

        public void JobApplicationPassedTest(string toEmail, string firstName) =>
    Send(toEmail,
        "SkyTech - Test Result",
        $"Dear {firstName},\n\n" +
        "Congratulations!\n\n" +
        "You have successfully passed the assessment for the position you applied for.\n\n" +
        "Your application will now proceed to the next stage of the recruitment process.\n\n" +
        "Please check your SkyTech account for further information.\n\n" +
        "Warm Regards,\n" +
        "SkyTech Team");

        public void JobApplicationFailedTest(string toEmail, string firstName) =>
    Send(toEmail,
        "SkyTech - Test Result",
        $"Dear {firstName},\n\n" +
        "Thank you for taking the assessment for the position you applied for.\n\n" +
        "Unfortunately, you did not achieve the required score to proceed to the next stage of the recruitment process.\n\n" +
        "We appreciate the time and effort you put into your application.\n\n" +
        "Kind Regards,\n" +
        "SkyTech Team");


        public void SendContract(string toEmail, string firstName) =>
    Send(toEmail,
        "SkyTech - Test Result",
        $"Dear {firstName},\n\n" +
        "Congratulation for your successful interview.\n\n" +
        "Please check and sign the contract that has been sent to you.\n\n" +
        "We appreciate the time and effort you put into your application.\n\n" +
        "Kind Regards,\n" +
        "SkyTech Team");

        public void JobInterviewScheduled(
    string toEmail,
    string firstName,
    DateTime scheduledDate) =>
    Send(
        toEmail,
        "SkyTech - Interview Scheduled",
        $"Dear {firstName},\n\n" +
        "Congratulations! Your application has progressed to the interview stage.\n\n" +
        $"Your interview has been scheduled for {scheduledDate:dddd, dd MMMM yyyy HH:mm}.\n\n" +
        "Please log in to your SkyTech account for further information.\n\n" +
        "Warm Regards,\n" +
        "SkyTech Team");

        public void JobTestFailed(
    string toEmail,
    string firstName) =>
    Send(
        toEmail,
        "SkyTech - Application Update",
        $"Dear {firstName},\n\n" +
        "Thank you for taking the time to complete our online assessment and Interview.\n\n" +
        "After careful review, we regret to inform you that you have not been " +
        "successful in progressing to the next stage on this occasion.\n\n" +
        "We appreciate your interest in SkyTech and encourage you to apply for " +
        "future opportunities that match your skills.\n\n" +
        "Warm Regards,\n" +
        "SkyTech Team");

        public void StaffAccountCreated(
    string toEmail,
    string firstName,
    string username,
    string password) =>
Send(
    toEmail,
    "SkyTech - Welcome to the Team!",
    $"Dear {firstName},\n\n" +
    "Congratulations! Your contract has been signed and your SkyTech staff " +
    "account has been created.\n\n" +
    "You can log in using the following details:\n\n" +
    $"Username: {username}\n" +
    $"Password: {password}\n\n" +
    "For your security, we recommend changing your password after your first login.\n\n" +
    "Warm Regards,\n" +
    "SkyTech Team");



        // ── Loan Repayment ───────────────────────────────
        public void LoanSettled(string toEmail, string firstName, string collateralSummary, DateTime collectionDate, string collectionToken)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(collectionToken, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(20);

            var htmlBody = $@"
        <div style='font-family: Arial, sans-serif; color:#0e1b35;'>
            <p>Dear {firstName},</p>
            <p>Congratulations! Your loan has been fully settled.</p>
            <p>Your collateral (<strong>{collateralSummary}</strong>) is now ready for collection.</p>
            <p>Please visit the branch on or after <strong>{collectionDate:dddd, dd MMMM yyyy}</strong> to collect your item.</p>
            <p><strong>Show this QR code at the branch:</strong></p>
            <img src='cid:collectionqr' alt='Collection QR Code' style='width:220px;height:220px;' />
            <p style='font-family: monospace; font-size: 13px; background:#f0f4fc; padding:8px 12px; border-radius:6px; display:inline-block;'>{collectionToken}</p>
            <p>Thank you for banking with SkyTech.</p>
            <p>Warm Regards,<br/>SkyTech Team</p>
        </div>";

            SendWithInlineImage(toEmail, "SkyTech - Loan Settled", htmlBody, qrBytes, "collectionqr");
        }


        // ── Lease Management ───────────────────────────────

        // Sent by LeaseBillingService's daily job, ~3 days before a monthly
        // payment (or renewal fee) falls due.
        public void PaymentReminder(string toEmail, string firstName, double amountDue, DateTime dueDate) =>
            Send(toEmail,
                "SkyTech - Lease Payment Due Soon",
                $"Dear {firstName},\n\n" +
                $"Your next lease payment of R{amountDue:N2} is due on {dueDate:dddd, dd MMMM yyyy}.\n\n" +
                "Please log in to your SkyTech account and use the 'Pay Now' button on your lease agreement to settle this before the due date.\n\n" +
                "Warm Regards,\n" +
                "SkyTech Team");

        // Sent by LeaseBillingService's daily job once a payment is 5+ days
        // overdue and the late fee has just been added to it.
        public void LateFeeWarning(string toEmail, string firstName, double lateFeeAmount, DateTime dueDate) =>
            Send(toEmail,
                "SkyTech - Late Payment Notice",
                $"Dear {firstName},\n\n" +
                $"Your lease payment due on {dueDate:dddd, dd MMMM yyyy} is now more than 5 days overdue.\n\n" +
                $"A late fee of R{lateFeeAmount:N2} has been added and will be included the next time you pay. Please settle this as soon as possible via your SkyTech account.\n\n" +
                "Warm Regards,\n" +
                "SkyTech Team");

        // Sent after a monthly installment or renewal fee payment clears,
        // with the full lease statement PDF attached.
        public void LeasePaymentConfirmation(string toEmail, string firstName, double amountPaid, byte[] statementPdf, string pdfFileName) =>
            SendWithAttachment(toEmail,
                "SkyTech - Lease Payment Received",
                $"Dear {firstName},\n\n" +
                $"We've received your payment of R{amountPaid:N2}. Thank you!\n\n" +
                "Your full lease statement — fees already paid, any amount still due, and any late fees applied — is attached to this email as a PDF.\n\n" +
                "Warm Regards,\n" +
                "SkyTech Team",
                statementPdf,
                pdfFileName);



        public void ReturnScheduled(string toEmail, string firstName, DateTime collectionDate, double earlyReturnFee) =>
       Send(toEmail,
           "SkyTech - Lease Return Scheduled",
           $"Dear {firstName},\n\n" +
           $"Your equipment collection has been scheduled for {collectionDate:dddd, dd MMMM yyyy}.\n\n" +
           "Please have the item ready and accessible on that date.\n\n" +
           (earlyReturnFee > 0
               ? $"Since this return is before your lease's end date, an early-return fee of R{earlyReturnFee:N2} will be deducted from your deposit refund.\n\n"
               : "") +
           "Warm Regards,\n" +
           "SkyTech Team");

        public void EquipmentReceived(string toEmail, string firstName) =>
            Send(toEmail,
                "SkyTech - Equipment Received",
                $"Dear {firstName},\n\n" +
                "We've received your returned equipment and it's now being inspected.\n\n" +
                "We'll let you know the outcome shortly.\n\n" +
                "Warm Regards,\n" +
                "SkyTech Team");

        public void InspectionComplete(string toEmail, string firstName, double damageFee, double depositRefund) =>
            Send(toEmail,
                "SkyTech - Inspection Complete",
                $"Dear {firstName},\n\n" +
                (damageFee > 0
                    ? $"Your inspection is complete. A damage fee of R{damageFee:N2} applies, leaving R{depositRefund:N2} of your deposit refundable.\n\n"
                    : "Your inspection is complete — no damage was found, so your full deposit is refundable.\n\n") +
                "Your refund will be processed shortly.\n\n" +
                "Warm Regards,\n" +
                "SkyTech Team");

        public void DepositRefunded(string toEmail, string firstName, double amount, string transferStatus) =>
            Send(toEmail,
                "SkyTech - Deposit Refunded",
                $"Dear {firstName},\n\n" +
                (amount > 0
                    ? $"Your deposit refund of R{amount:N2} has been processed (status: {transferStatus}).\n\n"
                    : "Your damage fee fully covered the deposit amount, so no refund was due.\n\n") +
                "Thank you for leasing with SkyTech.\n\n" +
                "Warm Regards,\n" +
                "SkyTech Team");

        public void ReturnOrRenewReminder(string toEmail, string firstName, DateTime endDate) =>
            Send(toEmail,
                "SkyTech - Your Lease is Ending Soon",
                $"Dear {firstName},\n\n" +
                $"Your lease ends on {endDate:dddd, dd MMMM yyyy}. You can renew for another term, or schedule a return of the equipment, from your SkyTech account.\n\n" +
                "Warm Regards,\n" +
                "SkyTech Team");


        //=======Loan application decision====

        public void LoanApplicationApproved(string toEmail, string firstName, decimal amount) =>
        Send(toEmail,
        "SkyTech - Loan Application Approved",
        $"Dear {firstName},\n\n" +
        "Good news! Your loan application with SkyTech has been approved for " +
        $"{amount:C}.\n" +
        "Please visit your branch with your collateral to complete the disbursement process.\n\n" +
        "Warm Regards,\nSkyTech Team");

        public void LoanApplicationRejected(string toEmail, string firstName, string reason) =>
            Send(toEmail,
                "SkyTech - Loan Application Outcome",
                $"Dear {firstName},\n\n" +
                "Thank you for applying for a loan with SkyTech. " +
                "Unfortunately, your application was not successful.\n\n" +
                $"Reason: {reason}\n\n" +
                "You can view full details of this decision by logging in to your account.\n\n" +
                "Kind Regards,\nSkyTech Team");


         public void LoanDisbursed(
         string toEmail,
         string firstName,
         int loanApplicationId,
         decimal amount,
         string accountHolderName,
         string bankName,
         string accountNumber,
         string transferReference,
         string transferStatus,
         DateTime disbursedOn) =>
          Send(toEmail,
         "SkyTech - Loan Payment Receipt",
         $"Dear {firstName},\n\n" +
         "Your loan has been disbursed. Here is your payment receipt:\n\n" +
         "------------------------------------\n" +
         $"Application Reference:  #{loanApplicationId}\n" +
         $"Amount Disbursed:       {amount:C}\n" +
         $"Paid To:                {accountHolderName}\n" +
         $"Bank:                   {bankName}\n" +
         $"Account Number:         {MaskAccountNumber(accountNumber)}\n" +
         $"Transfer Reference:     {transferReference}\n" +
         $"Transfer Status:        {transferStatus}\n" +
         $"Date:                   {disbursedOn:dd MMM yyyy HH:mm}\n" +
         "------------------------------------\n\n" +
         "Please keep this email for your records. If you have any questions about this payment, " +
         "contact your branch directly.\n\n" +
         "Warm Regards,\nSkyTech Team");
        
         /// <summary>Shows only the last 4 digits in the receipt email — full account
         /// numbers shouldn't be echoed back in plain-text email bodies.</summary>
         private static string MaskAccountNumber(string accountNumber)
         {
             if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length <= 4)
                 return accountNumber ?? "";
        
             return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
         }


    }


}
