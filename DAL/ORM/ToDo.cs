using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.ORM
{
    public class ToDo
    {
        public int Id { get; set; }

        public bool Completed { get; set; }

        public string Task { get; set; }

        public int UserId { get; set; }

        public int? AzureId { get; set; }
    }
}
