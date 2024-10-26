
    using KMG.Repository.Base;
    using KMG.Repository.Models;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace KMG.Repository.Repositories
    {

        public class PaymentTransactionRepository : GenericRepository<PaymentTransaction>
        {
            private readonly SwpkoiFarmShopContext _context;

            public PaymentTransactionRepository(SwpkoiFarmShopContext context)
                : base(context) // Pass the context to the base class
            {
                _context = context;
            }

            // Synchronous version
            public PaymentTransaction GetByTxnRef(string? txnRef)
            {
                return _context.PaymentTransactions
                    .FirstOrDefault(pt => pt.TxnRef == txnRef);
            }

            // Asynchronous version
            public async Task<PaymentTransaction> GetByTxnRefAsync(string? txnRef)
            {
                return await _context.PaymentTransactions
                    .FirstOrDefaultAsync(pt => pt.TxnRef == txnRef);
            }
        }


    }
