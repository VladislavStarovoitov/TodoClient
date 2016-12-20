using DAL.ORM;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace DAL.Repositories
{
    public class ToDoRepository : IDisposable
    {
        private DbContext _dataBase;

        public ToDoRepository (DbContext context)
        {
            _dataBase = context;
        }

        public ToDo Get(int id)
        {
            return _dataBase.Set<ToDo>().Find(id);
        }

        public IEnumerable<ToDo> GetAll()
        {
            return _dataBase.Set<ToDo>().Select(x => x);
        }

        public bool Create(ToDo toDo)
        {
            _dataBase.Set<ToDo>().Add(toDo);
            return _dataBase.SaveChanges() > 0;
        }

        public bool Update(ToDo toDo)
        {
            _dataBase.Entry(toDo).State = EntityState.Modified; // if there are changes
            return _dataBase.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            var entity = _dataBase.Set<ToDo>().Find(id);

            if (ReferenceEquals(entity, null))
            {
                return false;
            }

            _dataBase.Set<ToDo>().Remove(entity);
            return _dataBase.SaveChanges() > 0;
        }

        public bool SaveChanges()
        {
            return _dataBase.SaveChanges() > 0;
        }

        public void Dispose()
        {
            _dataBase.Dispose();
        }
    }
}
