using System;

namespace ParkingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ParkingSession session;
            var pm = new ParkingManager();
            pm.GetData();
            pm.EnterParking("123");
            pm.EnterParking("124");
            pm.EnterParking("125");
            var check = pm.TryLeaveParkingByCarPlateNumber("123", out session);
            Console.WriteLine(check);
        }

    }
}