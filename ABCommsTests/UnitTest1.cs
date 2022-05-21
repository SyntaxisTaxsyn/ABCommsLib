using Microsoft.VisualStudio.TestTools.UnitTesting;
using ABComms;

namespace ABCommsTests;

[TestClass]
public class PLC_Tests
{
    [TestMethod]
    public void PingTest_PLCExists()
    {
        PLC _plc = new PLC("TestPLC","127.0.0.1",0,5);
        bool retval = false;
        retval = _plc.PingPLCDevice();
        Assert.IsTrue(retval);
    }

    [TestMethod]
    public void PingTest_PLCMissing()
    {
        PLC _plc = new PLC("TestPLC","192.168.0.50",0,5);
        bool retval = false;
        retval = _plc.PingPLCDevice();
        Assert.IsFalse(retval);
    }
}