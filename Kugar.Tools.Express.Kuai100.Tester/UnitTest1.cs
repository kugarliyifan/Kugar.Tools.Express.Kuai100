using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kugar.Tools.Express.Tester
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var t=Kuai100.GetExpressLogsAsync("DA2FF8B2D616E2AD115CBC7A56A28532", "GbCnLBWM16", "‘œ¥ÔøÏ‘À", "4305842583111").Result;

            var s = Kuai100.ExpressNames;
        }
    }
}
