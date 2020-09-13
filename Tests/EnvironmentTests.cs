using Koek;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public sealed class EnvironmentTests : BaseTestClass
    {
        [TestMethod]
        public void IsMicrosoftOperatingSystem_IsOppositeOfIsNonMicrosoftOperatingSystem()
        {
            Assert.AreNotEqual(Helpers.Environment.IsNonMicrosoftOperatingSystem(), Helpers.Environment.IsMicrosoftOperatingSystem());
        }
    }
}