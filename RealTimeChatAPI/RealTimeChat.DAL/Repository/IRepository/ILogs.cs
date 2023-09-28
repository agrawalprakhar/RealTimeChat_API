using RealTimeChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeChat.DAL.Repository.IRepository
{
    public  interface ILogs
    {
        List<Logs> GetLogs(DateTime? startTime = null, DateTime? endTime = null);
    }
}
