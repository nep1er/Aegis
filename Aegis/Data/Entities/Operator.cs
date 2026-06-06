using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Data.Entities
{
    public class Operator
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ParkingId { get; set; }

        public User? User { get; set; }
        public Parking? Parking { get; set; }
    }
}
