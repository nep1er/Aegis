using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Data.Entities
{
    public class Parking
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public int ParkingStatusId { get; set; }

        public ParkingStatus? ParkingStatus { get; set; }
        public ICollection<Spot>? Spots { get; set; }
    }
}
