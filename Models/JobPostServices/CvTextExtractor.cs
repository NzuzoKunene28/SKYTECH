// Services/CvTextExtractor.cs
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using System.Text;

namespace SkyTech.JobPostServices
{
    public static class CvTextExtractor
    {
        public static string ExtractText(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension == ".pdf")
                return ExtractFromPdf(stream);

            if (extension == ".docx")
                return ExtractFromDocx(stream);

            throw new NotSupportedException("Only PDF and DOCX files are supported.");
        }

        private static string ExtractFromPdf(Stream stream)
        {
            using var pdf = PdfDocument.Open(stream);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
                sb.AppendLine(page.Text);
            return sb.ToString();
        }

        private static string ExtractFromDocx(Stream stream)
        {
            using var doc = WordprocessingDocument.Open(stream, false);
            return doc.MainDocumentPart?.Document?.Body?.InnerText ?? "";
        }
    }
}