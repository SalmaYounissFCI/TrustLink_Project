using Microsoft.EntityFrameworkCore.Storage;
using SW_Project.Data;
using SW_Project.Interfaces;
using SW_Project.Models;

namespace SW_Project.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        // Repositories
        public IBaseRepository<ApplicationUser> Users { get; private set; }
        public IBaseRepository<Listing> Listings { get; private set; }
        public IBaseRepository<Booking> Bookings { get; private set; }
        public IBaseRepository<Category> Categories { get; private set; }
        public IBaseRepository<ContactMessage> ContactMessages { get; private set; }
        public IBaseRepository<Contract> Contracts { get; private set; }
        public IBaseRepository<ContractSignature> ContractSignatures { get; private set; }
        public IBaseRepository<Conversation> Conversations { get; private set; }
        public IBaseRepository<Favorite> Favorites { get; private set; }
        public IBaseRepository<ListingImage> ListingImages { get; private set; }
        public IBaseRepository<Message> Messages { get; private set; }
        public IBaseRepository<Notification> Notifications { get; private set; }
        public IBaseRepository<Payment> Payments { get; private set; }
        public IBaseRepository<Report> Reports { get; private set; }
        public IBaseRepository<Review> Reviews { get; private set; }
        public IBaseRepository<ItemAvailability> ItemAvailabilities { get; private set; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            // Initialize all repositories
            Users = new BaseRepository<ApplicationUser>(_context);
            Listings = new BaseRepository<Listing>(_context);
            Bookings = new BaseRepository<Booking>(_context);
            Categories = new BaseRepository<Category>(_context);
            ContactMessages = new BaseRepository<ContactMessage>(_context);
            Contracts = new BaseRepository<Contract>(_context);
            ContractSignatures = new BaseRepository<ContractSignature>(_context);
            Conversations = new BaseRepository<Conversation>(_context);
            Favorites = new BaseRepository<Favorite>(_context);
            ListingImages = new BaseRepository<ListingImage>(_context);
            Messages = new BaseRepository<Message>(_context);
            Notifications = new BaseRepository<Notification>(_context);
            Payments = new BaseRepository<Payment>(_context);
            Reports = new BaseRepository<Report>(_context);
            Reviews = new BaseRepository<Review>(_context);
            ItemAvailabilities = new BaseRepository<ItemAvailability>(_context);
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}