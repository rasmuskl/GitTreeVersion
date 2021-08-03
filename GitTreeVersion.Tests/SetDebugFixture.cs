using NUnit.Framework;

namespace GitTreeVersion.Tests
{
    [SetUpFixture]
    public class SetDebugFixture
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Log.IsDebug = true;
        }
    }
}