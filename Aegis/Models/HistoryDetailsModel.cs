using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Models
{
    public class HistoryDetailsModel
    {
        public int ParkingRecordId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string? Vin { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public string SpotNumber { get; set; } = string.Empty;
        public string ParkingAddress { get; set; } = string.Empty;

        public DateTime AdmissionDate { get; set; }
        public string AdmissionOperator { get; set; } = string.Empty;

        public DateTime? ReleaseDate { get; set; }
        public string? ReleaseOperator { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentNumber { get; set; }
        public decimal? StorageFee { get; set; }
        public decimal? TowFine { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? ReceiptNumber { get; set; }

        public IEnumerable<VehiclePhotoModel> Photos { get; set; } = new List<VehiclePhotoModel>();
    }
}
