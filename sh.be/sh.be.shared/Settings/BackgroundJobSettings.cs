namespace sh.be.shared.Settings
{
    public class BackgroundJobSettings
    {
        public bool Enabled { get; set; } = true;
        public int WorkerCount { get; set; } = 2;
        public BackgroundJobScheduleSettings Jobs { get; set; } = new();
    }

    public class BackgroundJobScheduleSettings
    {
        public string ProjectDeadlineCheck { get; set; } = "0 7 * * *";
    }
}
