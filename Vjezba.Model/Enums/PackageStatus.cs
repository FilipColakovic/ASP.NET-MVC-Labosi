namespace Vjezba.Model.Enums
{
    public enum PackageStatus
    {
        Created = 1,
        PendingPickup = 2,
        PickedUp = 3,
        InTransit = 4,
        OutForDelivery = 5,
        Delivered = 6,
        DeliveryFailed = 7,
        ReturnedToSender = 8,
        Cancelled = 9
    }
}
