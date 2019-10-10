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
            pm.PayForParking(3, pm.GetRemainingCost(3));
            var check = pm.TryLeaveParkingByCarPlateNumber("125", out session);
            Console.WriteLine(check + " " + session.TicketNumber + " " + session.CarPlateNumber + " " + session.EntryDt);
            var checkcost = pm.GetRemainingCost(2);
            pm.PayForParking(2, checkcost);
            var checkleaving = pm.TryLeaveParkingWithTicket(2, out session);
            Console.WriteLine(checkleaving + " " + session.TicketNumber + " " + session.CarPlateNumber + " " + session.EntryDt + " " + session.TotalPayment + " " + session.ExitDt);
            var checkuser = pm.GetRemainingCost(1);
            pm.PayForParking(1, checkcost);
            var checkleavinguser = pm.TryLeaveParkingWithTicket(1, out session);
            Console.WriteLine(checkleavinguser + " " + session.TicketNumber + " " + session.CarPlateNumber + " " + session.EntryDt + " " + session.ParkingUser + " " + session.TotalPayment + " " + session.ExitDt);
        }

    }
}