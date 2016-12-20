using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.ORM
{
    public class ToDoContext : DbContext
    {
        public ToDoContext() : base("ToDoContext")
        {
        }

        public DbSet<ToDo> ToDos { get; set; }
    }
}
