using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aegis.Data.Entities
{
    public class VehicleType
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal TowFine { get; set; }
    }
}
