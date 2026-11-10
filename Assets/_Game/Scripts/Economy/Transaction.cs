using System;
using EmpireBuilder.Core;
using UnityEngine;

namespace EmpireBuilder.Economy
{
    [Serializable]
    public class Transaction
    {
        public string transactionId;
        public int dayNumber;
        public double amount; // Positive = Inflow (Cash received), Negative = Outflow (Cash paid)
        public TransactionCategory category;
        public string description;
        public string assetId;

        public Transaction()
        {
        }

        public Transaction(int day, double amt, TransactionCategory cat, string desc, string assetRefId = "")
        {
            transactionId = Guid.NewGuid().ToString("N");
            dayNumber = day;
            amount = amt;
            category = cat;
            description = desc;
            assetId = assetRefId;
        }

        public bool IsIncome => amount > 0;
        public bool IsExpense => amount < 0;

        public string FormattedAmount => amount >= 0 
            ? $"+${amount:N0}" 
            : $"-${Math.Abs(amount):N0}";
    }
}

