using KMG.Repository.Base;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Repositories
{
    public class OrderRepository : GenericRepository<Order>
    {
        private readonly SwpkoiFarmShopContext _context;
        public OrderRepository(SwpkoiFarmShopContext context) => _context = context;


        public async Task<bool> DeleteWithId(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                _context.Orders.Remove(order);  // Đảm bảo thực thể được xóa khỏi tập hợp đúng
                await _context.SaveChangesAsync();  // Phải gọi hàm này để lưu thay đổi vào cơ sở dữ liệu
                return true;
            }
            return false;
        }


    }



}

