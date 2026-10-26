namespace sh.be.domain.Module.Construction.QaAssignments.Services
{
    public static class QaSampleSizePolicy
    {
        public static (int SampleSize, int MaxFailed) Calculate(int documentCount, int sampleSize, decimal thresholdPercent)
        {
            var actualSampleSize = Math.Min(sampleSize, documentCount);
            return (actualSampleSize, (int)Math.Floor(actualSampleSize * thresholdPercent / 100));
        }
    }
}
