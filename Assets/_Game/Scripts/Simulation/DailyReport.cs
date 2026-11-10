using System;
using System.Collections.Generic;
using EmpireBuilder.Economy;

namespace EmpireBuilder.Simulation
{
    [Serializable]
    public class DailyReport
    {
        public int dayNumber;
        public double grossRevenue;
        public double operatingExpenses;
        public double operatingCosts => operatingExpenses;
        public double maintenanceCosts;
        public double netCashChange;
        public double endingCash;
        public double endingNetWorth;
        public string financialLesson;
        public List<Transaction> transactions = new List<Transaction>();

        public DailyReport()
        {
        }

        public DailyReport(int day, double revenue, double expenses, double repairs, double cashChange, double endCash, double endNetWorth, string lesson, List<Transaction> txs)
        {
            dayNumber = day;
            grossRevenue = revenue;
            operatingExpenses = expenses;
            maintenanceCosts = repairs;
            netCashChange = cashChange;
            endingCash = endCash;
            endingNetWorth = endNetWorth;
            financialLesson = lesson;
            if (txs != null)
            {
                transactions.AddRange(txs);
            }
        }
    }
}

