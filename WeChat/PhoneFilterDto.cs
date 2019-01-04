using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeChat
{
    public class PhoneFilterDto
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
        public string Sex { get; set; }
        public string IsFilter { get; set; }
    }
}
