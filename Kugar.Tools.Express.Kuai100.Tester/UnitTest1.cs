using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kugar.Tools.Express.Tester
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var t=Kuai100.SubscribeExpressCodeAsync("", "�ϴ����", "4305842583112", "http://zzl.stntian.com/api/Noitify/DeliverNotify").Result;

            var s = Kuai100.ExpressNames;
        }
    }
}
