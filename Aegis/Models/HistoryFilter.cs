using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Models
{
    public class HistoryFilter
    {
        public string? LicensePlate { get; set; }
        public string? Vin { get; set; }
        public int? OperatorId { get; set; }
        public string? DocumentNumber { get; set; }
        public int? DocumentTypeId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? OperationType { get; set; }
    }
}
