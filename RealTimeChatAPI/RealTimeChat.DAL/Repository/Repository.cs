using Microsoft.EntityFrameworkCore;
using RealTimeChat.DAL.Data;
using RealTimeChat.DAL.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.DAL.Repository
{
    public class Repository < T > : IRepository < T > where T: class
    {
        private readonly RealTimeChatContext _db;
        private DbSet<T> dbSet;
        string errorMessage = string.Empty;

        public Repository(RealTimeChatContext db)
        {
            _db = db;
           this.dbSet = _db.Set<T>();
        }

        public IEnumerable<T> GetAll()
        {
            IQueryable<T> query = dbSet.AsQueryable();
            return query.ToList();
        }

        public T Get(Expression<Func<T,bool>> filter)
        {
           IQueryable<T> query = dbSet.AsQueryable();
            query = query.Where(filter);
            return query.FirstOrDefault();

        }

        public void Add(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            dbSet.Add(entity);
            _db.SaveChanges();
        }

        public void Update(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            _db.SaveChanges();
        }

        public void Remove(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            dbSet.Remove(entity);
            _db.SaveChanges();
        }

     }
}

