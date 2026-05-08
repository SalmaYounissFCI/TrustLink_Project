using SW_Project.Models;

namespace SW_Project.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Repositories
        IBaseRepository<ApplicationUser> Users { get; }
        IBaseRepository<Listing> Listings { get; }
        IBaseRepository<Booking> Bookings { get; }
        IBaseRepository<Category> Categories { get; }
        IBaseRepository<ContactMessage> ContactMessages { get; }
        IBaseRepository<Contract> Contracts { get; }
        IBaseRepository<ContractSignature> ContractSignatures { get; }
        IBaseRepository<Conversation> Conversations { get; }
        IBaseRepository<Favorite> Favorites { get; }
        IBaseRepository<ListingImage> ListingImages { get; }
        IBaseRepository<Message> Messages { get; }
        IBaseRepository<Notification> Notifications { get; }
        IBaseRepository<Payment> Payments { get; }
        IBaseRepository<Report> Reports { get; }
        IBaseRepository<Review> Reviews { get; }
        IBaseRepository<ItemAvailability> ItemAvailabilities { get; }

        // Save and Transaction
        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}