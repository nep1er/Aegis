using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Data.Entities
{
    public class ParkingRecord
    {
        public int Id { get; set; }
        public int SpotId { get; set; }
        public DateTime AdmissionDate { get; set; }
        public int OperatorId { get; set; }
        public int VehicleTypeId { get; set; }
        public int? VehicleId { get; set; }
        public int VehicleStatusId { get; set; }

        public Spot? Spot { get; set; }
        public Operator? Operator { get; set; }
        public VehicleType? VehicleType { get; set; }
        public Vehicle? Vehicle { get; set; }
        public VehicleStatus? VehicleStatus { get; set; }
    }
}
