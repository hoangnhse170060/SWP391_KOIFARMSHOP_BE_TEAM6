using KMG.Repository.Dto;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KMG.Repository.Interfaces
{
    public interface IConsignmentService
    {
        // Tạo mới một Consignment
        Task<ConsignmentDto> CreateConsignmentAsync(int userID,int koitypeID, int? koiID, string consignmentType, string status, decimal consignmentPrice, DateTime consignmentDateFrom, DateTime consignmentDateTo, string userImage, string consignmentTitle, string consignmentDetail);

        // Cập nhật Consignment theo ID
        Task<bool> UpdateConsignmentAsync(int consignmentId, int userID, int koitypeID, int koiID, string consignmentType, string status, decimal consignmentPrice, DateTime consignmentDateFrom, DateTime consignmentDateTo, string userImage, string consignmentTitle, string consignmentDetail);

        // Xóa Consignment theo ID
        Task<bool> DeleteConsignmentAsync(int consignmentId);

        // Lấy thông tin Consignment theo ID
        Task<ConsignmentDto> GetConsignmentByIdAsync(int consignmentId);

        // Lấy tất cả danh sách Consignments
        Task<IEnumerable<ConsignmentDto>> GetAllConsignmentsAsync();

        // Lấy tất cả Consignments theo UserId
        Task<IEnumerable<ConsignmentDto>> GetConsignmentsByUserIdAsync(int userId);

        Task<IEnumerable<ConsignmentDto>> GetConsignmentsByUserNameAsync(string userName);

        Task<bool> UpdateConsignmentStatusAsync(int consignmentId, string newStatus);

        Task<ConsignmentDto> CreateConsignmentOrderAsync(int userId, int koiTypeId, int koiId, string consignmentType, decimal consignmentPrice, string? consignmentTitle, string? consignmentDetail);


         Task<ConsignmentDto> CreateConsignmentTakeCareOutsideShopAsync(
    int userId, int koiTypeId, string name, string origin, string gender, int age, decimal size,
    string breed, string personality, decimal feedingAmount, decimal filterRate, string healthStatus,
    string awardCertificates, string description, string detailDescription, string imageKoi,
    string imageCertificate, string additionImage, string consignmentType, DateTime consignmentDateTo,
    string consignmentTitle, string consignmentDetail);

        Task<ConsignmentDto> CreateConsignmentOrderFromOutsideShopAsync(
    int userId, int koiTypeId, string name, string origin, string gender, int age, decimal size,
    string breed, string personality, decimal feedingAmount, decimal filterRate, string healthStatus,
    string awardCertificates, string description, string detailDescription, string imageKoi,
    string imageCertificate, string additionImage, string consignmentType,
    string consignmentTitle, string consignmentDetail);


    }
}