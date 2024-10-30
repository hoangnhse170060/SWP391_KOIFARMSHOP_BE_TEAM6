using KMG.Repository.Dto;
using KMG.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KMG.Repository.Interfaces
{
    public interface IConsignmentService
    {
        // Tạo mới một Consignment
        Task<ConsignmentDto> CreateConsignmentAsync(int userID, int koiID, string consignmentType, string status, decimal consignmentPrice, DateTime consignmentDateFrom, DateTime consignmentDateTo, string userImage, string consignmentTitle, string consignmentDetail);

        // Cập nhật Consignment theo ID
        Task<bool> UpdateConsignmentAsync(int consignmentId, int userID, int koiID, string consignmentType, string status, decimal consignmentPrice, DateTime consignmentDateFrom, DateTime consignmentDateTo, string userImage, string consignmentTitle, string consignmentDetail);

        // Xóa Consignment theo ID
        Task<bool> DeleteConsignmentAsync(int consignmentId);

        // Lấy thông tin Consignment theo ID
        Task<ConsignmentDto> GetConsignmentByIdAsync(int consignmentId);

        // Lấy tất cả danh sách Consignments
        Task<IEnumerable<ConsignmentDto>> GetAllConsignmentsAsync();

        Task<ConsignmentDto> CreateConsignmentFromOrderAsync(int userID, int koiID, string consignmentType, string status, decimal consignmentPrice, DateTime consignmentDateFrom, DateTime consignmentDateTo, string userImage, string consignmentTitle, string consignmentDetail);


    }
}
