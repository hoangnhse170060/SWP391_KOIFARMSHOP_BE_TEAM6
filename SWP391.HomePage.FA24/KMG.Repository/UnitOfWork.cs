using KMG.Repository.Models;
using KMG.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository
{
    public class UnitOfWork
    {
        private readonly SwpkoiFarmShopContext _context;
        private KoiRepository _koiRepository;
        private KoiTypeRepository _koiTypeRepository;
        private FishRepository _fishRepository;
        private PromotionRepository _promotionRepository;
        private AddressRepository _addressRepository;
        private UserRepository _userRepository;
        private FeedbackRepository _feedbackRepository;
        private PurchaseHistoryRepository _purcharHistory;
        private OrderRepository _orderRepository;
        public OrderKoiRepository _orderKoiRepository;
        public OrderFishRepository _orderFishRepository;
        public  UnitOfWork() => _context = new SwpkoiFarmShopContext();
        public UserRepository UserRepository
        {
            get
            {
                return _userRepository ??= new UserRepository(_context);
            }
        }
        public KoiRepository KoiRepository
        {
            get
            {
                return _koiRepository ??= new KoiRepository(_context);
            }
        }

        public KoiTypeRepository KoiTypeRepository
        {
            get
            {
                return _koiTypeRepository ??= new KoiTypeRepository(_context);
            }
        }
        public FishRepository FishRepository
        {
            get
            {
                return _fishRepository ??= new FishRepository(_context);
            }
        }
        public PromotionRepository PromotionRepository
        {
            get
            {
                return _promotionRepository ??= new PromotionRepository(_context);
            }
        }
        public AddressRepository AddressRepository
        {
            get
            {
                return _addressRepository ??= new AddressRepository(_context);
            }
        }
        public FeedbackRepository FeedbackRepository
        {
            get
            {
                return _feedbackRepository ??= new FeedbackRepository(_context);
            }
        }
        public PurchaseHistoryRepository PurchaseHistoryRepository
        {
            get
            {
                return _purcharHistory ??= new PurchaseHistoryRepository(_context);
            }
        }
        public OrderRepository OrderRepository
        {
            get
            {
                return _orderRepository ??= new OrderRepository(_context);
            }
        }
        public OrderKoiRepository OrderKoiRepository
        {
            get
            {
                return _orderKoiRepository ??= new OrderKoiRepository(_context);
            }
        }
        public OrderFishRepository OrderFishRepository
        {
            get
            {
                return _orderFishRepository ??= new OrderFishRepository(_context);
            }
        }
    }
}
