using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Data.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string? Vin { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int VehicleTypeId { get; set; }

        public VehicleType? VehicleType { get; set; }
    }
}
