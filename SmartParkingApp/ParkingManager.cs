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
            session = activeSessions.Find(e => e.CarPlateNumber == carPlateNumber);
            var remainingCost = GetRemainingCost(session.TicketNumber);
            if (session.PaymentDt == null & remainingCost == 0)
            {
                session.ExitDt = DateTime.Now;
                activeSessions.Remove(session);
                endedSessions.Add(session);
                return true;
            }
            else
            {
                if (session.PaymentDt != null)
                {
                    if (remainingCost == 0)
                    {
                        session.ExitDt = DateTime.Now;
                        activeSessions.Remove(session);
                        endedSessions.Add(session);
                        return true;
                    }
                    else
                    {
                        session = null;
                        return false;
                    }
                }
                else
                {
                    if (session.PaymentDt != null)
                    {
                        if (session.ParkingUser != null)
                        {
                            session.ExitDt = DateTime.Now;
                            session.PaymentDt = session.ExitDt;
                            session.EntryDt.AddMinutes(15);
                            var userRemainingCost = GetRemainingCost(session.TicketNumber);
                            session.TotalPayment = remainingCost;
                            activeSessions.Remove(session);
                            endedSessions.Add(session);
                            return true;
                        }
                        else
                        {
                            session = null;
                            return false;
                        }
                    }
                }
            }
            session = null;
            return false;
        }
    }
}