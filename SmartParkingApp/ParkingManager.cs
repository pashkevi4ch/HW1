using SmartParkingApp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ParkingApp
{
    class ParkingManager
    {
        private List<ParkingSession> activeSessions = new List<ParkingSession>();
        private List<ParkingSession> endedSessions = new List<ParkingSession>();
        private List<Tariff> tariff = new List<Tariff>();
        private int capacity;
        private List<User> users = new List<User>();

        public ParkingSession EnterParking(string carPlateNumber)
        {
            if (activeSessions.Count < capacity)
            {
                var check = activeSessions.Exists(e => e.CarPlateNumber == carPlateNumber);
                if (check == true)
                    return null;
                else
                {
                    var newSession = new ParkingSession();
                    newSession.EntryDt = DateTime.Now;
                    newSession.CarPlateNumber = carPlateNumber;
                    if (activeSessions.Count != 0)
                        newSession.TicketNumber = activeSessions[activeSessions.Count - 1].TicketNumber + 1;
                        newSession.TicketNumber = 1;
                    var checkuser = users.Exists(e => e.CarPlateNumber == carPlateNumber);
                    if (checkuser == true)
                        newSession.ParkingUser = users.Find(e => e.CarPlateNumber == carPlateNumber);
                    activeSessions.Add(newSession);
                    SessionFileRewriter(activeSessions, "/dataActiveSession.txt");
                    return newSession;
                }
            }
            else
                return null;
        }


        public bool TryLeaveParkingWithTicket(int ticketNumber, out ParkingSession session)
        {
            session = activeSessions.Find(e => e.TicketNumber == ticketNumber);
            if (GetRemainingCost(session.TicketNumber) == 0)
            {
                session.ExitDt = DateTime.Now;
                activeSessions.Remove(session);
                SessionFileRewriter(activeSessions, "/dataActiveSession.txt");
                endedSessions.Add(session);
                SessionFileRewriter(endedSessions, "/dataEndedSession.txt");
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
            int? exitingTime;
            var currentTime = DateTime.Now;
            var session = activeSessions.Find(e => e.TicketNumber == ticketNumber);

            if (session.PaymentDt != null)
            {
                var tmpExitingTime = currentTime - session.PaymentDt;
                exitingTime = (tmpExitingTime?.Days * 24) * 60 + (tmpExitingTime?.Hours * 60) + (tmpExitingTime?.Minutes);
                if (exitingTime < tariff[tariff.Count - 1].Minutes)
                {
                    remainingCost = tariff.First(e => e.Minutes >= exitingTime).Rate;
                    return remainingCost;
                }
                else
                {
                    remainingCost = tariff[tariff.Count - 1].Rate;
                    return remainingCost;
                }
            }
            else
            {
                var tmpParkingTime = currentTime - session.EntryDt;
                parkingTime = (tmpParkingTime.Days * 24) * 60 + (tmpParkingTime.Hours * 60) + (tmpParkingTime.Minutes);
                remainingCost = tariff.First(e => e.Minutes >= parkingTime).Rate;
                return remainingCost;
            }
        }


        public void PayForParking(int ticketNumber, decimal amount)
        {
            var session = activeSessions.Find(e => e.TicketNumber == ticketNumber);
            session.PaymentDt = DateTime.Now;
            if (session.TotalPayment != null)
                session.TotalPayment += amount;
            else
                session.TotalPayment = amount;
        }


        public void GetData()
        {
            using (FileStream dataActiveSession = new FileStream(@"../../../dataActiveSession.txt", FileMode.OpenOrCreate))
            {
                byte[] array = new byte[dataActiveSession.Length];
                dataActiveSession.Read(array, 0, array.Length);
                string fullText = System.Text.Encoding.Default.GetString(array);
                activeSessions = SessionParser(fullText);
                CheckUser(activeSessions);
            }
            using (FileStream dataEndedSession = new FileStream(@"../../../dataEndedSession.txt", FileMode.OpenOrCreate))
            {
                byte[] array = new byte[dataEndedSession.Length];
                dataEndedSession.Read(array, 0, array.Length);
                string fullText = System.Text.Encoding.Default.GetString(array);
                endedSessions = SessionParser(fullText);
                CheckUser(endedSessions);
            }
            using (FileStream dataTariffs = new FileStream(@"../../../dataTariffs.txt", FileMode.Open))
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
            using (FileStream dataCapacity = new FileStream(@"../../../dataCapacity.txt", FileMode.Open))
            {
                byte[] array = new byte[dataCapacity.Length];
                dataCapacity.Read(array, 0, array.Length);
                string fullText = System.Text.Encoding.Default.GetString(array);
                capacity = Convert.ToInt32(fullText);
            }
            using (FileStream dataUsers = new FileStream(@"../../../dataUsers.txt", FileMode.Open))
            {
                byte[] array = new byte[dataUsers.Length];
                dataUsers.Read(array, 0, array.Length);
                string fullText = System.Text.Encoding.Default.GetString(array);
                var tmpData = fullText.Split(new char[] { ';' });
                foreach (var s in tmpData)
                {
                    if (s != "")
                    {
                        var data = s.Split(new char[] { ',' });
                        var newUser = new User();
                        newUser.Name = data[0];
                        newUser.CarPlateNumber = data[1];
                        newUser.Phone = data[2];
                        users.Add(newUser);
                    }
                }
            }
        }


        private void SessionFileRewriter(List<ParkingSession> sessions, string filename)
        {
            var newText = "";
            foreach (var s in sessions)
            {
                newText += s.EntryDt + "," + s.PaymentDt + "," + s.ExitDt + "," + s.TotalPayment + "," + s.CarPlateNumber + "," + s.TicketNumber + ";";
            }
            using (StreamWriter sw = new StreamWriter(@"../../.." + filename, false, System.Text.Encoding.Default))
            {
                sw.Write(newText);
            }
        }


        private List<ParkingSession> SessionParser(string fullText)
        {
            List<ParkingSession> parkingSessions = new List<ParkingSession>();
            if (fullText != "")
            {
                var tmpData = fullText.Split(new char[] { ';' });
                foreach (var s in tmpData)
                {
                    if (s != "")
                    {
                        var data = s.Split(new char[] { ',' });
                        var newSession = new ParkingSession();
                        if (data[0] != "")
                            newSession.EntryDt = DateTime.Parse(data[0]);
                        if (data[1] != "")
                            newSession.PaymentDt = DateTime.Parse(data[1]);
                        if (data[2] != "")
                            newSession.ExitDt = DateTime.Parse(data[2]);
                        if (data[3] != "")
                            newSession.TotalPayment = Convert.ToDecimal(data[3]);
                        newSession.CarPlateNumber = data[4];
                        newSession.TicketNumber = Convert.ToInt32(data[5]);
                        parkingSessions.Add(newSession);
                    }
                }
            }
            return parkingSessions;
        }


        private void CheckUser(List<ParkingSession> sessions)
        {
            foreach (var s in sessions)
            {
                foreach (var u in users)
                {
                    if (u.CarPlateNumber == s.CarPlateNumber)
                        s.ParkingUser = u;
                }
            }
        }


        public bool TryLeaveParkingByCarPlateNumber(string carPlateNumber, out ParkingSession session)
        {
            session = activeSessions.Find(e => e.CarPlateNumber == carPlateNumber);
            var remainingCost = GetRemainingCost(session.TicketNumber);
            if (session.PaymentDt == null & remainingCost == 0)
            {
                session.ExitDt = DateTime.Now;
                activeSessions.Remove(session);
                SessionFileRewriter(activeSessions, "/dataActiveSession.txt");
                endedSessions.Add(session);
                SessionFileRewriter(endedSessions, "/dataEndedSession.txt");
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
                        SessionFileRewriter(activeSessions, "/dataActiveSession.txt");
                        endedSessions.Add(session);
                        SessionFileRewriter(endedSessions, "/dataEndedSession.txt");
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
    }
}