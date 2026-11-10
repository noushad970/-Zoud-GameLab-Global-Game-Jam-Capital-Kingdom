namespace EmpireBuilder.Core
{
    public enum AssetType
    {
        Vehicle,
        Land,
        Building,
        Resort
    }

    public enum AssetState
    {
        AvailableForPurchase,
        Operating,
        NeedsRepair,
        UnderRepair,
        Inactive,
        Sold
    }

    public enum TransactionCategory
    {
        InitialCapital,
        Revenue,
        OperatingExpense,
        Repair,
        Purchase,
        Sale,
        Event,
        Tax,
        Loan
    }

    public enum ProgressionStage
    {
        Beginner,        // $0 - $25,000
        Entrepreneur,    // $25,000 - $100,000
        BusinessOwner,   // $100,000 - $500,000
        Investor,        // $500,000 - $2,000,000
        Tycoon           // $2,000,000+
    }
}

