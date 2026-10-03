namespace SkyTech.ViewModels
{
    public class TestResultViewModel
    {
        public int Score { get; set; }

        public int TotalQuestions { get; set; }

        public bool Passed { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
