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
            var check = pm.TryLeaveParkingByCarPlateNumber("125", out session);
            Console.WriteLine(check + "" + session.TicketNumber + "" + session.CarPlateNumber + "" + session.EntryDt);
        }

    }
}