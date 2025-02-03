namespace DeltaLake.Tests
{
    public static class Settings
    {
        public static string TestRoot = System.Environment.GetEnvironmentVariable("TEST_ROOT") ?? Path.Combine("..", "..", "..", "..", "data");
    }
}