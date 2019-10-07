using SmartParkingApp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ParkingApp
{
    class ParkingManager
    {
        private List<ParkingSession> activeSessions;
        private List<ParkingSession> endedSessions;
        private List<Tariff> tariff = new List<Tariff>();
        private int capacity;
        private int freeleaveperiod;
        private List<User> users;

        public ParkingSession EnterParking(string carPlateNumber)
        {
            if (activeSessions.Count < capacity)
            {
                var check = activeSessions.Exists(e => e.CarPlateNumber == carPlateNumber);
                if (check == true)
                    return null;
                else
                {
                    var newsession = new ParkingSession();
                    newsession.EntryDt = DateTime.Now;
                    newsession.CarPlateNumber = carPlateNumber;
                    newsession.TicketNumber = activeSessions[activeSessions.Count].TicketNumber + 1;
                    var checkuser = users.Exists(e => e.CarPlateNumber == carPlateNumber);
                    if (checkuser == true)
                        newsession.ParkingUser = users.Find(e => e.CarPlateNumber == carPlateNumber);
                    activeSessions.Add(newsession);
                    return newsession;
                }
            }
            else
                return null;
        }

        public bool TryLeaveParkingWithTicket(int ticketNumber, out ParkingSession session)
        {
            session = activeSessions.Find(e => e.TicketNumber == ticketNumber);
            var sessionSpan = DateTime.Now - session.EntryDt;
            var sessionMinutes = (sessionSpan.Days * 24) * 60 + (sessionSpan.Hours * 60) + (sessionSpan.Minutes);
            if (sessionMinutes > freeleaveperiod)
            {
                session.ExitDt = DateTime.Now;
                var tmpsession = session;
                activeSessions.Remove(activeSessions.Find(e => e.TicketNumber == ticketNumber));
                endedSessions.Add(tmpsession);
                return true;
            }
            else
            {
                session = null;
                return false;
            }
        }

        public decimal GetRemainingCost(int ticketNumber)
        {
            decimal remainingCost;
            int parkingTime;
            var currentTime = DateTime.Now;
            var session = activeSessions.Find(e => e.TicketNumber == ticketNumber);
            if ((session.PaymentDt < currentTime) & (session.PaymentDt != null))
            {
                var tmpParkingTime = DateTime.Now - currentTime;
                parkingTime = (tmpParkingTime.Days * 24) * 60 + (tmpParkingTime.Hours * 60) + (tmpParkingTime.Minutes);
                if (parkingTime >= freeleaveperiod)
                {
                    remainingCost = tariff.First(e => e.Minutes <= parkingTime).Rate;
                    return remainingCost;
                }
                else
                    return 0;
            }
            else
            {
                var tmpParkingTime = DateTime.Now - currentTime;
                parkingTime = (tmpParkingTime.Days * 24) * 60 + (tmpParkingTime.Hours * 60) + (tmpParkingTime.Minutes);
                if (parkingTime >= freeleaveperiod)
                {
                    remainingCost = tariff.First(e => e.Minutes >= parkingTime).Rate;
                    return remainingCost;
                }
                else
                    return 0;
            }
        }


        public void PayForParking(int ticketNumber, decimal amount)
        {
            var session = activeSessions.Find(e => e.TicketNumber == ticketNumber);
            session.PaymentDt = DateTime.Now;
            if (session.TotalPayment != null)
            {
                session.TotalPayment += GetRemainingCost(session.TicketNumber);
            }
            else
                session.TotalPayment = GetRemainingCost(session.TicketNumber);
        }

        public void GetData()
        {
            var path = Directory.GetCurrentDirectory();
            Console.WriteLine(path);
            using (FileStream dataActiveSession = new FileStream(path + "/dataActiveSession.txt", FileMode.OpenOrCreate))
            {
                byte[] array = new byte[dataActiveSession.Length];
                dataActiveSession.Read(array, 0, array.Length);
                string fullText = System.Text.Encoding.Default.GetString(array);
                if (fullText != "")
                {
                    var tmpData = fullText.Split(new char[] { ';' });
                    foreach (var s in tmpData)
                    {
                        var data = s.Split(new char[] { ',' });
                        var newActiveSession = new ParkingSession();
                        newActiveSession.EntryDt = DateTime.Parse(data[0]);
                        newActiveSession.PaymentDt = DateTime.Parse(data[1]);
                        newActiveSession.ExitDt = DateTime.Parse(data[2]);
                        newActiveSession.TotalPayment = Convert.ToDecimal(data[3]);
                        newActiveSession.CarPlateNumber = data[4];
                        newActiveSession.TicketNumber = Convert.ToInt32(data[5]);
                        activeSessions.Add(newActiveSession);
                    }
                }
            }
            using (FileStream dataEndedSession = new FileStream(path + "/dataEndedSession.txt", FileMode.OpenOrCreate))
            {
                byte[] array = new byte[dataEndedSession.Length];
                dataEndedSession.Read(array, 0, array.Length);
                string fullText = System.Text.Encoding.Default.GetString(array);
                if (fullText != "")
                {
                    var tmpData = fullText.Split(new char[] { ';' });
                    foreach (var s in tmpData)
                    {
                        var data = s.Split(new char[] { ',' });
                        var newEndedSession = new ParkingSession();
                        newEndedSession.EntryDt = DateTime.Parse(data[0]);
                        newEndedSession.PaymentDt = DateTime.Parse(data[1]);
                        newEndedSession.ExitDt = DateTime.Parse(data[2]);
                        newEndedSession.TotalPayment = Convert.ToDecimal(data[3]);
                        newEndedSession.CarPlateNumber = data[4];
                        newEndedSession.TicketNumber = Convert.ToInt32(data[5]);
                        endedSessions.Add(newEndedSession);
                    }
                }
            }
            using (FileStream dataTariffs = new FileStream(path + "/dataTariffs.txt", FileMode.Open))
            {
                byte[] array = new byte[dataTariffs.Length];
                dataTariffs.Read(array, 0, array.Length);
                string fullText = System.Text.Encoding.Default.GetString(array);
                var tmpData = fullText.Split(new char[] { ';' });
                foreach (var s in tmpData)
                {
                    if (s != "")
                    {
                        var data = s.Split(new char[] { ',' });
                        var newTariff = new Tariff();
                        newTariff.Minutes = Convert.ToInt32(data[0]);
                        newTariff.Rate = Convert.ToDecimal(data[1]);
                        tariff.Add(newTariff);
                    }
                }
            }
            using (FileStream dataCapacity = new FileStream(path + "/dataCapacity.txt", FileMode.Open))
            {
                byte[] array = new byte[dataCapacity.Length];
                dataCapacity.Read(array, 0, array.Length);
                string fullText = System.Text.Encoding.Default.GetString(array);
                capacity = Convert.ToInt32(fullText);
            }
        }

        /* ADDITIONAL TASK 2 */
        public bool TryLeaveParkingByCarPlateNumber(string carPlateNumber, out ParkingSession session)
        {
            /* There are 3 scenarios for this method:
            
            1. The user has not made any payments but leaves the parking within the free leave period
            from EntryDt:
               1.1 Complete the parking session by setting the ExitDt property
               1.2 Move the session from the list of active sessions to the list of past sessions             * 
               1.3 return true and the completed parking session object in the out parameter
            
            2. The user has already paid for the parking session (session.PaymentDt != null):
            Check that the current time is within the free leave period from session.PaymentDt
               2.1. If yes, complete the session in the same way as in the previous scenario
               2.2. If no, return false, session = null

            3. The user has not paid for the parking session:            
            3a) If the session has a connected user (see advanced task from the EnterParking method):
            ExitDt = PaymentDt = current date time; 
            TotalPayment according to the tariff table:            
            
            IMPORTANT: before calculating the parking charge, subtract FreeLeavePeriod 
            from the total number of minutes passed since entry
            i.e. if the registered visitor enters the parking at 10:05
            and attempts to leave at 10:25, no charge should be made, otherwise it would be unfair
            to loyal customers, because an ordinary printed ticket could be inserted in the payment
            kiosk at 10:15 (no charge) and another 15 free minutes would be given (up to 10:30)

            return the completed session in the out parameter and true in the main return value

            3b) If there is no connected user, set session = null, return false (the visitor
            has to insert the parking ticket and pay at the kiosk)
            */
            throw new NotImplementedException();
        }
    }
}
